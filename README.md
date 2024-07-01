# Introduction 
To a person with physical disabilities, a proper functioning wheelchair is of paramount importance, especially the battery being the heart of the wheelchair. Unfortunately, the only indicator of the battery's remaining capacity is the voltage on the display panel. Voltage is obvisouly not an accurate measurement of its remaining capacity, as it is not linearly proprotional to the remaining capacity and may sometimes be misleading (e.g. Recovery effect). A better alternatvie is coulomb counting, which counts the actual energy in and out.

# The ultimate goal -- The (Bat)tery (Man)ager BatMan App
To develop an IOS/Android App to display battery's remaining capacity; current and today's power consumption, current Amperes and voltage output in realtime.
![BatMan App - Main Penal](/ESPBatMan3/HW/Panel.png)
The daily consumption history data forms a normal distribution to predict if a recharge is needed
![BatMan App - Analytic](/ESPBatMan3/HW/Analyze.png)
Share the consumption history data as a csv file with other Apps
![BatMan App - Data Sharing](/ESPBatMan3/HW/Share.png)

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
[ESP32-C3-DevKit](https://www.amazon.com/-/zh_TW/16385/dp/B08RDDFWFJ)
* Power Monitor
[INA226 0-36V 20A Power Monitor Module](https://www.amazon.com/-/zh_TW/dp/B0CZJ5J25T)
* 7-36v DC to 5v DC switching power supply
[OKI-78SR-5/1.5-W36-C](https://www.digikey.tw/zh/products/detail/murata-power-solutions-inc/OKI-78SR-5-1-5-W36-C/2259781)
## Design
* High side sensing: INA226 is placed in between the battery's positive connector and the wheelchair's positive pole. Their ground poles are interconnected
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
## Communication Specification
### Name and UUIDs
* The Device Name must start with 'BatMan', such as 'BatMan010'
* The Device must provide the followigs:-
	* Service UUID: 4fafc201-1fb5-459e-8fcc-c5c9c331914b
	* Characteristics UUID:
    	* Broadcast: aa337802-6aca-42a5-b34d-a8d9fc12a266
    	* Update: beb5483e-36e1-4688-b7f5-ea07361b26a8
### Broadcast Data Format
* It is a sequence of 12 bytes of 8-bits data:
* 1-2 byte: a 16-bits signed mV (1000 mV = 1V)
* 3-4 byte: a 16-bits signed mA (1000 mA = 1A)
* 5-8 byte: a 32-bits signed mW (1000 mW = 1W)
* 9-12 byte: a 32-bits signed mWH (1000 mWH = 1WH)
* Expect to broadcast every 5 seconds.
### Update Data Format
* It is 2 bytes of 8-bits data:
	* 1-2 byte: a 16-bits signed WH.

# The BatMan Mobile App
* The Batman2.sln is the App's Visual Studio solution written in C# Cross Platforms MAUI App
* Xamarin's support has endded on May 1, 2024. Please refer to this [old Xamarin Branch](https://github.com/leungkimming/BatMan1/tree/main)
## Scan and Connect Battery
Pull down to start scanning for nearby BLE devices. If only one is found,
it will be connected. Otherwise, users can select which one to connect.

## Demo Mode
Select the 'Demo' menu item to switch to Demo mode. The App will simulate 3 Devices
for Demo purposes. Once a device is selected, users can shake the iPhone
to switch between 'Charging' mode and 'Consuming' mode.

## History
* Before you connect to any battery, all batteries' histories will be shown. Otherwise,
only the histories of the connected battery will be shown.
* Pull down to refresh lastest histories.
* If you select a history row, the selected row's battery histories will be selected
for Analyze, Share and Delete. Otherwise, all battery's histories will be selected.
* To save storage space, only 7 days' per 5-minutes record will be kept. Beyond 7 days,
history records will be compressed without affecting the daily consumption analysis.
* Select the 'Analyze' menu item to analyze the histories. 
* Select the 'Share' menu item to share the histories thru your phone's App e.g. Email.
* Select the 'Delete' menu item to delete the histories.
* Select the 'Import' menu item to select and import a csv file. Use the same csv file format
as output from the 'Share' function

## Analyze
* If you have connected to a battery or selected one of the history row, the selected/connected
battery's histories will be analyzed. Otherwise, histories of all batteries will be analyzed.
* The App will use the best fit model between 'Normal' and 'Gamma' distribution model to do analysis. 
The consumption's mean and standard deviation will be calculated.
* You can adjust the default 90% confidence level to calculate the Recharge Alert or Threshold.
For example, you can have 99% for sure the remaining WH is enough for a typical day's usage 
before recharge. You need this WH level to setup the Panel's alert level.
* Analysis needs big data. Too few consumption data will result in error.
* Pull down to refresh the lastest consumption histories and recalculate the recharge alert.
* Select the 'Share' menu item to share the consumption histories thru your phone's App e.g. Email.

## Panel
* The top left box shows the current date, the last data update time and the current
day's consumption in Watts-Hour
* The top right Gauge is current recharge (+ve) and consumption (-ve) Watts.
* The middle Gauge is the oil gauge showing the remaining energy of your battery.
You can use the 'Setup' function to adjust your battery's maximum capacity and recharge alert level.
* The bottom left Gauge is current Ampere coming in to or going out from your battery.
* The bottom right Gauge is current Voltage of your battery.
* Expect to received update from the BlueTooth device every 5 seconds.
* Select the 'History' menu item to display the battery's histories. 
Select the 'Analyze' menu item to analyze the battery's histories. 
Select the 'Setup' menu item to setup the Panel maximum and alert level.
* Select the 'Adjust' menu item to update the BlueTooth device's remaining WH.

## Adjust
The remaining energy in WH of your battery is kept inside the flash memory of the
BlueTooth device. You may need a few recharge cycles to determine the total energy capacity
of your battery. Under a safe circumstance (e.g. you have a backup battery or you are near
a recharge station), try to use up your battery but be careful not to drain it out. Then, adjust
the remainig WH to 0WH and fully recharge your battery. Upon fully recharge, note down
the remaining WH reading in the Panel. The reading is the Maximum WH of your battery
that you will be used to Setup your battery's Panel using the Setup function.

## Setup
* Set the Gauge Max. WH to the value you noted down above.
* Set the Gauge Alert WH to the value you obtain in the Analyze function above. Recall
that this is of the certain confidence level that your battery is enough for a typical
day's usage. A Recharge is required if your battery is falling near this alert level.

# Mac setup (MAUI)
* Visual Studio for Mac will no longer be supported after August 31, 2024.
* Hence, only need to install:
	* Latest Xcode.
	* Latest .NET
* To test in IOS, in Visual Studio (Windows), "paired to Mac" to build & run in simulators and local devices.

# Mac setup (Xamarin)
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
# Visual Studio Windows Setup
```
Microsoft Visual Studio Community 2022
Version 17.10.1
VisualStudio.17.Release/17.10.1+34928.147
Microsoft .NET Framework
Version 4.8.09032

Installed Version: Community

ASP.NET and Web Tools   17.10.338.1105
ASP.NET and Web Tools

C# Tools   4.10.0-3.24270.2+e8f775c1d8a73dee7ad02408712d714251e708ea
C# components used in the IDE. Depending on your project type and settings, a different version of the compiler may be used.

Extensibility Message Bus   1.4.39 (main@e8108eb)
Provides common messaging-based MEF services for loosely coupled Visual Studio extension components communication and integration.

Microsoft JVM Debugger   1.0
Provides support for connecting the Visual Studio debugger to JDWP compatible Java Virtual Machines

Mono Debugging for Visual Studio   17.10.8 (a565b86)
Support for debugging Mono processes with Visual Studio.

NuGet Package Manager   6.10.0
NuGet Package Manager in Visual Studio. For more information about NuGet, visit https://docs.nuget.org/

Razor (ASP.NET Core)   17.10.3.2427201+4f57d1de251e654812adde201c0265a8ca7ca31d
Provides languages services for ASP.NET Core Razor.

SQL Server Data Tools   17.10.171.4
Microsoft SQL Server Data Tools

ToolWindowHostedEditor   1.0
Hosting json editor into a tool window

Visual Studio IntelliCode   2.2
AI-assisted development for Visual Studio.

VisualStudio.DeviceLog   1.0
Information about my package

VisualStudio.Mac   1.0
Mac Extension for Visual Studio

VSPackage Extension   1.0
VSPackage Visual Studio Extension Detailed Info

Xamarin   17.10.0.110 (main@cf2e960)
Visual Studio extension to enable development for Xamarin.iOS and Xamarin.Android.

Xamarin Designer   17.10.3.10 (remotes/origin/d17-10@3beef58f89)
Visual Studio extension to enable Xamarin Designer tools in Visual Studio.

Xamarin Templates   17.9.0 (38e87ba)
Templates for building iOS, Android, and Windows apps with Xamarin and Xamarin.Forms.

Xamarin.Android SDK   13.2.2.0 (d17-5/45b0e14)
Xamarin.Android Reference Assemblies and MSBuild support.
    Mono: d9a6e87
    Java.Interop: xamarin/java.interop/d17-5@149d70fe
    SQLite: xamarin/sqlite/3.40.1@68c69d8
    Xamarin.Android Tools: xamarin/xamarin-android-tools/d17-5@ca1552d


Xamarin.iOS and Xamarin.Mac SDK   16.4.0.23 (9defd91b3)
Xamarin.iOS and Xamarin.Mac Reference Assemblies and MSBuild support.
```
