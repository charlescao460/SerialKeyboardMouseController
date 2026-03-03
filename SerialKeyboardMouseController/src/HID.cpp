#include "HID.h"

#if defined(USBCON)

HID_& HID()
{
    static HID_ hid;
    return hid;
}

HID_::HID_()
    : ShdPluggableUsbModule(1, 1, epType_)
    , root_node_(NULL)
    , descriptor_size_(0)
    , protocol_(HID_REPORT_PROTOCOL)
    , idle_(1u)
    , keyboard_leds_(0u)
    , keyboard_output_handler_(NULL)
{
#if defined(ARDUINO_ARCH_AVR)
    epType_[0] = EP_TYPE_INTERRUPT_IN;
#elif defined(ARDUINO_ARCH_SAMD)
    epType_[0] = USB_ENDPOINT_TYPE_INTERRUPT | USB_ENDPOINT_IN(0);
#endif
    PluggableUSB().plug(this);
}

int HID_::begin()
{
    return 0;
}

void HID_::AppendDescriptor(HIDSubDescriptor* node)
{
    if (node == NULL)
    {
        return;
    }

    if (root_node_ == NULL)
    {
        root_node_ = node;
    }
    else
    {
        HIDSubDescriptor* current = root_node_;
        while (current->next != NULL)
        {
            current = current->next;
        }
        current->next = node;
    }

    descriptor_size_ = (uint16_t)(descriptor_size_ + node->length);
}

int HID_::SendReport(uint8_t id, const void* data, int len)
{
#if defined(ARDUINO_ARCH_AVR)
    int sent = USB_Send(pluggedEndpoint, &id, 1);
    if (sent < 0)
    {
        return sent;
    }

    {
        int body = USB_Send(pluggedEndpoint | TRANSFER_RELEASE, data, len);
        if (body < 0)
        {
            return body;
        }
        sent += body;
    }

    return sent;
#elif defined(ARDUINO_ARCH_SAMD)
    int sent = (int)USBDevice.send(pluggedEndpoint, &id, 1u);
    if (sent < 0)
    {
        return sent;
    }
    {
        int body = (int)USBDevice.send(pluggedEndpoint, data, (uint32_t)len);
        if (body < 0)
        {
            return body;
        }
        sent += body;
    }
    return sent;
#else
    (void)id;
    (void)data;
    (void)len;
    return -1;
#endif
}

int HID_::SendBootKeyboardReport(const void* data, int len)
{
#if defined(ARDUINO_ARCH_AVR)
    return USB_Send(pluggedEndpoint | TRANSFER_RELEASE, data, len);
#elif defined(ARDUINO_ARCH_SAMD)
    return (int)USBDevice.send(pluggedEndpoint, data, (uint32_t)len);
#else
    (void)data;
    (void)len;
    return -1;
#endif
}

void HID_::setKeyboardOutputHandler(HidKeyboardOutputHandler handler)
{
    keyboard_output_handler_ = handler;
}

uint8_t HID_::keyboardLedState() const
{
    return keyboard_leds_;
}

uint8_t HID_::keyboardProtocol() const
{
    return protocol_;
}

uint8_t HID_::hostStatusFlags() const
{
    uint8_t flags = 0u;
#if defined(ARDUINO_ARCH_AVR)
    if (USBDevice.configured())
    {
        flags |= SHD_HOST_STATUS_CONFIGURED;
    }
    if (USBDevice.isSuspended())
    {
        flags |= SHD_HOST_STATUS_SUSPENDED;
    }
    if (USBDevice.configured() && !USBDevice.isSuspended())
    {
        flags |= SHD_HOST_STATUS_SOF_ACTIVE;
    }
#elif defined(ARDUINO_ARCH_SAMD)
    if (USBDevice.configured())
    {
        flags |= SHD_HOST_STATUS_CONFIGURED;
    }
    if (USBDevice.connected())
    {
        flags |= SHD_HOST_STATUS_SOF_ACTIVE;
    }
    if (USBDevice.configured() && !USBDevice.connected())
    {
        flags |= SHD_HOST_STATUS_SUSPENDED;
    }
#endif
    return flags;
}

