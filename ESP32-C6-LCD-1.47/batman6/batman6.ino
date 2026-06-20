//ref: https://www.youtube.com/watch?v=KEcr22qZAVE&t=76s
//document/Arduino/libraries/lvgl/src/lv_conf.h ->
//LV_FONT_MONTSERRAT 18, 14, 20
//Board: EPS32 Dev Module
//Port COM3 (ESP32 Family Device)
//Memory: No OTA (2MB APP/2MB FATFS)
//USB CDC On Boot: "Enabled"
//lvgl: replace C:\Users\Micl\Documents\Arduino\libraries\lvgl_libraries\lvgl
//with C:\Users\Micl\Documents\Arduino\libraries\lvgl_libraries\lvgl_8.3.10

#include "Display_ST7789.h"
#include "LVGL_Driver.h"
#include "ui.h"
//libraries in C:\Users\Micl\AppData\Local\Arduino15\packages\esp32\hardware\esp32
#include "BLEDevice.h"
#include "Preferences.h"

#define RGB_BUILDIN 8
#define RGB_BRIGHTNESS 64
#define BTN_BUILDIN 9
int state=0;
bool pressed=false;
String screen="Home";
bool start=false;
uint32_t int32_mWH00Day = 0;
uint32_t int32_mWH00Now = 0;
Preferences preferences;

