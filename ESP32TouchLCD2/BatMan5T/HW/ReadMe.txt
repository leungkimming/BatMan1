Board: EPS32 Dev Module
Port COM3 (ESP32 Family Device)
Flash size 16MB
Partition scheme: Huge APP (3MG No OTA/1MB SPIFFS)
USB CDC On Boot: "Enabled"
PSRAM: OPI PSRAM
lvgl: replace C:\Users\Micl\Documents\Arduino\libraries\lvgl_libraries\lvgl
with C:\Users\Micl\Documents\Arduino\libraries\lvgl_libraries\lvgl_8.4


#include <Arduino.h>

// On ESP32-S3, GPIO 4 is mapped to Touch Channel 4 (T4)
#define WAKE_TOUCH_PIN T4 

// Choose a threshold. On ESP32-S3, the value INCREASES when touched.
// If your resting value is ~80,000, set this to wake when it crosses higher (e.g., 100,000).
#define TOUCH_THRESHOLD 105000 

void callback() {
  // This placeholder function is required by the wake-up API 
  // but it will not fully execute because deep sleep resets the chip.
}

void setup() {
  Serial.begin(115200);
  delay(1000); 

  // Check what woke the board up
  esp_sleep_wakeup_cause_t wakeup_reason = esp_sleep_get_wakeup_cause();
  
  if (wakeup_reason == ESP_SLEEP_WAKEUP_TOUCHPAD) {
    Serial.println("Wakeup caused by native touch on GPIO 4!");
  } else {
    Serial.println("Normal boot or other wake source.");
  }

  // Print raw values to the console so you can dial in your threshold
  Serial.print("Current Touch Value: ");
  Serial.println(touchRead(4));

  Serial.println("Going to deep sleep in 5 seconds... Touch GPIO 4 to wake me up.");
  delay(5000);

  // 1. Setup the touch pin and its target threshold
  touchSleepWakeUpEnable(WAKE_TOUCH_PIN, TOUCH_THRESHOLD);

  // 2. Instruct the RTC controller to keep the touch pad module powered on
  esp_sleep_enable_touchpad_wakeup();

  // 3. Enter Deep Sleep mode
  esp_deep_sleep_start();
}

void loop() {
  // This code will never run because the chip resets on wake-up.
}

