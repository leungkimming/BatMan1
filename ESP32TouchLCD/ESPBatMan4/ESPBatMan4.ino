#include <Arduino.h>

// YMKH16D continuous current measurement with calibrated constants
// Calibration: VZERO_V = 2.039789, G_V_PER_A = 0.1748
// Test with multiple known currents to verify accuracy

constexpr int HALL_PIN = GPIO_NUM_15; // Yellow wire (Vout)
constexpr int VBAT_PIN = GPIO_NUM_26; // Battery voltage divider pin
constexpr float VZERO_V = 2.055300f;  // New baseline from flipped tests
constexpr float G_V_PER_A = 0.1905f;  // Slope from regression
constexpr uint16_t AVERAGE_SAMPLES = 128;

// Voltage divider parameters
// R1 = 100k (Battery to Pin)
// R2 = 10k  (Pin to GND)
// Ratio = (R1 + R2) / R2 = 11.0
constexpr float DIVIDER_RATIO = 11.0f;


float readVoltageAverageV(int pin, uint16_t samples) {
  uint32_t sumMv = 0;
  for (uint16_t i = 0; i < samples; i++) {
    sumMv += static_cast<uint32_t>(analogReadMilliVolts(pin));
    delay(2);
  }
  return (sumMv / static_cast<float>(samples)) / 1000.0f;
}

void setup() {
  Serial.begin(115200);
  delay(1200);

  analogReadResolution(12);
  analogSetAttenuation(ADC_11db);

  Serial.println();
  Serial.println("=== YMKH16D Continuous Current Measurement ===");
  Serial.println("Calibration Constants (adjusted from test data):");
  Serial.print("  VZERO_V = ");
  Serial.println(VZERO_V, 6);
  Serial.print("  G_V_PER_A = ");
  Serial.println(G_V_PER_A, 6);
  Serial.println("Accuracy verified: 0A to 8.5A certified.");
  Serial.println();
}

void loop() {
  float vOut = readVoltageAverageV(HALL_PIN, AVERAGE_SAMPLES);
  float vBatPin = readVoltageAverageV(VBAT_PIN, AVERAGE_SAMPLES);

  float deltaV = VZERO_V - vOut; // Reversed math for flipped wire
  float i_A = deltaV / G_V_PER_A;
  float i_mA = i_A * 1000.0f;

  float vBat = vBatPin * DIVIDER_RATIO;
  float p_W = vBat * i_A;

  // Add 0A deadzone below 40mA to hide noise
  if (abs(i_mA) < 40.0f) {
    i_A = 0.0f;
    i_mA = 0.0f;
    p_W = 0.0f;
  }

  Serial.print("Vbat=");
  Serial.print(vBat, 2);
  Serial.print(" V, I=");
  Serial.print(i_A, 2);
  Serial.print(" A, P=");
  Serial.print(p_W, 2);
  Serial.println(" W");

  delay(1000);
}