int HID_::getInterface(uint8_t* interfaceCount)
{
    uint16_t packet_size = 0u;
#if defined(ARDUINO_ARCH_AVR)
    packet_size = USB_EP_SIZE;
#else
    packet_size = EPX_SIZE;
#endif

    HIDDescriptor hid_interface = {
        D_INTERFACE(pluggedInterface, 1, USB_DEVICE_CLASS_HUMAN_INTERFACE, 1, 1),
        D_HIDREPORT(descriptor_size_),
        D_ENDPOINT(USB_ENDPOINT_IN(pluggedEndpoint), USB_ENDPOINT_TYPE_INTERRUPT, packet_size, 1)};

    *interfaceCount = (uint8_t)(*interfaceCount + 1u);
#if defined(ARDUINO_ARCH_AVR)
    return USB_SendControl(0, &hid_interface, sizeof(hid_interface));
#else
    return (int)USBDevice.sendControl(&hid_interface, sizeof(hid_interface));
#endif
}

int HID_::getDescriptor(ShdUsbSetup& setup)
{
    int total = 0;

    if (setup.bmRequestType != REQUEST_DEVICETOHOST_STANDARD_INTERFACE)
    {
        return 0;
    }
    if (setup.wValueH != HID_REPORT_DESCRIPTOR_TYPE)
    {
        return 0;
    }
    if (setup.wIndex != pluggedInterface)
    {
        return 0;
    }

    for (HIDSubDescriptor* node = root_node_; node != NULL; node = node->next)
    {
#if defined(ARDUINO_ARCH_AVR)
        int sent = USB_SendControl(TRANSFER_PGM, node->data, node->length);
#else
        int sent = (int)USBDevice.sendControl(node->data, node->length);
#endif
        if (sent < 0)
        {
            return -1;
        }
        total += sent;
    }

    protocol_ = HID_REPORT_PROTOCOL;
    return total;
}

bool HID_::setup(ShdUsbSetup& setup)
{
    int length = 0;

    if (setup.wIndex != pluggedInterface)
    {
        return false;
    }

    if (setup.bmRequestType == REQUEST_DEVICETOHOST_CLASS_INTERFACE)
    {
        if (setup.bRequest == HID_GET_PROTOCOL)
        {
#if defined(ARDUINO_ARCH_AVR)
            USB_SendControl(0, &protocol_, 1);
#else
            USBDevice.sendControl(&protocol_, 1);
#endif
            return true;
        }
        if (setup.bRequest == HID_GET_IDLE)
        {
#if defined(ARDUINO_ARCH_AVR)
            USB_SendControl(0, &idle_, 1);
#else
            USBDevice.sendControl(&idle_, 1);
#endif
            return true;
        }
        if (setup.bRequest == HID_GET_REPORT)
        {
            // Not implemented by this device.
            return false;
        }
    }

    if (setup.bmRequestType == REQUEST_HOSTTODEVICE_CLASS_INTERFACE)
    {
        if (setup.bRequest == HID_SET_PROTOCOL)
        {
            protocol_ = setup.wValueL;
            return true;
        }
        if (setup.bRequest == HID_SET_IDLE)
        {
            idle_ = setup.wValueL;
            return true;
        }
        if (setup.bRequest == HID_SET_REPORT)
        {
            uint8_t raw[8];
            uint8_t parsed[2] = {0u, 0u};
            int parsed_len = 0;
            length = setup.wLength;

            while (length > 0)
            {
                int chunk = length;
                if (chunk > (int)sizeof(raw))
                {
                    chunk = (int)sizeof(raw);
                }
#if defined(ARDUINO_ARCH_AVR)
                USB_RecvControl(raw, chunk);
#else
                USBDevice.recvControl(raw, (uint32_t)chunk);
#endif

                {
                    int i = 0;
                    for (i = 0; i < chunk && parsed_len < 2; ++i)
                    {
                        parsed[parsed_len++] = raw[i];
                    }
                }

                length -= chunk;
            }

            if (((uint8_t)setup.wValueH) == HID_REPORT_TYPE_OUTPUT && parsed_len > 0)
            {
                uint8_t leds = parsed[0];
                if (setup.wValueL != 0u && parsed_len > 1)
                {
                    leds = parsed[1];
                }
                keyboard_leds_ = leds;
                if (keyboard_output_handler_ != NULL)
                {
                    keyboard_output_handler_(keyboard_leds_);
                }
            }
            return true;
        }
    }

    return false;
}

uint8_t HID_::getShortName(char* name)
{
    name[0] = 'H';
    name[1] = 'I';
    name[2] = 'D';
    name[3] = '0' + ((descriptor_size_ >> 4) & 0x0Fu);
    name[4] = '0' + (descriptor_size_ & 0x0Fu);
    return 5;
}

#endif // USBCON