static BLEUUID serviceUUID("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
static BLEUUID announceUUID("aa337802-6aca-42a5-b34d-a8d9fc12a266");
static BLEUUID updateUUID("beb5483e-36e1-4688-b7f5-ea07361b26a8");
static boolean doConnect = false;
static boolean connected = false;
static boolean allowReScan = false;
static BLERemoteCharacteristic *pAnnounceCharacteristic;
static BLERemoteCharacteristic *pUpdateCharacteristic;
static BLEAdvertisedDevice *myDevice;

static unsigned long pressStartTime = 0;
static bool buttonPressed = false;
static bool buttonLongPressed = false;
static lv_obj_t *refButton = nullptr;

static void notifyCallback(BLERemoteCharacteristic *pBLEAnnounceCharacteristic, uint8_t *pData, size_t length, bool isNotify) {
  // Serial.print("Notify callback for characteristic ");
  // Serial.print(pBLEAnnounceCharacteristic->getUUID().toString().c_str());
  // Serial.print(" of data length ");
  // Serial.println(length);
  printData(pData, length);
  //Serial.println();
}

static void printData(uint8_t *pData, size_t length) {
  if (length >= 12) { // Ensure the data length is sufficient
    uint16_t received_mV = pData[0] | (pData[1] << 8);
    int16_t received_mA = pData[2] | (pData[3] << 8);
    int32_t received_mW00 = pData[4] | (pData[5] << 8) | (pData[6] << 16) | (pData[7] << 24);
    uint32_t received_mWH00 = pData[8] | (pData[9] << 8) | (pData[10] << 16) | (pData[11] << 24);
    int32_mWH00Now = received_mWH00;

    float voltage_V = received_mV / 1000.0;
    float current_A = received_mA / 1000.0;
    float power_W = received_mW00 / (1000.0 * 100.0);
    float remaining_WH = received_mWH00 / (1000.0 * 100.0);
    // if (received_mW00 < 0) {
    //   int32_mWH00Day -= received_mW00;
    // }
    // float Daily_WH = int32_mWH00Day / (1000.0 * 100.0) * 5 / 60 / 60;
    // subtract the current power from the start of day meter to get the daily consumption
    float Daily_WH = (int32_mWH00Day - int32_mWH00Now) / (1000.0 * 100.0);

    // Serial.print("Received Voltage (V): ");
    // Serial.println(voltage_V, 2);
    // Serial.print("Received Current (A): ");
    // Serial.println(current_A, 2);
    // Serial.print("Received Power (W): ");
    // Serial.println(power_W, 4);
    // Serial.print("Received Energy (H): ");
    // Serial.println(remaining_WH, 4);
    // Serial.print("Daily Energy (H): ");
    // Serial.println(Daily_WH, 4);
    lv_label_set_text(ui_VoltageValue, String(voltage_V,3).c_str());
    lv_label_set_text(ui_CurrentValue, String(current_A,3).c_str());
    lv_label_set_text(ui_WattsValue, String(power_W,3).c_str());
    lv_label_set_text(ui_MWRemainingValue, String(remaining_WH,4).c_str());
    lv_label_set_text(ui_DailyValue, String(Daily_WH,3).c_str());
    Timer_Loop();
    lv_arc_set_value(ui_WHRemainingARC, remaining_WH);
  } else {
    Serial.println("Error: Insufficient data length");
  }
}

class MyClientCallback : public BLEClientCallbacks {
  void onConnect(BLEClient *pclient) {}

  void onDisconnect(BLEClient *pclient) {
    preferences.putUInt("mWH00Day", int32_mWH00Day); //save daily meter
    screen="Home";
    state=0;
    lv_disp_load_scr(ui_Home);
    Serial.println("onDisconnect");
    connected = false;
  }
};

bool connectToServer() {
  Serial.print("Forming a connection to ");
  rgbLedWrite(RGB_BUILTIN, 0, 0, RGB_BRIGHTNESS);
  Serial.println(myDevice->getAddress().toString().c_str());

  BLEClient *pClient = BLEDevice::createClient();
  Serial.println(" - Created client");

  pClient->setClientCallbacks(new MyClientCallback());

  // Connect to the remove BLE Server.
  pClient->connect(myDevice);  // if you pass BLEAdvertisedDevice instead of address, it will be recognized type of peer device address (public or private)
  Serial.println(" - Connected to server");
  pClient->setMTU(517);  //set client to request maximum MTU from server (default is 23 otherwise)

  // Obtain a reference to the service we are after in the remote BLE server.
  BLERemoteService *pRemoteService = pClient->getService(serviceUUID);
  if (pRemoteService == nullptr) {
    Serial.print("Failed to find our service UUID: ");
    Serial.println(serviceUUID.toString().c_str());
    pClient->disconnect();
    return false;
  }
  Serial.println(" - Found our service");

  // Obtain a reference to the characteristic in the service of the remote BLE server.
  pAnnounceCharacteristic = pRemoteService->getCharacteristic(announceUUID);
  if (pAnnounceCharacteristic == nullptr) {
    Serial.print("Failed to find our characteristic UUID: ");
    Serial.println(announceUUID.toString().c_str());
    pClient->disconnect();
    return false;
  }
  Serial.println(" - Found our announce characteristic");

  // Read the value of the characteristic.
  if (!pAnnounceCharacteristic->canRead()) {
    Serial.print("Failed to read characteristic UUID:: ");
    Serial.println(announceUUID.toString().c_str());
    pClient->disconnect();
    return false;
  }

  if (pAnnounceCharacteristic->canNotify()) {
    pAnnounceCharacteristic->registerForNotify(notifyCallback);
  } else {
    Serial.print("Failed to registerForNotify characteristic UUID:: ");
    Serial.println(announceUUID.toString().c_str());
    pClient->disconnect();
    return false;
  }

  pUpdateCharacteristic = pRemoteService->getCharacteristic(updateUUID);
  if (pUpdateCharacteristic == nullptr) {
    Serial.print("Failed to find our update characteristic UUID: ");
    Serial.println(updateUUID.toString().c_str());
    pClient->disconnect();
    return false;
  }
  Serial.println(" - Found our update characteristic");
  // Read the value of the characteristic.
  if (!pUpdateCharacteristic->canWrite()) {
    Serial.print("Failed to write to characteristic UUID:: ");
    Serial.println(updateUUID.toString().c_str());
    pClient->disconnect();
    return false;
  }

  connected = true;
  return true;
}
/**
 * Scan for BLE servers and find the first one that advertises the service we are looking for.
 */
class MyAdvertisedDeviceCallbacks : public BLEAdvertisedDeviceCallbacks {
  /**
   * Called for each advertising BLE server.
   */
  void onResult(BLEAdvertisedDevice advertisedDevice) {
    Serial.print("BLE Advertised Device found: ");
    rgbLedWrite(RGB_BUILTIN, 0, RGB_BRIGHTNESS, 0);
    Serial.println(advertisedDevice.toString().c_str());
    if (!advertisedDevice.getName().isEmpty()) {
      std::string scanMessage = std::string("Scanning...\n") + std::string(advertisedDevice.getName().c_str());
      lv_label_set_text(ui_StartBTLB, scanMessage.c_str());
      Timer_Loop();
    }
    // We have found a device, let us now see if it contains the service we are looking for.
    //if (advertisedDevice.haveServiceUUID() && advertisedDevice.isAdvertisingService(serviceUUID)) {
    if (advertisedDevice.getName().startsWith("ESPBatMon")) {
      BLEDevice::getScan()->stop();
      myDevice = new BLEAdvertisedDevice(advertisedDevice);
      doConnect = true;
      allowReScan = true;
      lv_label_set_text(ui_StartBTLB, "Scan\nBattery");
      Timer_Loop();
      if (screen=="Home") {
          screen="BatMan";
          state=1;
          lv_disp_load_scr(ui_BatMan);
      }
      Serial.print("ESPBatMon2 Found: ");
      Serial.println(advertisedDevice.toString().c_str());
    }  // Found our server
  }  // onResult
};  // MyAdvertisedDeviceCallbacks

void setup()
{
  Serial.begin(115200);
  delay(2000);
  Serial.println("Starting ...");

  pinMode(BTN_BUILDIN, INPUT_PULLUP);
  LCD_Init();
  Lvgl_Init();
  ui_init();

  preferences.begin("batman_lcd", false);
  int32_mWH00Day = preferences.getUInt("mWH00Day", 99999999);
  if (int32_mWH00Day == 99999999) {
    int32_mWH00Day = 40000000;
    preferences.putUInt("mWH00Day", int32_mWH00Day);  //if not exist, set max 400WH
  }
  Serial.print("Daily meter: ");
  Serial.println(int32_mWH00Day);

  Serial.println("Starting Arduino BLE Client application...");
  rgbLedWrite(RGB_BUILTIN, RGB_BRIGHTNESS, 0, 0);
  BLEDevice::init("");
  // Retrieve a Scanner and set the callback we want to use to be informed when we
  // have detected a new device.  Specify that we want active scanning and start the
  // scan to run for 15 seconds.
  BLEScan *pBLEScan = BLEDevice::getScan();
  pBLEScan->setAdvertisedDeviceCallbacks(new MyAdvertisedDeviceCallbacks());
  pBLEScan->setInterval(1349);
  pBLEScan->setWindow(449);
  pBLEScan->setActiveScan(true);
}

void ScanResultsCB(BLEScanResults result) {
  if (!connected) {
    Serial.println("Scan complete: No devices found.");
  }
  lv_label_set_text(ui_StartBTLB, "Scan\nBattery");
  Timer_Loop();
}

void loop()
{
  if (digitalRead(BTN_BUILDIN) == 0) { // Button pressed
    if (!buttonPressed && pressStartTime == 0) { // Set pressStartTime only once
      pressStartTime = millis();
      if (refButton != nullptr) { 
        lv_obj_add_state(refButton, LV_STATE_PRESSED);
      }
    }
  } else { // Button released
    if (pressStartTime != 0) { // Process only if pressStartTime was set
      unsigned long pressDuration = millis() - pressStartTime;
      pressStartTime = 0; // Reset pressStartTime      
      if (refButton != nullptr) { 
        lv_obj_clear_state(refButton, LV_STATE_PRESSED);
      }

      buttonPressed = true; // Set flag after release
      buttonLongPressed = false;
      if (pressDuration >= 5000) { // Long press
        buttonLongPressed = true;
      }
    }
  }

  if (screen=="Home") {
    refButton = ui_StartBT;
    rgbLedWrite(RGB_BUILTIN, RGB_BRIGHTNESS, RGB_BRIGHTNESS, RGB_BRIGHTNESS);  // white
    if (buttonPressed) {
      buttonPressed = false;
      buttonLongPressed = false;
      lv_label_set_text(ui_StartBTLB, "Scanning...\n");
      Timer_Loop();
      Serial.println("Starting scaning...");
      bool result=BLEDevice::getScan()->start(15, *ScanResultsCB, false);
    }
  }

  if (screen=="BatMan") {
    refButton = nullptr;
    rgbLedWrite(RGB_BUILTIN, RGB_BRIGHTNESS, 0, 0);  // Green

    if (buttonPressed) {
      buttonPressed = false;
      buttonLongPressed = false;
      screen="ResetDaily";
      lv_disp_load_scr(ui_ResetDaily);
    }
  }

  if (screen=="ResetDaily") {
    refButton = ui_ResetDailyBT;
    rgbLedWrite(RGB_BUILTIN, 0, 0, RGB_BRIGHTNESS);  // Blue
    if (buttonLongPressed) {
      buttonPressed = false;
      buttonLongPressed = false;
      int32_mWH00Day = int32_mWH00Now; //set current remaining power as start of day reading
      lv_label_set_text(ui_ResetDailyResultLB, "Reset Done");
      Timer_Loop();
    }
    if (buttonPressed) {
      buttonPressed = false;
      lv_label_set_text(ui_ResetDailyResultLB, "");
      screen="BatteryMax";
      lv_disp_load_scr(ui_BatteryMax);
    }
  }

  if (screen=="BatteryMax") {
    refButton = ui_BatteryMaxBT;
    rgbLedWrite(RGB_BUILTIN, 0, RGB_BRIGHTNESS, 0);  // red
    if (buttonLongPressed) {
      buttonPressed = false;
      buttonLongPressed = false;
      uint16_t max_WH = 400;
      pUpdateCharacteristic->writeValue((uint8_t *)&max_WH, sizeof(max_WH));
      lv_label_set_text(ui_BatteryMaxResultLB, "Set to Max");
      Timer_Loop();
    }
    if (buttonPressed) {
      buttonPressed = false;
      lv_label_set_text(ui_BatteryMaxResultLB, "");
      screen="BatMan";
      lv_disp_load_scr(ui_BatMan);
    }
  }

  if (doConnect == true) {
    if (connectToServer()) {
      Serial.println("We are now connected to the BLE Server.");
    } else {
      Serial.println("We have failed to connect to the server; there is nothing more we will do.");
      preferences.putUInt("mWH00Day", int32_mWH00Day); //save daily meter
      screen="Home";
      state=0;
      lv_disp_load_scr(ui_Home);
    }
    doConnect = false;
  }
  if (!connected) {
    if (allowReScan) {
      allowReScan = false;
      Serial.println("Starting rescan...");
      lv_label_set_text(ui_StartBTLB, "Scanning...\n");
      Timer_Loop();
      bool result=BLEDevice::getScan()->start(15, *ScanResultsCB, false);
    }
  }

  Timer_Loop();
  delay(10);
}
