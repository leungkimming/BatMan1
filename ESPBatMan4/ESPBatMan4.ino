#include <Arduino.h>
#include <BLEDevice.h>
#include <BLEUtils.h>
#include <BLEServer.h>
#include <BLE2902.h>
#include "Preferences.h"

// YMKH16D continuous current measurement with calibrated constants
constexpr int HALL_PIN = GPIO_NUM_15; // Yellow wire (Vout)
constexpr int VBAT_PIN = GPIO_NUM_26; // Battery voltage divider pin
constexpr float VZERO_V = 2.055300f;  // New baseline from flipped tests
constexpr float G_V_PER_A = 0.1905f;  // Slope from regression
constexpr uint16_t AVERAGE_SAMPLES = 128;
constexpr float DIVIDER_RATIO = 11.0f;

#define DEVICE_ID      "ESPBatMon2" // ESPBatMon2=R010; ESPBatMon1=R002
#define SERVICE_UUID   "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define Update_UUID    "beb5483e-36e1-4688-b7f5-ea07361b26a8"
#define Announce_UUID  "aa337802-6aca-42a5-b34d-a8d9fc12a266"
#define inActive_Sec   60 //60 x 5 = 5 minute no current; shut down ESP32 to save power
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

Preferences preferences;

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
        int32_mWH00 = temp * 1000 * 100;;
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

void blink(int times) {
  for (int i=times; i > 0; i--) {
    digitalWrite(LED_BUILTIN,HIGH);
    delay(100);
    digitalWrite(LED_BUILTIN,LOW);
    delay(100);
  }
  delay(300);
}

void setup() {
  Serial.begin(115200);
  pinMode(LED_BUILTIN,OUTPUT);
  Serial.println("Setup");
  blink(1);
  
  pinMode(HALL_PIN, INPUT_PULLUP);
  
  ymkh16d.init();
  Serial.println("YMKH16D Setup");
  blink(2);
  
  preferences.begin("batman v3", false);
  int32_mWH00 = preferences.getUInt("mWH00", 0);
  Serial.print("Meter: ");
  Serial.println(int32_mWH00);
  blink(3);
 
  BLEDevice::init(DEVICE_ID);
  pServer = BLEDevice::createServer();
  pServer->setCallbacks(new ServerCallbacks());
  
  pService = pServer->createService(SERVICE_UUID);
  pWR = pService->createCharacteristic(  Update_UUID,
                                         BLECharacteristic::PROPERTY_READ |
                                         BLECharacteristic::PROPERTY_WRITE |
                                         BLECharacteristic::PROPERTY_NOTIFY |
                                         BLECharacteristic::PROPERTY_INDICATE
                                      );
  pWR->setCallbacks(new WHCallbacks());
  pA = pService->createCharacteristic(   Announce_UUID,
                                         BLECharacteristic::PROPERTY_READ |
                                         BLECharacteristic::PROPERTY_NOTIFY
                                     );
  
  BLEDescriptor *pDescriptor = new BLEDescriptor((uint16_t)0x2902);
  pA->addDescriptor(pDescriptor);
  
  pService->start();
  
  BLEAdvertising *pAdvertising = pServer->getAdvertising();
  pAdvertising->start();

  blink(4);
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

void loop() {
  digitalWrite(LED_BUILTIN,HIGH);
  delay(600);
  
  int16_mV = ymkh16d.getBusVoltage_V() * 1000.00;
  delay(500);
  int16_mA = ymkh16d.getCurrent_mA() * -1;
  delay(500);
  int32_mW00 = (static_cast<int64_t>(int16_mV) * static_cast<int64_t>(int16_mA)) / 10;
  
  // meter: Charging add, under load minus
  tempint32 = int32_mW00 * 5.00000 / 60.00000 / 60.00000; // 5 seconds /60/60 = per hour
  int32_mWH00 += tempint32;

  setBLEbuffer(0, int16_mV);
  delay(350);
  setBLEbuffer(2, int16_mA);
  delay(400);
  setBLEbuffer32(4, int32_mW00);
  delay(400);
  digitalWrite(LED_BUILTIN,LOW);

  delay(2000);
  setBLEbuffer32(8, int32_mWH00);
  
  if (tempData[0] != 255) { //not corrupt, send data
    pA->setValue(tempData, 12);
    pA->notify();
  }
  delay(246);  
  
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

  if (inActive_Sec_count == inActive_Sec) {
    Serial.println("No current Sleep");
    preferences.putUInt("mWH00", int32_mWH00);
    esp_sleep_enable_ext0_wakeup((gpio_num_t)HALL_PIN, 0); //1 = High, 0 = Low
    esp_deep_sleep_start();
  }
}
