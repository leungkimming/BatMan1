#include <Arduino.h>
#include <BLEDevice.h>
#include <BLEUtils.h>
#include <BLEServer.h>
#include <BLE2902.h>
#include "Preferences.h"
#include "batmanble.h"

// YMKH16D continuous current measurement with calibrated constants
constexpr int HALL_PIN = GPIO_NUM_2; // Yellow wire (Vout)
constexpr int VBAT_PIN = GPIO_NUM_1; // Battery voltage divider pin
constexpr float VZERO_V = 2.055300f;  // New baseline from flipped tests
constexpr float G_V_PER_A = 0.1905f;  // Slope from regression from calibration
constexpr uint16_t AVERAGE_SAMPLES = 128;
constexpr float DIVIDER_RATIO = 11.0f; //30v with 100k and 10k divider

#define DEVICE_ID      "ESPBatMon5" // Hall effect YMKH16 10A 2V
#define SERVICE_UUID   "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define Update_UUID    "beb5483e-36e1-4688-b7f5-ea07361b26a8"
#define Announce_UUID  "aa337802-6aca-42a5-b34d-a8d9fc12a266"
#define IdlemA         50 //idle current 50mA; shut down ESP32 to save power

uint8_t tempData[14];
BLECharacteristic* pWR;
BLECharacteristic* pA;
BLEService* pService;
BLEServer* pServer;

uint8_t inActive_Sec_count = 0;
uint32_t int32_mWH00 = 0;
uint16_t int16_mV = 0;
int16_t int16_mA = 0;
int32_t int32_mW00 = 0; //mW * 100 2 decimal place
int32_t tempint32 = 0;

static unsigned long lastBlePublishMs = 0;
static const unsigned long BLE_PUBLISH_INTERVAL_MS = 5000;

extern Preferences preferences;

float readVoltageAverageV(int pin, uint16_t samples) {
  uint32_t sumMv = 0;
  for (uint16_t i = 0; i < samples; i++) {
    sumMv += static_cast<uint32_t>(analogReadMilliVolts(pin));
    delay(2);
  }
  return (sumMv / static_cast<float>(samples)) / 1000.0f;
}

class YMKH16D_Sensor {
public:
  void init() {
    analogReadResolution(12);
    analogSetAttenuation(ADC_11db);
  }

  // Returns battery voltage in Volts
  float getBusVoltage_V() {
    float vBatPin = readVoltageAverageV(VBAT_PIN, AVERAGE_SAMPLES);
    return vBatPin * DIVIDER_RATIO;
  }

  // Returns current in milliamps (positive = discharge, negative = charge)
  float getCurrent_mA() {
    float vOut = readVoltageAverageV(HALL_PIN, AVERAGE_SAMPLES);
    float deltaV = VZERO_V - vOut; // Reversed math for flipped wire
    float i_A = deltaV / G_V_PER_A;
    float i_mA = i_A * 1000.0f;

    // Add 0A deadzone below 40mA to hide noise
    if (abs(i_mA) < 40.0f) {
      return 0.0f;
    }
    return i_mA;
  }

  // Returns power in Watts (positive = discharging, negative = charging)
  float getBusPower() {
    float v = getBusVoltage_V();
    float i_A = getCurrent_mA() / 1000.0f;
    return v * i_A;
  }
};

YMKH16D_Sensor ymkh16d;

class WHCallbacks: public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic *pCharacteristic) {
      String rxValue = pCharacteristic->getValue();

      if (rxValue.length() > 0) {
        Serial.print("Received Value: ");
        uint16_t temp = (int) rxValue[1] << 8;
        temp = temp + (int) rxValue[0];
        Serial.println(temp);
        bleSetWhRemainingWh(temp);
        Serial.println(int32_mWH00);
      }
    };
    void onRead(BLECharacteristic *pCharacteristic) {
        pCharacteristic->setValue("OK");
    };
};

class ServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer *pServer) {
        Serial.println("Connected");
    }
    void onDisconnect(BLEServer *pServer) {
        Serial.println("Disconnected");
        BLEAdvertising *pAdvertising = pServer->getAdvertising();
        pAdvertising->start();
    }
};

