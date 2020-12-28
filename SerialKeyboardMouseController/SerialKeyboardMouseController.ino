/*
 Name:		SerialKeyboardMouseController.ino
 Created:	2020/12/28 4:23:35
 Author:	CSR
*/

#include <Arduino.h>

#define _DEBUG
#include "debug_print.h"

/****************************** Settings ******************************/
constexpr unsigned long BAUD_RATE = 500000;
HardwareSerial& ControlSerial = Serial1;

// the setup function runs once when you press reset or power the board
void setup()
{
#ifdef _DEBUG
    Serial.begin(115200);
#endif
    ControlSerial.begin(BAUD_RATE);
}

// the loop function runs over and over again until power down or reset
void loop()
{
    
}
