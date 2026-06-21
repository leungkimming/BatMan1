#ifndef BATMANBLE_H
#define BATMANBLE_H

#include <Arduino.h>

void bleSetup();
void bleLoop();
void blePersistState();
void bleSetWhRemainingWh(uint16_t wh);
uint8_t bleGetIdleCount();
uint16_t bleGetVoltageMv();
int16_t bleGetCurrentMa();
int32_t bleGetPowerMw();
uint32_t bleGetWhRemaining();

#endif
