#include <lvgl.h>
#include <demos/lv_demos.h>
#include <Arduino_GFX_Library.h>
#include "bsp_cst816.h"
#include "FastIMU.h"
#include <Wire.h>
#include "ui.h"
#include "BLEDevice.h"
#include "Preferences.h"

#define IMU_ADDRESS 0x6B    //Change to the address of the IMU
#define PERFORM_CALIBRATION //Comment to disable startup calibration
QMI8658 IMU;               //Change to the name of any supported IMU! 

calData calib = { 0 };  //Calibration data
AccelData accelData;    //Sensor data
GyroData gyroData;

#define PIN_NUM_LCD_SCLK 39
#define PIN_NUM_LCD_MOSI 38
#define PIN_NUM_LCD_MISO 40
#define PIN_NUM_LCD_DC 42
#define PIN_NUM_LCD_RST -1
#define PIN_NUM_LCD_CS 45
#define PIN_NUM_LCD_BL 1
#define PIN_NUM_TP_SDA 48
#define PIN_NUM_TP_SCL 47

#define LEDC_FREQ             5000
#define LEDC_TIMER_10_BIT     10


#define LCD_ROTATION 0
#define LCD_H_RES 240
#define LCD_V_RES 320

#define BTN_BUILDIN 0

int state=0;
String screen="Home";
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

/* More data bus class: https://github.com/moononournation/Arduino_GFX/wiki/Data-Bus-Class */
Arduino_DataBus *bus = new Arduino_ESP32SPI(
  PIN_NUM_LCD_DC /* DC */, PIN_NUM_LCD_CS /* CS */,
  PIN_NUM_LCD_SCLK /* SCK */, PIN_NUM_LCD_MOSI /* MOSI */, PIN_NUM_LCD_MISO /* MISO */);

/* More display class: https://github.com/moononournation/Arduino_GFX/wiki/Display-Class */
Arduino_GFX *gfx = new Arduino_ST7789(
  bus, PIN_NUM_LCD_RST /* RST */, LCD_ROTATION /* rotation */, true /* IPS */,
  LCD_H_RES /* width */, LCD_V_RES /* height */);


/*******************************************************************************
 * End of Arduino_GFX setting
 ******************************************************************************/

/*******************************************************************************
 * Please config the touch panel in touch.h
 ******************************************************************************/
// #include "touch.h"

uint32_t bufSize;
lv_disp_draw_buf_t draw_buf;
lv_color_t *disp_draw_buf;
lv_disp_drv_t disp_drv;
bool isPortrait = true; // Assume portrait mode by default
lv_timer_t *qmi8658_timer = NULL;

static void qmi8658_callback(lv_timer_t *timer) {
  IMU.update();
  IMU.getAccel(&accelData);
  static int orientation = 4;
  static int lastOrientation = 0;

  // Lay flat detection: Z is close to -1g (gravity pointing down in negative Z)
  if (fabs(accelData.accelZ) > 0.9 && fabs(accelData.accelX) < 0.1 && fabs(accelData.accelY) < 0.1) {
    orientation = 4; // Lay flat
  } 
  // Landscape (0°): X > 0.5, others small
  else if (accelData.accelX > 0.5 && fabs(accelData.accelY) < 0.3) {
    orientation = 0; // Portrait usb below
  } 
  // Portrait (90°): Y < -0.5, others small  
  else if (accelData.accelY < -0.5 && fabs(accelData.accelX) < 0.3) {
    orientation = 3; // Landscape usb left
  } 
  // Landscape flipped (180°): X < -0.5, others small
  else if (accelData.accelX < -0.5 && fabs(accelData.accelY) < 0.3) {
    orientation = 2; // Portrait usb above
  } 
  // Portrait (270°): Y > 0.5, others small
  else if (accelData.accelY > 0.5 && fabs(accelData.accelX) < 0.3) {
    orientation = 1; // Landscape usb right
  }
  // Serial.println("last orientation: " + String(lastOrientation) + ", current orientation: " + String(orientation));
  // Only update display if orientation changed
  if (orientation != lastOrientation && orientation != 4) {
    updateDisplayOrientation(orientation);
    lastOrientation = orientation;
  }
}

