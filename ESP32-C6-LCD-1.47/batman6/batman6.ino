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
#include "Preferences.h"
#include "batmanble.h"

#define RGB_BUILDIN 8
#define RGB_BRIGHTNESS 64
#define BTN_BUILDIN 9
#define WAKEUP_PIN GPIO_NUM_0
#define IDLE_SLEEP_INTERVALS 60 //60 x 5 = 5 minute no current; shut down ESP32 to save power
int state=0;
bool pressed=false;
String screen="BatMan";
bool start=false;
uint32_t int32_mWH00Day = 0;
uint32_t int32_mWH00Now = 0;
Preferences preferences;

static unsigned long pressStartTime = 0;
static bool buttonPressed = false;
static bool buttonLongPressed = false;
static lv_obj_t *refButton = nullptr;

static void printData() {
  uint16_t received_mV = bleGetVoltageMv();
  int16_t received_mA = bleGetCurrentMa();
  int32_t received_mW00 = bleGetPowerMw();
  uint32_t received_mWH00 = bleGetWhRemaining();
  int32_mWH00Now = received_mWH00;

  float voltage_V = received_mV / 1000.0;
  float current_A = received_mA / 1000.0;
  float power_W = received_mW00 / (1000.0 * 100.0);
  float remaining_WH = received_mWH00 / (1000.0 * 100.0);
  float Daily_WH = (int32_mWH00Day - int32_mWH00Now) / (1000.0 * 100.0);

  lv_label_set_text(ui_VoltageValue, String(voltage_V,3).c_str());
  lv_label_set_text(ui_CurrentValue, String(current_A,3).c_str());
  lv_label_set_text(ui_WattsValue, String(power_W,3).c_str());
  lv_label_set_text(ui_MWRemainingValue, String(remaining_WH,4).c_str());
  lv_label_set_text(ui_DailyValue, String(Daily_WH,3).c_str());
  Timer_Loop();
  lv_arc_set_value(ui_WHRemainingARC, remaining_WH);
}

static void updateButtonState() {
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
}

static void updateScreenState() {
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
      onBatteryMaxRequested();
    }
    if (buttonPressed) {
      buttonPressed = false;
      lv_label_set_text(ui_BatteryMaxResultLB, "");
      screen="BatMan";
      lv_disp_load_scr(ui_BatMan);
    }
  }
}

static void requestSleep() {
  blePersistState();
  esp_sleep_enable_ext1_wakeup(WAKEUP_PIN, (esp_sleep_ext1_wakeup_mode_t)0); //1 = High, 0 = Low
  esp_deep_sleep_start();
}

static void onBatteryMaxRequested() {
  bleSetWhRemainingWh(400);
  blePersistState();
  lv_label_set_text(ui_BatteryMaxResultLB, "Set to Max");
  Timer_Loop();
}

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

  Serial.println("Starting application...");
  rgbLedWrite(RGB_BUILTIN, RGB_BRIGHTNESS, 0, 0);
  bleSetup();
}

void loop()
{
  updateButtonState();
  updateScreenState();
  bleLoop();

  if (bleGetIdleCount() >= IDLE_SLEEP_INTERVALS) {
    Serial.println("No current Sleep");
    requestSleep();
  }

  printData();

  Timer_Loop();
  delay(10);
}
