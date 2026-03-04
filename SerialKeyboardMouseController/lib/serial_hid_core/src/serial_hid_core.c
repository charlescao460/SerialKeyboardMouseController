#include "serial_hid_core.h"

#include <string.h>

#define SHD_DEFAULT_TIMEOUT_MS ((uint32_t)1000u)
#define SHD_REQ_TYPE_OFFSET ((size_t)2u)
#define SHD_INVALID_FRAME_ID ((uint16_t)0xFFFFu)

typedef struct shd_reply_info {
    shd_reply_type_t type;
    uint8_t payload[4];
    size_t payload_len;
} shd_reply_info_t;

static bool shd_core_deps_valid(const shd_core_deps_t* deps)
{
    return deps != NULL && deps->serial.available != NULL && deps->serial.read_byte != NULL &&
           deps->serial.read_bytes != NULL && deps->serial.write_bytes != NULL &&
           deps->keyboard.press_scan_code != NULL && deps->keyboard.release_scan_code != NULL &&
           deps->keyboard.release_all != NULL && deps->keyboard.get_lock_state != NULL &&
           deps->rel_mouse.move != NULL && deps->rel_mouse.scroll != NULL &&
           deps->rel_mouse.press != NULL && deps->rel_mouse.release != NULL &&
           deps->abs_mouse.move != NULL && deps->abs_mouse.change_resolution != NULL &&
           deps->clock.now_ms != NULL && deps->host.get_status_flags != NULL &&
           deps->reset.reboot != NULL;
}

static void shd_log(const shd_core_t* core, const char* msg)
{
    if (core != NULL && core->deps.logger.log != NULL)
    {
        core->deps.logger.log(core->deps.logger.ctx, msg);
    }
}

static bool shd_timeout_expired(uint32_t now, uint32_t deadline)
{
    return (int32_t)(now - deadline) >= 0;
}

static uint8_t shd_crc8_default(const uint8_t* data, size_t len)
{
    uint8_t crc = 0u;
    size_t i = 0u;

    for (i = 0u; i < len; ++i)
    {
        uint8_t bit = 0u;
        crc ^= data[i];
        for (bit = 0u; bit < 8u; ++bit)
        {
            if ((crc & 0x80u) != 0u)
            {
                crc = (uint8_t)((crc << 1u) ^ 0x07u);
            }
            else
            {
                crc <<= 1u;
            }
        }
    }

    return crc;
}

static uint8_t shd_crc8_compute(const shd_core_t* core, const uint8_t* data, size_t len)
{
    if (core->deps.crc8.compute != NULL)
    {
        return core->deps.crc8.compute(core->deps.crc8.ctx, data, len);
    }
    return shd_crc8_default(data, len);
}

static shd_status_t shd_read_exact(shd_core_t* core, uint8_t* dst, size_t len, size_t* out_received)
{
    const uint32_t start = core->deps.clock.now_ms(core->deps.clock.ctx);
    const uint32_t deadline = start + core->timeout_ms;
    size_t received = 0u;

    while (received < len)
    {
        size_t count = core->deps.serial.read_bytes(core->deps.serial.ctx, dst + received, len - received);
        if (count > 0u)
        {
            received += count;
            continue;
        }

        if (core->deps.serial.available(core->deps.serial.ctx) > 0)
        {
            const int b = core->deps.serial.read_byte(core->deps.serial.ctx);
            if (b >= 0)
            {
                dst[received] = (uint8_t)b;
                ++received;
                continue;
            }
        }

        if (shd_timeout_expired(core->deps.clock.now_ms(core->deps.clock.ctx), deadline))
        {
            if (out_received != NULL)
            {
                *out_received = received;
            }
            return SHD_STATUS_TIMEOUT;
        }
    }

    if (out_received != NULL)
    {
        *out_received = received;
    }
    return SHD_STATUS_OK;
}

static uint16_t shd_read_u16_le(const uint8_t* src)
{
    return (uint16_t)(((uint16_t)src[0]) | ((uint16_t)src[1] << 8u));
}