void updateDisplayOrientation(int gfx_rotation) {
  static int last_isPortrait = isPortrait;

  gfx->setRotation(gfx_rotation);

  // Update touch rotation
  bsp_touch_init(&Wire, gfx->getRotation(), gfx->width(), gfx->height());

  // Update display resolution
  lv_disp_t * disp = lv_disp_get_default();
  disp->driver->hor_res = gfx->width();
  disp->driver->ver_res = gfx->height();
  lv_disp_drv_update(disp, disp->driver);

  // Determine new orientation
  if (gfx_rotation == 0 || gfx_rotation == 2) {
    isPortrait = true;
  } else {
    isPortrait = false;
  }

  // Update UI based on orientation change
  if (isPortrait != last_isPortrait) {
      lv_scr_load(get_correct_screen());
      last_isPortrait = isPortrait;
  }
}

lv_obj_t* get_correct_screen() {
  lv_obj_t *scr = lv_disp_get_scr_act(NULL);
  if (scr == NULL) {
    scr = lv_disp_get_scr_act(lv_disp_get_default());
  }
  if (scr == ui_Home || scr == ui_HomeLD) {
    scr = isPortrait ? ui_Home : ui_HomeLD;
  }
  else if (scr == ui_BatMan || scr == ui_BatManLD) {
    scr = isPortrait ? ui_BatMan : ui_BatManLD;
  }
  else if (scr == ui_ResetDaily || scr == ui_ResetDailyLD) {
    scr = isPortrait ? ui_ResetDaily : ui_ResetDailyLD;
  }
  else if (scr == ui_BatteryMax || scr == ui_BatteryMaxLD) {
    scr = isPortrait ? ui_BatteryMax : ui_BatteryMaxLD;
  }
  return scr;
}

/*Read the touchpad*/
void my_touchpad_read(lv_indev_drv_t *indev_drv, lv_indev_data_t *data) {
  uint16_t touchpad_x;
  uint16_t touchpad_y;
  bsp_touch_read();
  if (bsp_touch_get_coordinates(&touchpad_x, &touchpad_y)) {
    data->point.x = touchpad_x;
    data->point.y = touchpad_y;
    data->state = LV_INDEV_STATE_PRESSED;
  } else {
    data->state = LV_INDEV_STATE_RELEASED;
  }
}

/*******************************************************************************
 * BatMan Logics
 ******************************************************************************/
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
    lv_label_set_text(ui_BatMan_VoltageValue, String(voltage_V,3).c_str());
    lv_label_set_text(ui_BatMan_CurrentValue, String(current_A,3).c_str());
    lv_label_set_text(ui_BatMan_WattsValue, String(power_W,3).c_str());
    lv_label_set_text(ui_BatMan_MWRemainingValue, String(remaining_WH,4).c_str());
    lv_label_set_text(ui_BatMan_DailyValue, String(Daily_WH,3).c_str());
    lv_label_set_text(ui_BatManLD_VoltageValue, String(voltage_V,3).c_str());
    lv_label_set_text(ui_BatManLD_CurrentValue, String(current_A,3).c_str());
    lv_label_set_text(ui_BatManLD_WattsValue, String(power_W,3).c_str());
    lv_label_set_text(ui_BatManLD_MWRemainingValue, String(remaining_WH,4).c_str());
    lv_label_set_text(ui_BatManLD_DailyValue, String(Daily_WH,3).c_str());
    lv_timer_handler();
    lv_arc_set_value(ui_BatMan_WHRemainingARC, remaining_WH);
    lv_arc_set_value(ui_BatManLD_WHRemainingARC, remaining_WH);
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
    lv_scr_load(isPortrait ? ui_Home : ui_HomeLD);
    Serial.println("onDisconnect");
    connected = false;
  }
};