void bleSetup() {
  pinMode(LED_BUILTIN,OUTPUT);
  Serial.println("Setup");
  Serial.flush();
  
  pinMode(HALL_PIN, INPUT_PULLUP);
  
  ymkh16d.init();
  
  preferences.begin("batman v3", false);
  int32_mWH00 = preferences.getUInt("mWH00", 0);
  Serial.print("Meter: ");
  Serial.println(int32_mWH00);
 
  memset(tempData, 0, sizeof(tempData));

  BLEDevice::init(DEVICE_ID);
  pServer = BLEDevice::createServer();
  pServer->setCallbacks(new ServerCallbacks());
  
  pService = pServer->createService(SERVICE_UUID);
  pWR = pService->createCharacteristic(  Update_UUID,
                                         BLECharacteristic::PROPERTY_READ |
                                         BLECharacteristic::PROPERTY_WRITE
                                      );
  pWR->setCallbacks(new WHCallbacks());
  pA = pService->createCharacteristic(   Announce_UUID,
                                         BLECharacteristic::PROPERTY_READ |
                                         BLECharacteristic::PROPERTY_NOTIFY
                                     );
  
  BLE2902 *pDescriptor = new BLE2902();
  pA->addDescriptor(pDescriptor);
  pA->setValue(tempData, 12);
  
  pService->start();
  
  BLEAdvertising *pAdvertising = pServer->getAdvertising();
  pAdvertising->start();

  lastBlePublishMs = millis();
}

void setBLEbuffer(int index, int value) {
  tempData[index] = value;
  tempData[index + 1] = value>>8;
}

void setBLEbuffer32(int index, uint32_t value) {
  tempData[index] = value;
  tempData[index + 1] = value>>8;
  tempData[index + 2] = value>>16;
  tempData[index + 3] = value>>24;
}

void printserial(String name, String unit, float value, int places) {
  Serial.print(name + ": ");
  Serial.print(value, places);
  Serial.println(unit);
}

void bleLoop() {
  unsigned long now = millis();

  if (now - lastBlePublishMs < BLE_PUBLISH_INTERVAL_MS) {
    return;
  }

  lastBlePublishMs = now;

  int16_mV = ymkh16d.getBusVoltage_V() * 1000.00;
  int16_mA = ymkh16d.getCurrent_mA() * -1;
  int32_mW00 = (static_cast<int64_t>(int16_mV) * static_cast<int64_t>(int16_mA)) / 10;
  
  // meter: Charging add, under load minus
  tempint32 = int32_mW00 * 5.00000 / 60.00000 / 60.00000; // 5 seconds /60/60 = per hour
  int32_mWH00 += tempint32;

  setBLEbuffer(0, int16_mV);
  setBLEbuffer(2, int16_mA);
  setBLEbuffer32(4, int32_mW00);
  setBLEbuffer32(8, int32_mWH00);
  
  if (tempData[0] != 255 && pServer != nullptr && pServer->getConnectedCount() > 0) { //not corrupt, send data
    pA->setValue(tempData, 12);
    pA->notify();
  }
  
  printserial("Voltage", "V", int16_mV / 1000.00, 3);
  printserial("Current", "A", int16_mA / 1000.0000, 3);
  printserial("Watts", "W", int32_mW00 / 100000.0000, 5);
  printserial("WH remaining", "WH", int32_mWH00 / 100000.000000, 7);
  printserial("No current", " Sec", inActive_Sec_count * 5, 0);
  printserial("elapsed", " Sec", millis() / 1000.0000, 3);
  Serial.println("----------");

  if (abs(int16_mA) < IdlemA) { //idle current
    inActive_Sec_count += 1; //count continue seconds after sleeping
  } else {
    inActive_Sec_count = 0; // has current, activated
  }
}

void blePersistState() {
  preferences.putUInt("mWH00", int32_mWH00);
}

void bleSetWhRemainingWh(uint16_t wh) {
  int32_mWH00 = static_cast<uint32_t>(wh) * 1000 * 100;
}

uint8_t bleGetIdleCount() {
  return inActive_Sec_count;
}

uint16_t bleGetVoltageMv() {
  return int16_mV;
}

int16_t bleGetCurrentMa() {
  return int16_mA;
}

int32_t bleGetPowerMw() {
  return int32_mW00;
}

uint32_t bleGetWhRemaining() {
  return int32_mWH00;
}