static void shd_write_u16_le(uint8_t* dst, uint16_t value)
{
    dst[0] = (uint8_t)(value & 0xFFu);
    dst[1] = (uint8_t)((value >> 8u) & 0xFFu);
}

static int16_t shd_read_i16_le(const uint8_t* src)
{
    return (int16_t)shd_read_u16_le(src);
}

static shd_status_t shd_send_reply(shd_core_t* core,
                                   uint16_t frame_id,
                                   shd_reply_type_t reply_type,
                                   const uint8_t* payload,
                                   size_t payload_len)
{
    const size_t length = SHD_FRAME_ID_SIZE + 1u + payload_len + SHD_CRC8_SIZE;
    uint8_t* const body = core->frame_buffer + 2u;
    uint8_t crc = 0u;

    if (length > SHD_MAX_DATA_LENGTH)
    {
        return SHD_STATUS_INVALID_LENGTH;
    }

    core->frame_buffer[0] = SHD_FRAME_START;
    core->frame_buffer[1] = (uint8_t)length;
    shd_write_u16_le(body, frame_id);
    body[2] = (uint8_t)reply_type;

    if (payload_len > 0u && payload != NULL)
    {
        memcpy(body + 3u, payload, payload_len);
    }

    crc = shd_crc8_compute(core, body, length - SHD_CRC8_SIZE);
    body[length - 1u] = crc;

    if (core->deps.serial.write_bytes(core->deps.serial.ctx, core->frame_buffer, length + 2u) != length + 2u)
    {
        return SHD_STATUS_IO_ERROR;
    }

    return SHD_STATUS_OK;
}

static uint16_t shd_frame_id_from_partial(const uint8_t* body, size_t received)
{
    if (received >= 2u)
    {
        return shd_read_u16_le(body);
    }
    if (received == 1u)
    {
        return (uint16_t)(0xFF00u | (uint16_t)body[0]);
    }
    return SHD_INVALID_FRAME_ID;
}

static shd_reply_type_t shd_error_reply_type(shd_status_t status)
{
    if (status == SHD_STATUS_TIMEOUT)
    {
        return SHD_REPLY_TIMEOUT;
    }
    return SHD_REPLY_INVALID;
}

