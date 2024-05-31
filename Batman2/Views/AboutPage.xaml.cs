using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace Batman2.Views
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage()
        {
            InitializeComponent();
            Title = "About BatMan";
            content.Text =
@"The main purpose of this App is to extend your Wheelchair battery's useful life by
avoiding over consumption and at the same time reducing the number of recharge cycles,
knowing that these two are the major factors affecting a battery's life. The App uses
statistical models from your consumption histories to suggest a safety margin before recharge.<br>
Very often, your wheelchair will simply display the current Voltage of your battery, but that doesn't
tell you much, especially for older batteries. The most accurate measurement is to monitor
the actual energy going into and coming out from your battery.<br>
<br><h4>If you are planning for a special trip, don't rely on this App to determine the
recharge cycle. You should always fully recharge your battery, carry a backup battery
or even plan for a mid-day recharge.</h4> 
<br><h4>Note: This App works with a BlueTooth BLE device to monitor and broadcast information
about your battery.</h4>

<br><h1>Communication Specification</h1>
Some backgrounds:<br>
Voltage(V) is the current voltage of your battery.<br>
Ampere(A) is the current Ampere output from your battery.<br>
Watts(W) = A x V is the current energy coming in to or going out from your battery.<br>
Watts Hours(WH) = W x Hours is the remaining energy of your battery. If you have 10WH,
then you can have 1W continuous output for 10 hours or 10W for 1 hour.<br>

The Device Name must start with 'BatMan', such as 'BatMan010'<br>
The Device must provide the followigs:-<br>
Service UUID: 4fafc201-1fb5-459e-8fcc-c5c9c331914b<br>
Characteristics UUID:
<ul>
    <li>Broadcast: aa337802-6aca-42a5-b34d-a8d9fc12a266</li>
    <li>Update: beb5483e-36e1-4688-b7f5-ea07361b26a8</li>
</ul>
<h2>Broadcast Data Format</h2>
It is a sequence of 12 bytes of 8-bits data:<br>
1-2 byte: a 16-bits signed mV (1000 mV = 1V)<br>
3-4 byte: a 16-bits signed mA (1000 mA = 1A)<br>
5-8 byte: a 32-bits signed mW (1000 mW = 1W)<br>
9-12 byte: a 32-bits signed mWH (1000 mWH = 1WH)<br>
Expect to broadcast every 5 seconds.<br>

<h2>Update Data Format</h2>
It is 2 bytes of 8-bits data:<br>
1-2 byte: a 16-bits signed WH.<br>

<br><h1>BatMan Functions</h1><br>
<h2>Scan and Connect Battery</h2>
Pull down to start scanning for nearby BLE devices. If only one is found,
it will be connected. Otherwise, users can select which one to connect.

<h2>Demo Mode</h2>
Select the 'Demo' menu item to switch to Demo mode. The App will simulate 3 Devices
for Demo purposes. Once a device is selected, users can shake the iPhone
to switch between 'Charging' mode and 'Consuming' mode.

<h2>History</h2>
Before you connect to any battery, all batteries' histories will be shown. Otherwise,
only the histories of the connected battery will be shown.<br>
Pull down to refresh lastest histories.<br>
If you select a history row, the selected row's battery histories will be selected
for Analyze, Share and Delete. Otherwise, all battery's histories will be selected.
<br>To save storage space, only 7 days' per 5-minutes record will be kept. Beyond 7 days,
history records will be compressed without affecting the daily consumption analysis.<br>
Select the 'Analyze' menu item to analyze the histories. 
Select the 'Share' menu item to share the histories thru your phone's App e.g. Email.<br>
Select the 'Delete' menu item to delete the histories.<br>
Select the 'Import' menu item to select and import a csv file. Use the same csv file format
as output from the 'Share' function<br>

<h2>Analyze</h2>
If you have connected to a battery or selected one of the history row, the selected/connected
battery's histories will be analyzed. Otherwise, histories of all batteries will be analyzed.<br>
The App will use the best fit model between 'Normal' and 'Gamma' distribution model to do analysis. 
The consumption's mean and standard deviation will be calculated.<br>
You can adjust the default 90% confidence level to calculate the Recharge Alert or Threshold.
For example, you can have 99% for sure the remaining WH is enough for a typical day's usage 
before recharge. You need this WH level to setup the Panel's alert level.<br>
Analysis needs big data. Too few consumption data will result in error.<br>
Pull down to refresh the lastest consumption histories and recalculate the recharge alert.<br>
Select the 'Share' menu item to share the consumption histories thru your phone's App e.g. Email.<br>

<h2>Panel</h2>
The top left box shows the current date, the last data update time and the current
day's consumption in Watts-Hour<br>
The top right Gauge is current recharge (+ve) and consumption (-ve) Watts.<br>
The middle Gauge is the oil gauge showing the remaining energy of your battery.
You can use the 'Setup' function to adjust your battery's maximum capacity and recharge alert level.<br>
The bottom left Gauge is current Ampere coming in to or going out from your battery.<br>
The bottom right Gauge is current Voltage of your battery.<br>
Expect to received update from the BlueTooth device every 5 seconds.<br>
Select the 'History' menu item to display the battery's histories. 
Select the 'Analyze' menu item to analyze the battery's histories. 
Select the 'Setup' menu item to setup the Panel maximum and alert level.<br>
Select the 'Adjust' menu item to update the BlueTooth device's remaining WH.<br>

<h2>Adjust</h2>
The remaining energy in WH of your battery is kept inside the flash memory of the
BlueTooth device. You may need a few recharge cycles to determine the total energy capacity
of your battery. Under a safe circumstance (e.g. you have a backup battery or you are near
a recharge station), try to use up your battery but be careful not to drain it out. Then, adjust
the remainig WH to 0WH and fully recharge your battery. Upon fully recharge, note down
the remaining WH reading in the Panel. The reading is the Maximum WH of your battery
that you will be used to Setup your battery's Panel using the Setup function.<br>

<h2>Setup</h2>
Set the Gauge Max. WH to the value you noted down above.<br>
Set the Gauge Alert WH to the value you obtain in the Analyze function above. Recall
that this is of the certain confidence level that your battery is enough for a typical
day's usage. A Recharge is required if your battery is falling near this alert level.<br> 
";
        }
    }
}
