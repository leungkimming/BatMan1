# Introduction 
To a person with physical disabilities, a proper functioning wheelchair is of paramount importance, especially the battery being the heart of the wheelchair. Unfortunately, the only indicator of the battery's remaining capacity is the voltage on the display panel. Voltage is obvisouly not an accurate measurement of its remaining capacity, as it is not linearly proprotional to the remaining capacity and may sometimes be misleading (e.g. Recovery effect). A better alternatvie is coulomb counting, which counts the actual energy in and out.

# The ultimate goal -- The (Bat)tery (Man)ager BatMan App
To develop an IOS/Android App to display battery's remaining capacity; current and today's power consumption, current Amperes and voltage output in realtime.
![BatMan App - Main Penal](/ESPBatMan3/HW/Panel.png)
The daily consumption history data forms a normal distribution to predict if a recharge is needed
![BatMan App - Analytic](/ESPBatMan3/HW/Analyze.png)
Share the consumption history data as a csv file with other Apps
![BatMan App - Data Sharing](/ESPBatMan3/HW/Share.png)
* The Batman2.sln is the App's Visual Studio solution written in C# Xamarin
* As Xamarin's support has endded on May 1, 2024, my next plan is to rewrite the solution in MAUI

# Coulomb counting basics
## Watt-Hour calculation
* Energy is in the unit of Watt-Hour (WH)
* Watt (W) = Voltage (V) x Current in Amperes (A)
* 1 WH = 1 hour of continous supply of 1 Watt
* e.g. A motor with a 24V power source
    * Consuming a steady 2.5 Amperes 
    * Continous running for half an hour
    * = 24 * 2.5 * 0.5 = 30 WH
## Realtime Watt-Hour measurement
* The V and A of the wheelchair's battery is not fixed but continously changing over time
* Hence, I use a power monitor to take snapshot readings of Voltage (V) and Amperes (A) of the Wheelchair battery every 5 seconds
* 1 second = (1 / 60 / 60) = 1/3600 Hour
* Assume that the V and A is the average V and A throughout the 5 seconds
* Watt (W) = V x A in that 5 seconds
* Watt-Hour (WH) = W * 5 * (1/3600) in that 5 seconds
* Sum up these 5 seconds WH to obtain the total WH consumption of the Wheelchair

# The hardware
## Functions
* Develop a circuit to sit in between the battery and the wheelchair body
* The circuit will measure watt-hour in realtime mode and compute the remaining capacity; current power consumption, current Amperes and voltage output
* The BatMan App will connect to the circuit via Bluetooth and receive these data every 5 seconds
## Components
* CPU 
[ESP32-C3-DevKit](https://www.amazon.com/ESP32-C3-DevKitM-1-Development-ESP32-C3-MINI-1-Module-ESP32-C3FN4/dp/B09F5XRK12)
* Power Monitor
[INA226 0-36V 20A Power Monitor Module](https://www.amazon.com/-/zh_TW/1/dp/B08MKSQSL9/ref=sr_1_5?crid=2Y8E1WAQRXWQ2&dib=eyJ2IjoiMSJ9.d0LlPHX67LZ7GOswO4aaNd-XWBmz_wJCpfx5QxZD7NtE1Jb73oBMl37KnGlycMP_UGUTOgaDo04ssmXRsm05yuDjNES5gqGyPUHDagGKXjIvubQ5S0hzC-wF791mj8avR0c8i1wZCF8hpOx0_yrCwl3DuAnuDRG_y8LrUcqfu96OObwy7gypzQJ6zQgt7G-lgprv--8bJIYaZfyO69r1i6MNRHJte7lp3GGH-zWhc2o.2ce_MX7VkVsEFW4yfC_BL-Bz6hMtUyl2bOe7p76BHms&dib_tag=se&keywords=INA226&qid=1717505192&sprefix=ina22%2Caps%2C276&sr=8-5)
* 7-36v DC to 5v DC switching power supply
[OKI-78SR-5/1.5-W36-C](https://www.digikey.tw/zh/products/detail/murata-power-solutions-inc/OKI-78SR-5-1-5-W36-C/2259781)
## Design
* Low side sensing: INA266 is placed in between the wheelchair's negative connector and the battery's negative pole. Their positive poles are interconnected
* OKI-78SR draw power from the 24-36v battery and supply 5v to ESP32 and INA226
* ESP32 communicate with INA226 via I2C interface to obtain current V and A every 5 seconds and send the computed remaining capacity; current power consumption, current Amperes and voltage via Bluetooth to the BatMan App
* The ESP32 will store the remaining capacity in its internal memory before going hibernation due to low current. This is to save power
* INA226 will wake up ESP32 upon high current detected
## The Circuit
![Circuit board](/ESPBatMan3/HW/circuit.png)
## The final assembly
![The final assembly](/ESPBatMan3/HW/IMG_7569.png)
## Software
* ESPBatMan3.ino is the arduino Program running inside the ESP32
* Double check the shunt resistor value in your INA226 and update the value to the program 
