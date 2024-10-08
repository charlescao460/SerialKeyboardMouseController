#ifndef DEBUG_PRINT_H_
#define DEBUG_PRINT_H_

#include <Arduino.h>
#include <USBDesc.h>

//#define _DEBUG

#if defined(_DEBUG) && defined(CDC_ENABLED)

#define debug_print(arg) do{ Serial.print(arg); }while(0);
#define debug_println(arg) do{ Serial.println(arg); }while(0);

#else

#define debug_print(arg) do{ }while(0);
#define debug_println(arg) do{ }while(0);

#endif

#endif

