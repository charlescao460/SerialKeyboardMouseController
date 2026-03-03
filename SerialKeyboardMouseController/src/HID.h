#ifndef HID_h
#define HID_h

#include <Arduino.h>

#if defined(USBCON)
#define _USING_HID

#if defined(ARDUINO_ARCH_AVR)
#include "PluggableUSB.h"
#include "USBAPI.h"
#include "USBCore.h"
typedef USBSetup ShdUsbSetup;
typedef PluggableUSBModule ShdPluggableUsbModule;
typedef uint8_t ShdEndpointType;
#elif defined(ARDUINO_ARCH_SAMD)
#include "api/PluggableUSB.h"
#include "USB/USBAPI.h"
#include "USB/USBCore.h"
typedef arduino::USBSetup ShdUsbSetup;
typedef arduino::PluggableUSBModule ShdPluggableUsbModule;
typedef unsigned int ShdEndpointType;
#endif

#include "serial_hid_types.h"

#define HID_GET_REPORT 0x01u
#define HID_GET_IDLE 0x02u
#define HID_GET_PROTOCOL 0x03u
#define HID_SET_REPORT 0x09u
#define HID_SET_IDLE 0x0Au
#define HID_SET_PROTOCOL 0x0Bu

#define HID_REPORT_DESCRIPTOR_TYPE 0x22u

#define HID_BOOT_PROTOCOL 0u
#define HID_REPORT_PROTOCOL 1u

#define HID_REPORT_TYPE_OUTPUT 2u

typedef struct
{
    uint8_t len;
    uint8_t dtype;
    uint8_t addr;
    uint8_t versionL;
    uint8_t versionH;
    uint8_t country;
    uint8_t desctype;
    uint8_t descLenL;
    uint8_t descLenH;
} HIDDescDescriptor;

typedef struct
{
    InterfaceDescriptor hid;
    HIDDescDescriptor desc;
    EndpointDescriptor in;
} HIDDescriptor;

class HIDSubDescriptor
{
public:
    HIDSubDescriptor* next;
    const void* data;
    const uint16_t length;

    HIDSubDescriptor(const void* d, uint16_t l)
        : next(NULL)
        , data(d)
        , length(l)
    {
    }
};

typedef void (*HidKeyboardOutputHandler)(uint8_t leds);

class HID_ : public ShdPluggableUsbModule
{
public:
    HID_();

    int begin();
    int SendReport(uint8_t id, const void* data, int len);
    int SendBootKeyboardReport(const void* data, int len);

    void AppendDescriptor(HIDSubDescriptor* node);
    void setKeyboardOutputHandler(HidKeyboardOutputHandler handler);

    uint8_t keyboardLedState() const;
    uint8_t keyboardProtocol() const;
    uint8_t hostStatusFlags() const;

protected:
    int getInterface(uint8_t* interfaceCount) override;
    int getDescriptor(ShdUsbSetup& setup) override;
    bool setup(ShdUsbSetup& setup) override;
    uint8_t getShortName(char* name) override;

private:
    ShdEndpointType epType_[1];
    HIDSubDescriptor* root_node_;
    uint16_t descriptor_size_;
    uint8_t protocol_;
    uint8_t idle_;
    uint8_t keyboard_leds_;
    HidKeyboardOutputHandler keyboard_output_handler_;
};

HID_& HID();

#define D_HIDREPORT(length) { 9, 0x21, 0x01, 0x01, 0, 1, HID_REPORT_DESCRIPTOR_TYPE, lowByte(length), highByte(length) }

#endif // USBCON

#endif // HID_h