bool connectToServer() {
  Serial.print("Forming a connection to ");
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
    Serial.println(advertisedDevice.toString().c_str());
    if (!advertisedDevice.getName().isEmpty()) {
      std::string scanMessage = std::string("Scanning...\n") + std::string(advertisedDevice.getName().c_str());
      lv_label_set_text(ui_Home_StartBTLB, scanMessage.c_str());
      lv_label_set_text(ui_HomeLD_StartBTLB, scanMessage.c_str());
      lv_timer_handler();
    }
    // We have found a device, let us now see if it contains the service we are looking for.
    //if (advertisedDevice.haveServiceUUID() && advertisedDevice.isAdvertisingService(serviceUUID)) {
    if (advertisedDevice.getName().startsWith("ESPBatMon")) {
      BLEDevice::getScan()->stop();
      myDevice = new BLEAdvertisedDevice(advertisedDevice);
      doConnect = true;
      allowReScan = true;
      lv_label_set_text(ui_Home_StartBTLB, "Scan\nBattery");
      lv_label_set_text(ui_HomeLD_StartBTLB, "Scan\nBattery");
      lv_timer_handler();
      if (screen=="Home") {
          screen="Others";
          state=1;
          lv_scr_load(isPortrait ? ui_BatMan : ui_BatManLD);
      }
      Serial.print("ESPBatMon2 Found: ");
      Serial.println(advertisedDevice.toString().c_str());
    }  // Found our server
  }  // onResult
};  // MyAdvertisedDeviceCallbacks

void ScanResultsCB(BLEScanResults result) {
  if (!connected) {
    Serial.println("Scan complete: No devices found.");
  }
  lv_label_set_text(ui_Home_StartBTLB, "Scan\nBattery");
  lv_label_set_text(ui_HomeLD_StartBTLB, "Scan\nBattery");
  lv_timer_handler();
}

void StartScanBattery(lv_event_t * e) {
  Serial.println("Starting scan for ESPBatMon2 devices...");
  lv_label_set_text(ui_Home_StartBTLB, "Scanning...\n");
  lv_label_set_text(ui_HomeLD_StartBTLB, "Scanning...\n");
  lv_timer_handler();
  bool result = BLEDevice::getScan()->start(15, *ScanResultsCB, false);
  if (!result) {
    Serial.println("Scan failed to start.");
  }
}

void ResetDailyWH(lv_event_t * e) {
    int32_mWH00Day = int32_mWH00Now; //set current remaining power as start of day reading
    lv_label_set_text(ui_ResetDaily_ResetDailyResultLB, "Reset Done");
    lv_label_set_text(ui_ResetDailyLD_ResetDailyResultLB, "Reset Done");
    lv_timer_handler();
}

void SetBatterytoMax(lv_event_t * e){
    uint16_t max_WH = 400; // Set maximum to 400 WH
    pUpdateCharacteristic->writeValue((uint8_t *)&max_WH, sizeof(max_WH));
    lv_label_set_text(ui_BatteryMax_BatteryMaxResultLB, "Set to Max");
    lv_label_set_text(ui_BatteryMaxLD_BatteryMaxResultLB, "Set to Max");
    lv_timer_handler();
}

/* Display flushing */
void my_disp_flush(lv_disp_drv_t *disp_drv, const lv_area_t *area, lv_color_t *color_p) {
  lv_disp_flush_ready(disp_drv);
}

