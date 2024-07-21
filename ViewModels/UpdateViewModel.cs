using System.Text.RegularExpressions;

namespace BatMan2.ViewModels
{
    [QueryProperty(nameof(WH), nameof(WH))]
    public class UpdateViewModel : BaseViewModel
    {
        private readonly Regex format = new Regex(@"^[1-9][0-9]{1,2}$|^\d$");

        public Command UpdateBatteryCommand { get; }

        private string _WH;
        public string WH
        {
            get => _WH;
            set
            {
                SetProperty(ref _WH, value);
                InputValid = format.IsMatch(value);
            }
        }

        private bool _Inputvalid;
        public bool InputValid
        {
            get => _Inputvalid;
            set
            {
                SetProperty(ref _Inputvalid, value);
                if (_Inputvalid) Message = ""; else Message = "Must be from 0 to 999";
            }
        }

        private string message;
        public string Message
        {
            get => message;
            set
            {
                SetProperty(ref message, value);
            }
        }

        public UpdateViewModel(IReadingStore<Reading> readingstore)
        {
            ReadingStore = readingstore;
            Message = "Please enter a value from 0-999";
            UpdateBatteryCommand = new Command(OnUpdateBattery);
        }

        public void OnAppearing()
        {
            Title = $"{Battery.ConnectedDevice?.Name} - Update";
        }

        private async void OnUpdateBattery(object obj)
        {
            Message = "";
            if (!Battery.CanUpdate)
            {
                Message = "Please connect to battery first!";
                return;
            }
            Int16 maxWH = Convert.ToInt16(WH);
            try
            {
                await Battery.UpdateAsync(maxWH);
                Reading reading = new Reading()
                {
                    V = 0,
                    A = 0,
                    W = 0,
                    WH = maxWH,
                    DT = DateTime.Now,
                    Battery = Battery.ConnectedDevice.Name
                };
                int rows = await ReadingStore.AddReadingAsync(reading);
                Message = "Successfully Updated";
            }
            catch (Exception ex)
            {
                Message = "Update Error - " + ex.Message.ToString();
            }
        }
    }
}
