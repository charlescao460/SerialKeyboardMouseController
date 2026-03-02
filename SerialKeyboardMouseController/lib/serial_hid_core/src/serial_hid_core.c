#include "serial_hid_core.h"

#include <string.h>

#define SHD_DEFAULT_TIMEOUT_MS ((uint32_t)1000u)
#define SHD_CHECKSUM_SIZE ((size_t)1u)

static bool shd_core_deps_valid(const shd_core_deps_t* deps)
{
    return deps != NULL && deps->serial.available != NULL && deps->serial.read_byte != NULL &&
           deps->serial.read_bytes != NULL && deps->serial.write_bytes != NULL &&
           deps->keyboard.press_scan_code != NULL && deps->keyboard.release_scan_code != NULL &&
           deps->keyboard.release_all != NULL && deps->rel_mouse.move != NULL &&
           deps->rel_mouse.scroll != NULL && deps->rel_mouse.press != NULL &&
           deps->rel_mouse.release != NULL && deps->abs_mouse.move != NULL &&
           deps->abs_mouse.change_resolution != NULL && deps->checksum.compute != NULL &&
           deps->clock.now_ms != NULL;
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

static shd_status_t shd_read_exact(shd_core_t* core, uint8_t* dst, size_t len)
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
            return SHD_STATUS_TIMEOUT;
        }
    }

    return SHD_STATUS_OK;
}

static bool shd_checksum_ok(const shd_core_t* core,
                            const uint8_t* data,
                            size_t data_len,
                            const uint8_t* expected,
                            size_t expected_len)
{
    if (core->deps.checksum.checksum_ok != NULL)
    {
        return core->deps.checksum.checksum_ok(
            core->deps.checksum.ctx, data, data_len, expected, expected_len);
    }

    uint8_t computed[8];
    size_t computed_len =
        core->deps.checksum.compute(core->deps.checksum.ctx, data, data_len, computed, sizeof(computed));

    if (computed_len != expected_len)
    {
        return false;
    }
    return memcmp(computed, expected, expected_len) == 0;
}

static uint16_t shd_read_u16_le(const uint8_t* src)
{
    return (uint16_t)(((uint16_t)src[0]) | ((uint16_t)src[1] << 8u));
}

static int16_t shd_read_i16_le(const uint8_t* src)
{
    return (int16_t)shd_read_u16_le(src);
}

static shd_status_t shd_execute_frame(shd_core_t* core, const uint8_t* data, size_t data_len)
{
    if (data_len == 0u)
    {
        return SHD_STATUS_INVALID_PAYLOAD;
    }

    switch ((shd_frame_type_t)data[0])
    {
    case SHD_FRAME_REL_MOUSE_MOVE:
    {
        if (data_len != 5u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        const int16_t dx16 = shd_read_i16_le(data + 1u);
        const int16_t dy16 = shd_read_i16_le(data + 3u);
        if (dx16 < -128 || dx16 > 127 || dy16 < -128 || dy16 > 127)
        {
            return SHD_STATUS_OUT_OF_RANGE;
        }
        core->deps.rel_mouse.move(core->deps.rel_mouse.ctx, (int8_t)dx16, (int8_t)dy16);
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_MOUSE_MOVE_ABS:
    {
        if (data_len != 5u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        const uint16_t x = shd_read_u16_le(data + 1u);
        const uint16_t y = shd_read_u16_le(data + 3u);
        if (x == 0u || y == 0u || x > core->current_resolution_width ||
            y > core->current_resolution_height)
        {
            return SHD_STATUS_OUT_OF_RANGE;
        }
        core->deps.abs_mouse.move(core->deps.abs_mouse.ctx, x, y);
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_MOUSE_SCROLL:
    {
        if (data_len != 2u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        core->deps.rel_mouse.scroll(core->deps.rel_mouse.ctx, (int8_t)data[1]);
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_MOUSE_PRESS:
    {
        if (data_len != 2u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        core->deps.rel_mouse.press(core->deps.rel_mouse.ctx, data[1]);
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_MOUSE_RELEASE:
    {
        if (data_len != 2u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        if (data[1] == SHD_RELEASE_ALL_KEYS)
        {
            const uint8_t all_buttons =
                (uint8_t)(SHD_MOUSE_BTN_LEFT | SHD_MOUSE_BTN_RIGHT | SHD_MOUSE_BTN_MIDDLE);
            core->deps.rel_mouse.release(core->deps.rel_mouse.ctx, all_buttons);
        }
        else
        {
            core->deps.rel_mouse.release(core->deps.rel_mouse.ctx, data[1]);
        }
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_MOUSE_RESOLUTION:
    {
        if (data_len != 5u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        const uint16_t width = shd_read_u16_le(data + 1u);
        const uint16_t height = shd_read_u16_le(data + 3u);
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
        if (data_len != 2u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        core->deps.keyboard.press_scan_code(core->deps.keyboard.ctx, data[1]);
        return SHD_STATUS_OK;
    }
    case SHD_FRAME_KEY_RELEASE:
    {
        if (data_len != 2u)
        {
            return SHD_STATUS_INVALID_PAYLOAD;
        }

        if (data[1] == SHD_RELEASE_ALL_KEYS)
        {
            core->deps.keyboard.release_all(core->deps.keyboard.ctx);
        }
        else
        {
            core->deps.keyboard.release_scan_code(core->deps.keyboard.ctx, data[1]);
        }
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
    shd_status_t status = SHD_STATUS_OK;
    uint8_t* data = NULL;

    if (core == NULL)
    {
        return SHD_STATUS_BAD_DEPENDENCY;
    }
    if (!shd_core_deps_valid(&core->deps))
    {
        shd_log(core, "bad dependency");
        return SHD_STATUS_BAD_DEPENDENCY;
    }

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
            return SHD_STATUS_INVALID_START;
        }
        core->frame_buffer[0] = (uint8_t)start;
    }

    status = shd_read_exact(core, &length, 1u);
    if (status != SHD_STATUS_OK)
    {
        return status;
    }
    if (length == 0u || (size_t)length > SHD_MAX_DATA_LENGTH)
    {
        shd_log(core, "invalid frame length");
        return SHD_STATUS_INVALID_LENGTH;
    }
    core->frame_buffer[1] = length;

    status = shd_read_exact(core, core->frame_buffer + 2u, (size_t)length);
    if (status != SHD_STATUS_OK)
    {
        return status;
    }

    data = core->frame_buffer + 2u;
    if (length <= SHD_CHECKSUM_SIZE)
    {
        shd_log(core, "invalid checksum length");
        return SHD_STATUS_INVALID_LENGTH;
    }

    if (!shd_checksum_ok(core, data, (size_t)length - SHD_CHECKSUM_SIZE, data + (size_t)length - 1u,
                         SHD_CHECKSUM_SIZE))
    {
        shd_log(core, "checksum mismatch");
        return SHD_STATUS_CHECKSUM_MISMATCH;
    }

    status = shd_execute_frame(core, data, (size_t)length - SHD_CHECKSUM_SIZE);
    if (status != SHD_STATUS_OK)
    {
        shd_log(core, "frame execution failed");
        return status;
    }

    if (core->deps.serial.write_bytes(core->deps.serial.ctx, core->frame_buffer, (size_t)length + 2u) !=
        (size_t)length + 2u)
    {
        shd_log(core, "ack write failed");
        return SHD_STATUS_IO_ERROR;
    }

    return SHD_STATUS_OK;
}