void setup() {
  pinMode(BTN_BUILDIN, INPUT_PULLUP);
  Serial.begin(115200);

  Serial.println("BatMan5T Starting... ");

  String LVGL_Arduino = String('V') + lv_version_major() + "." + lv_version_minor() + "." + lv_version_patch();
  Serial.println(LVGL_Arduino);

#ifdef GFX_EXTRA_PRE_INIT
  GFX_EXTRA_PRE_INIT();
#endif

  // Init Display
  if (!gfx->begin()) {
    Serial.println("gfx->begin() failed!");
  }
  gfx->fillScreen(BLACK);

#ifdef PIN_NUM_LCD_BL
  ledcAttach(PIN_NUM_LCD_BL , LEDC_FREQ, LEDC_TIMER_10_BIT);
  ledcWrite(PIN_NUM_LCD_BL , (1 << LEDC_TIMER_10_BIT) / 100 * 80);
#endif

  // Init touch device
  // touch_init(gfx->width(), gfx->height(), gfx->getRotation());
  Wire.begin(PIN_NUM_TP_SDA, PIN_NUM_TP_SCL);
  bsp_touch_init(&Wire, gfx->getRotation(), gfx->width(), gfx->height());
  int err = IMU.init(calib, IMU_ADDRESS);
  if (err != 0) {
    Serial.print("Error initializing IMU: ");
    Serial.println(err);
  }

  lv_init();

  uint32_t screenWidth = gfx->width();
  uint32_t screenHeight = gfx->height();

  bufSize = screenWidth * screenHeight;


  disp_draw_buf = (lv_color_t *)heap_caps_malloc(bufSize * 2, MALLOC_CAP_INTERNAL | MALLOC_CAP_8BIT);
  if (!disp_draw_buf) {
    // remove MALLOC_CAP_INTERNAL flag try again
    disp_draw_buf = (lv_color_t *)heap_caps_malloc(bufSize * 2, MALLOC_CAP_8BIT);
  }

  if (!disp_draw_buf) {
    Serial.println("LVGL disp_draw_buf allocate failed!");
  } else {
    lv_disp_draw_buf_init(&draw_buf, disp_draw_buf, NULL, bufSize);

    /* Initialize the display */
    lv_disp_drv_init(&disp_drv);
    /* Change the following line to your display resolution */
    disp_drv.hor_res = screenWidth;
    disp_drv.ver_res = screenHeight;
    disp_drv.flush_cb = my_disp_flush;
    disp_drv.draw_buf = &draw_buf;
    disp_drv.direct_mode = true;

    lv_disp_drv_register(&disp_drv);

    /* Initialize the (dummy) input device driver */
    static lv_indev_drv_t indev_drv;
    lv_indev_drv_init(&indev_drv);
    indev_drv.type = LV_INDEV_TYPE_POINTER;
    indev_drv.read_cb = my_touchpad_read;
    lv_indev_drv_register(&indev_drv);

    ui_init();

    /* Initialize QMI8658 */
    qmi8658_timer = lv_timer_create(qmi8658_callback, 1000, NULL);

    /* Initialize BatMan */
    preferences.begin("batman_lcd", false);
    int32_mWH00Day = preferences.getUInt("mWH00Day", 99999999);
    if (int32_mWH00Day == 99999999) {
      int32_mWH00Day = 40000000;
      preferences.putUInt("mWH00Day", int32_mWH00Day);  //if not exist, set max 400WH
    }
    Serial.print("Daily meter: ");
    Serial.println(int32_mWH00Day);

    Serial.println("Starting Arduino BLE Client application...");
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

  Serial.println("Setup done");
}

void loop() {

  if (doConnect == true) {
    if (connectToServer()) {
      Serial.println("We are now connected to the BLE Server.");
    } else {
      Serial.println("We have failed to connect to the server; there is nothing more we will do.");
      preferences.putUInt("mWH00Day", int32_mWH00Day); //save daily meter
      screen="Home";
      state=0;
      lv_scr_load(isPortrait ? ui_Home : ui_HomeLD);
    }
    doConnect = false;
  }
  if (!connected) {
    if (allowReScan) {
      allowReScan = false;
      Serial.println("Starting rescan...");
      lv_label_set_text(ui_Home_StartBTLB, "Scanning...\n");
      lv_label_set_text(ui_HomeLD_StartBTLB, "Scanning...\n");
      lv_timer_handler();
      bool result=BLEDevice::getScan()->start(15, *ScanResultsCB, false);
    }
  }

  lv_timer_handler(); /* let the GUI do its work */
#if (LV_COLOR_16_SWAP != 0)
  gfx->draw16bitBeRGBBitmap(0, 0, (uint16_t *)disp_draw_buf, gfx->width(), gfx->height());
#else
  gfx->draw16bitRGBBitmap(0, 0, (uint16_t *)disp_draw_buf, gfx->width(), gfx->height());
#endif
  delay(5);
}
