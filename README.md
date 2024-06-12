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
# Mac setup
```
Visual Studio Community 2022 for Mac
Version 17.6.12 (build 410)
Installation UUID: 84b78f24-02ec-45d3-ba44-3e429eab5e82

Runtime
.NET 7.0.3 (64-bit)
Architecture: X64
Microsoft.macOS.Sdk 13.1.1007; git-rev-head:8afca776a0a96613dfb7200e0917bb57f9ed5583; git-branch:release/7.0.1xx-xcode14.2

Roslyn (Language Service)
4.6.0-3.23180.6+99e956e42697a6dd886d1e12478ea2b27cceacfa

NuGet
Version: 6.4.0.117

.NET SDK (x64)
SDK: /usr/local/share/dotnet/sdk/6.0.422/Sdks
SDK Versions:
	6.0.422
	6.0.403
	6.0.401
	5.0.408
	5.0.407
	5.0.406
	5.0.405
	5.0.404
	5.0.403
	5.0.402
	5.0.401
	5.0.400
	5.0.302
	5.0.100-rc.2.20479.15
	3.1.426
	3.1.423
	3.1.420
	3.1.418
	3.1.417
	3.1.416
	3.1.415
	3.1.414
	3.1.413
	3.1.412
	3.1.411
MSBuild SDKs: /Applications/Visual Studio.app/Contents/MonoBundle/MSBuild/Current/bin/Sdks

.NET Runtime (x64)
Runtime: /usr/local/share/dotnet/dotnet
Runtime Versions:
	6.0.30
	6.0.11
	6.0.9
	5.0.17
	5.0.16
	5.0.15
	5.0.14
	5.0.13
	5.0.12
	5.0.11
	5.0.10
	5.0.9
	5.0.8
	5.0.0-rc.2.20475.5
	3.1.32
	3.1.29
	3.1.26
	3.1.24
	3.1.23
	3.1.22
	3.1.21
	3.1.20
	3.1.19
	3.1.18
	3.1.17

Xamarin.Profiler
Version: 1.8.0.49
Location: /Applications/Xamarin Profiler.app/Contents/MacOS/Xamarin Profiler

Updater
Version: 11

Apple Developer Tools
Xcode: 15.4 22622
Build: 15F31d

Xamarin Designer
Version: 17.6.3.9
Hash: 2648399ae8
Branch: remotes/origin/d17-6
Build date: 2024-05-08 22:24:17 UTC

Xamarin.Mac
Not Installed

Xamarin.Android
Not Installed

Microsoft Build of OpenJDK
Java SDK: /Library/Java/JavaVirtualMachines/microsoft-11.jdk
11.0.12
Android Designer EPL code available here:
https://github.com/xamarin/AndroidDesigner.EPL

Eclipse Temurin JDK
Java SDK: Not Found

Android SDK Manager
Version: 17.6.0.50
Hash: a715dca
Branch: HEAD
Build date: 2024-05-08 22:24:22 UTC

Android Device Manager
Version: 0.0.0.1309
Hash: 06e3e77
Branch: HEAD
Build date: 2024-05-08 22:24:22 UTC

Xamarin.iOS
Version: 16.4.0.23 Visual Studio Community
Hash: 9defd91b3
Branch: xcode14.3
Build date: 2023-10-23 16:15:00-0400

Build Information
Release ID: 1706120410
Git revision: 2f8e0518dd80a933901821bac53f7398d4b61c0f
Build date: 2024-05-08 22:22:37+00
Build branch: release-17.6
Build lane: release-17.6

Operating System
Mac OS X 14.5.0
Darwin 23.5.0 Darwin Kernel Version 23.5.0
    Wed May  1 20:09:52 PDT 2024
    root:xnu-10063.121.3~5/RELEASE_X86_64 x86_64
```
# Visual Studio Windows

