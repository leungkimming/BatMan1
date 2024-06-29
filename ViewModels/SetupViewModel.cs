using System.Text.RegularExpressions;

namespace BatMan2.ViewModels
{
    public class SetupViewModel : BaseViewModel
    {
        private readonly Regex format = new Regex(@"^[1-9][0-9]{1,2}$|^\d$");

        public Command SetupGaugeCommand { get; }

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

        private string _Alert;
        public string Alert
        {
            get => _Alert;
            set
            {
                SetProperty(ref _Alert, value);
                InputValid = format.IsMatch(value) && (Convert.ToInt16(WH) > Convert.ToInt16(value));
            }
        }

        private bool _Inputvalid;
        public bool InputValid
        {
            get => _Inputvalid;
            set
            {
                SetProperty(ref _Inputvalid, value);
                if (_Inputvalid) Message = ""; else Message = "Must be from 0 to 999 and Max must be larger than Alert";
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

        public SetupViewModel()
        {
            Message = "Please enter values from 0-999. Max must be larger than Alert";
            SetupGaugeCommand = new Command(OnSetupGauge);
        }

        public void OnAppearing()
        {
            Title = $"{Battery.ConnectedDevice?.Name} - Max/Alert WH";
            WH = Preferences.Get(Battery.ConnectedDevice?.Name+"_wh", 0).ToString();
            Alert = Preferences.Get(Battery.ConnectedDevice?.Name + "_alert", 0).ToString();
        }

        private void OnSetupGauge(object obj)
        {
            Message = "";
            //if (!Battery.CanUpdate)
            //{
            //    Message = "Please connect to battery first!";
            //    return;
            //}
            Int16 maxWH = Convert.ToInt16(WH);
            Preferences.Set(Battery.ConnectedDevice?.Name + "_wh", maxWH);
            Int16 minWH = Convert.ToInt16(Alert);
            Preferences.Set(Battery.ConnectedDevice?.Name + "_alert", minWH);
            Message = "Successfully Setup";
        }
    }
}
