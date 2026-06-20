#include <BLEDevice.h>
#include <BLEUtils.h>
#include <BLEServer.h>
#include <BLE2902.h>
#include "Preferences.h"
#include <Wire.h>
#include <INA226_WE.h>
#define I2C_ADDRESS 0x40

// See the following for generating UUIDs:
// https://www.uuidgenerator.net/

#define DEVICE_ID      "ESPBatMon2" // ESPBatMon2=R010; ESPBatMon1=R002
#define Shunt_resistor 0.010 // R002=0.002; R010=0.010; look at your ina226 shunt resistor
#define max_Amp        10.0  // 10A; your chair's estimated max current; affects the resolution
#define SERVICE_UUID   "4fafc201-1fb5-459e-8fcc-c5c9c331914b"
#define Update_UUID    "beb5483e-36e1-4688-b7f5-ea07361b26a8"
#define Announce_UUID  "aa337802-6aca-42a5-b34d-a8d9fc12a266"
#define inActive_Sec   60 //60 x 5 = 5 minute no current; shut down ESP32 to save power
#define noUse_Sec      12 //12 x 5 = 1 minute App not foreground; obsolete
#define noUseSleepTime 30000000    //30 * 1000000  /* Conversion factor for micro seconds to seconds */; obsolete
#define IdlemA         50 //idle current 50mA; shut down ESP32 to save power
#define AlertmW        1425 //trigger alert watts 50mA * 28.5V * 1000; wake up ESP32 current

#define maxmWH00 34560000 // 12AH * 28V * 1000 (mwH) * 100 2 decimal; obsolete

//1 volt is equal to 1,000,000 microvolt (uV), or 1,000 millivolt (mV).
//1 Ampere is equal to 1,000,000 microampere (uA), or 1,000 milliampere (mA).
uint8_t tempData[14];
BLECharacteristic* pWR;
BLECharacteristic* pA;
BLEService* pService;
BLEServer* pServer;

uint8_t inActive_Sec_count = 0;
uint8_t noUse_Sec_count = 0;
uint32_t int32_mWH00 = 0;
uint16_t int16_mV = 0;
int16_t int16_mA = 0;
int32_t int32_mW00 = 0; //mW * 100 2 decimal place
int32_t tempint32 = 0;

INA226_WE ina226 = INA226_WE(I2C_ADDRESS);

Preferences preferences;

class WHCallbacks: public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic *pCharacteristic) {
      std::string rxValue = pCharacteristic->getValue();

      if (rxValue.length() > 0) {
        Serial.print("Received Value: ");
        uint16_t temp = (int) rxValue[1] << 8;
        temp = temp + (int) rxValue[0];
        Serial.println(temp);
        //uint32_t temp1 = temp * 1000 * 100;
        //Serial.println(temp1);
        int32_mWH00 = temp * 1000 * 100;;
        Serial.println(int32_mWH00);
        //int32_mWH00 = maxmWH00;
      }
    };
    void onRead(BLECharacteristic *pCharacteristic) {
        pCharacteristic->setValue("OK");
//        noUse_Sec_count = 0; //App is in foreground, reset noUse count
//        Serial.println("Read");
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

void setup() {
  Serial.begin(115200);
  pinMode(LED_BUILTIN,OUTPUT);
  Serial.println("Setup");
  blink(1);
  
  pinMode(15, INPUT_PULLUP);
  Wire.begin();
  Serial.println("Wire begin");
  ina226.init();
  ina226.reset_INA226();
  ina226.setResistorRange(Shunt_resistor,max_Amp);
  ina226.setAverage(AVERAGE_1024);
  ina226.setConversionTime(CONV_TIME_1100); //1024 * 1.1ms * 2 = 2.2528 seconds
  ina226.setAlertType(POWER_OVER, AlertmW);
  ina226.readAndClearFlags();
  ina226.waitUntilConversionCompleted(); //if you comment this line the first data might be zero
  Serial.println("ina226 Setup");
  blink(2);
  
  preferences.begin("batman v3", false);
  int32_mWH00 = preferences.getUInt("mWH00", 0);
  // int32_mWH00 = maxmWH00; //set remaining WH
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
  //Make Announcement
  pA = pService->createCharacteristic(   Announce_UUID,
                                         BLECharacteristic::PROPERTY_READ |
                                         BLECharacteristic::PROPERTY_NOTIFY
                                     );
  
// https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.descriptor.gatt.client_characteristic_configuration.xml
// Create a BLE Descriptor
//  BLEDescriptor *pDescriptor = new BLE2902();
//  pA->addDescriptor(pDescriptor);
//  pDescriptor->setValue("SOC data");
  BLEDescriptor *pDescriptor = new BLEDescriptor((uint16_t)0x2902); // Characteristic User Description
  pA->addDescriptor(pDescriptor);
  
  pService->start();
//  pDescriptor->setValue("My Test Characteristic");
//  Serial.print("pDescriptor->getHandle()=0x"); Serial.println(pDescriptor->getHandle(), HEX);
  
  BLEAdvertising *pAdvertising = pServer->getAdvertising();
  pAdvertising->start();

  //Wire.begin();
  //bq->setup(0,0,0,0,0);
  blink(4);
}

void blink(int times) {
  for (int i=times; i > 0; i--) {
    digitalWrite(LED_BUILTIN,HIGH);
    delay(100);
    digitalWrite(LED_BUILTIN,LOW);
    delay(100);
  }
  delay(300);
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
  int16_mV = ina226.getBusVoltage_V() * 1000.00;
  delay(500);
  int16_mA = ina226.getCurrent_mA() * -1;
  delay(500);
  int32_mW00 = ina226.getBusPower() * 100.000;
  if (int16_mA < 0) {
    int32_mW00 = int32_mW00 * -1;
  }
  // meter: Charging add, under load minus
  tempint32 = int32_mW00 * 5.00000 / 60.00000 / 60.00000; // 5 seconds /60/60 = per hour
  int32_mWH00 += tempint32;

  setBLEbuffer(0, int16_mV); //mv bq->getVoltage());
  delay(350);
  setBLEbuffer(2, int16_mA); //bq->getCurrent());
  delay(400);
  setBLEbuffer32(4, int32_mW00); //uw bq->getCapacity());
  delay(400);
  digitalWrite(LED_BUILTIN,LOW);
  //setBLEbuffer(8, bq->getRemaining());
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

//  if (tempData[0] == 255) { //corrupt, reboot
//    Serial.println("corrupted!");
//    Wire.endTransmission();
//    Wire.begin();
//    delay(1000);
//    ina226.init();
//  }
  if (abs(int16_mA) < IdlemA) { //idle current
    inActive_Sec_count += 1; //count continue seconds after sleeping
  } else {
    inActive_Sec_count = 0; // has current, activated
  }

  if (inActive_Sec_count == inActive_Sec) {
    Serial.println("No current Sleep");
    preferences.putUInt("mWH00", int32_mWH00);
    ina226.readAndClearFlags();
    esp_sleep_enable_ext0_wakeup(GPIO_NUM_15,0); //1 = High, 0 = Low
    esp_deep_sleep_start();
  }
}