static shd_status_t shd_execute_request(shd_core_t* core,
                                        uint8_t req_type,
                                        const uint8_t* payload,
                                        size_t payload_len,
                                        shd_reply_info_t* reply)
{
    if (reply == NULL)
    {
        return SHD_STATUS_BAD_DEPENDENCY;
    }

    reply->type = SHD_REPLY_OP_OK;
    reply->payload_len = 0u;

    switch ((shd_frame_type_t)req_type)
    {
    case SHD_FRAME_REL_MOUSE_MOVE:
    {
        int16_t dxv = 0;
        int16_t dyv = 0;

        if (payload_len != 4u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        dxv = shd_read_i16_le(payload);
        dyv = shd_read_i16_le(payload + 2u);
        if (dxv < -128 || dxv > 127 || dyv < -128 || dyv > 127)
        {
            return SHD_STATUS_OUT_OF_RANGE;
        }

        core->deps.rel_mouse.move(core->deps.rel_mouse.ctx, (int8_t)dxv, (int8_t)dyv);
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_MOUSE_MOVE_ABS:
    {
        uint16_t x = 0u;
        uint16_t y = 0u;

        if (payload_len != 4u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        x = shd_read_u16_le(payload);
        y = shd_read_u16_le(payload + 2u);
        if (x == 0u || y == 0u || x > core->current_resolution_width || y > core->current_resolution_height)
        {
            return SHD_STATUS_OUT_OF_RANGE;
        }

        core->deps.abs_mouse.move(core->deps.abs_mouse.ctx, x, y);
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_MOUSE_SCROLL:
    {
        if (payload_len != 1u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }
        core->deps.rel_mouse.scroll(core->deps.rel_mouse.ctx, (int8_t)payload[0]);
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_MOUSE_PRESS:
    {
        if (payload_len != 1u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }
        core->deps.rel_mouse.press(core->deps.rel_mouse.ctx, payload[0]);
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_MOUSE_RELEASE:
    {
        if (payload_len != 1u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        if (payload[0] == SHD_RELEASE_ALL_KEYS)
        {
            const uint8_t all_buttons =
                (uint8_t)(SHD_MOUSE_BTN_LEFT | SHD_MOUSE_BTN_RIGHT | SHD_MOUSE_BTN_MIDDLE);
            core->deps.rel_mouse.release(core->deps.rel_mouse.ctx, all_buttons);
        }
        else
        {
            core->deps.rel_mouse.release(core->deps.rel_mouse.ctx, payload[0]);
        }
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_MOUSE_RESOLUTION:
    {
        uint16_t width = 0u;
        uint16_t height = 0u;

        if (payload_len != 4u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        width = shd_read_u16_le(payload);
        height = shd_read_u16_le(payload + 2u);
        if (width == 0u || height == 0u || width > SHD_MAX_RESOLUTION_WIDTH ||
            height > SHD_MAX_RESOLUTION_HEIGHT)
        {
            return SHD_STATUS_OUT_OF_RANGE;
        }

        core->current_resolution_width = width;
        core->current_resolution_height = height;
        core->deps.abs_mouse.change_resolution(core->deps.abs_mouse.ctx, width, height);
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_KEY_PRESS:
    {
        if (payload_len != 1u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }
        core->deps.keyboard.press_scan_code(core->deps.keyboard.ctx, payload[0]);
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_KEY_RELEASE:
    {
        if (payload_len != 1u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        if (payload[0] == SHD_RELEASE_ALL_KEYS)
        {
            core->deps.keyboard.release_all(core->deps.keyboard.ctx);
        }
        else
        {
            core->deps.keyboard.release_scan_code(core->deps.keyboard.ctx, payload[0]);
        }
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_QUERY_KEYBOARD_LOCK:
    {
        if (payload_len != 0u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        reply->type = SHD_REPLY_KEYBOARD_LOCK;
        reply->payload[0] = core->deps.keyboard.get_lock_state(core->deps.keyboard.ctx);
        reply->payload_len = 1u;
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_QUERY_HOST_STATUS:
    {
        if (payload_len != 0u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        reply->type = SHD_REPLY_HOST_STATUS;
        reply->payload[0] = core->deps.host.get_status_flags(core->deps.host.ctx);
        reply->payload_len = 1u;
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_RESET:
    {
        if (payload_len != 1u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        core->pending_reset = 1u;
        core->pending_bootloader = (payload[0] != SHD_RESET_NORMAL) ? 1u : 0u;
        return SHD_STATUS_OK;
    }
    default:
        return SHD_STATUS_UNSUPPORTED_FRAME;
    }
}

void shd_core_init(shd_core_t* core, const shd_core_deps_t* deps)
{
    if (core == NULL)
    {
        return;
    }

    memset(core, 0, sizeof(*core));

    if (deps != NULL)
    {
        core->deps = *deps;
    }

    core->timeout_ms = SHD_DEFAULT_TIMEOUT_MS;
    core->current_resolution_width = SHD_MAX_RESOLUTION_WIDTH;
    core->current_resolution_height = SHD_MAX_RESOLUTION_HEIGHT;
}

void shd_core_set_timeout_ms(shd_core_t* core, uint32_t timeout_ms)
{
    if (core == NULL)
    {
        return;
    }
    core->timeout_ms = timeout_ms;
}

void shd_core_set_resolution(shd_core_t* core, uint16_t width, uint16_t height)
{
    if (core == NULL)
    {
        return;
    }
    if (width == 0u || height == 0u || width > SHD_MAX_RESOLUTION_WIDTH ||
        height > SHD_MAX_RESOLUTION_HEIGHT)
    {
        return;
    }

    core->current_resolution_width = width;
    core->current_resolution_height = height;
    if (core->deps.abs_mouse.change_resolution != NULL)
    {
        core->deps.abs_mouse.change_resolution(core->deps.abs_mouse.ctx, width, height);
    }
}

uint16_t shd_core_resolution_width(const shd_core_t* core)
{
    return core != NULL ? core->current_resolution_width : 0u;
}

uint16_t shd_core_resolution_height(const shd_core_t* core)
{
    return core != NULL ? core->current_resolution_height : 0u;
}

shd_status_t shd_core_tick(shd_core_t* core)
{
    uint8_t length = 0u;
    size_t received = 0u;
    shd_status_t status = SHD_STATUS_OK;
    uint8_t* body = NULL;
    uint16_t frame_id = SHD_INVALID_FRAME_ID;
    uint8_t req_type = 0u;
    uint8_t crc_expected = 0u;
    shd_reply_info_t reply;

    if (core == NULL)
    {
        return SHD_STATUS_BAD_DEPENDENCY;
    }
    if (!shd_core_deps_valid(&core->deps))
    {
        shd_log(core, "bad dependency");
        return SHD_STATUS_BAD_DEPENDENCY;
    }

    core->pending_reset = 0u;
    core->pending_bootloader = 0u;

    if (core->deps.serial.available(core->deps.serial.ctx) <= 0)
    {
        return SHD_STATUS_NO_DATA;
    }

    {
        const int start = core->deps.serial.read_byte(core->deps.serial.ctx);
        if (start < 0)
        {
            return SHD_STATUS_NO_DATA;
        }
        if ((uint8_t)start != SHD_FRAME_START)
        {
            shd_log(core, "invalid frame start");
            status = SHD_STATUS_INVALID_START;
            (void)shd_send_reply(core, SHD_INVALID_FRAME_ID, shd_error_reply_type(status), NULL, 0u);
            return status;
        }
    }

    status = shd_read_exact(core, &length, 1u, &received);
    if (status != SHD_STATUS_OK)
    {
        (void)received;
        (void)shd_send_reply(core, SHD_INVALID_FRAME_ID, shd_error_reply_type(status), NULL, 0u);
        return status;
    }
    if ((size_t)length < SHD_MIN_DATA_LENGTH || (size_t)length > SHD_MAX_DATA_LENGTH)
    {
        shd_log(core, "invalid frame length");
        status = SHD_STATUS_INVALID_LENGTH;
        (void)shd_send_reply(core, SHD_INVALID_FRAME_ID, shd_error_reply_type(status), NULL, 0u);
        return status;
    }

    status = shd_read_exact(core, core->frame_buffer, (size_t)length, &received);
    frame_id = shd_frame_id_from_partial(core->frame_buffer, received);
    if (status != SHD_STATUS_OK)
    {
        (void)shd_send_reply(core, frame_id, shd_error_reply_type(status), NULL, 0u);
        return status;
    }

    body = core->frame_buffer;
    frame_id = shd_read_u16_le(body);
    req_type = body[SHD_REQ_TYPE_OFFSET];
    crc_expected = body[(size_t)length - 1u];

    if (shd_crc8_compute(core, body, (size_t)length - SHD_CRC8_SIZE) != crc_expected)
    {
        shd_log(core, "crc mismatch");
        status = SHD_STATUS_CRC_MISMATCH;
        (void)shd_send_reply(core, frame_id, shd_error_reply_type(status), NULL, 0u);
        return status;
    }

    status = shd_execute_request(core,
                                 req_type,
                                 body + SHD_REQ_TYPE_OFFSET + 1u,
                                 (size_t)length - SHD_FRAME_ID_SIZE - 1u - SHD_CRC8_SIZE,
                                 &reply);

    if (status == SHD_STATUS_OK)
    {
        status = shd_send_reply(core, frame_id, reply.type, reply.payload, reply.payload_len);
        if (status == SHD_STATUS_OK && core->pending_reset != 0u)
        {
            const uint8_t enter_bootloader = core->pending_bootloader;
            core->pending_reset = 0u;
            core->pending_bootloader = 0u;
            core->deps.reset.reboot(core->deps.reset.ctx, enter_bootloader);
        }
        else
        {
            core->pending_reset = 0u;
            core->pending_bootloader = 0u;
        }
        return status;
    }

    shd_log(core, "request execution failed");
    body[0] = (uint8_t)status;
    (void)shd_send_reply(core, frame_id, SHD_REPLY_OP_ERROR, body, 1u);
    return status;
}
