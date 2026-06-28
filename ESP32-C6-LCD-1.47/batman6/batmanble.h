#ifndef BATMANBLE_H
#define BATMANBLE_H

#include <Arduino.h>

void bleSetup();
bool bleLoop();
void blePersistState();
void bleSetWhRemainingWh(uint16_t wh);
float bleReadZeroBase_V();
void bleSetZeroBase_V(float newZeroBase_V);
uint8_t bleGetIdleCount();
uint16_t bleGetVoltageMv();
int16_t bleGetCurrentMa();
int32_t bleGetPowerMw();
uint32_t bleGetWhRemaining();

#endif
