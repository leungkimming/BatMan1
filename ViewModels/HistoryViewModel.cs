using System.Collections.ObjectModel;

namespace BatMan2.ViewModels
{
    public class HistoryViewModel : BaseViewModel
    {
        private ObservableCollection<ReadingGroup> _Items;
        public ObservableCollection<ReadingGroup> Items
        {
            get { return _Items; }
            set { SetProperty(ref _Items, value); }
        }
        public Command LoadItemsCommand { get; }
        public Command AnalyzeCommand { get; }
        public Command ShareCommand { get; }
        public Command DeleteCommand { get; }
        public Command ImportCommand { get; }
        private string _error;
        public string Error
        {
            get { return _error; }
            set { SetProperty(ref _error, value); }
        }
        private Reading _SelectedGroup;
        public Reading SelectedGroup
        {
            get => _SelectedGroup;
            set
            {
                SetProperty(ref _SelectedGroup, value);
            }
        }

        public Action<bool> onBindingAction { get; set; }

        public HistoryViewModel(IReadingStore<Reading> readingstore)
        {
            ReadingStore = readingstore;
            Items = new ObservableCollection<ReadingGroup>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());

            AnalyzeCommand = new Command(OnAnalyze);
            ShareCommand = new Command(OnShare);
            DeleteCommand = new Command(OnDelete);
            ImportCommand = new Command(OnImport);
            Task.Run(() => this.ExecuteLoadItemsCommand()).Wait();
        }

        async Task ExecuteLoadItemsCommand()
        {
            Title = $"{Battery.ConnectedDevice?.Name} History";
            IsBusy = true;
            onBindingAction?.Invoke(false);
            Items = new ObservableCollection<ReadingGroup>();
            SelectedGroup = null;
            try
            {
                var items = await ReadingStore.GetReadingsAsync();
                string[] batterys = null;

                if (Battery.ConnectedDevice == null)
                {
                    batterys = items.Select(x => x.Battery).Distinct().ToArray();
                } else
                {
                    batterys = new string[] { Battery.ConnectedDevice.Name };
                }

                foreach (string bat in batterys)
                {
                    Items.Add(new ReadingGroup(bat,
                        new ObservableCollection<Reading>(
                            items.Where(x => x.Battery == bat))));
                }
                //	ex	{Foundation.MonoTouchException: Objective-C exception thrown.  Name: NSInternalInconsistencyException Reason: Invalid update: invalid number of sections.  The number of sections contained in the collection view after the update (2) must be equal to the number o…}
                await Task.Delay(500);
                onBindingAction?.Invoke(true);
            }
            catch (Exception ex)
            {
                Error = $"SQLite Error: {ex}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void OnAppearing()
        {
            IsBusy = false;
        }

        private async void OnShare()
        {
            var cacheDir = FileSystem.CacheDirectory;
            var path = Path.Combine(cacheDir, "..", "readings.csv");
            using (StreamWriter file = new StreamWriter(path, append: false))
            {
                await file.WriteLineAsync("\"Battery\", \"DT\", \"WH\", \"W\", \"A\", \"V\"");
                for (int i = Items.Count-1; i >= 0; i--)
                {
                    if ((SelectedGroup == null) ||
                        (Items[i].BatteryG == SelectedGroup.Battery))
                    {
                        for (int j = Items[i].Count - 1; j >= 0; j--)
                        {
                            await file.WriteLineAsync($"\"{Items[i][j].Battery}\",{Items[i][j].DT.ToString("dd/MM/yyyy HH:mm:ss")},{Items[i][j].WH},{Items[i][j].W},{Items[i][j].A},{Items[i][j].V}");
                        }
                    }
                }
                //foreach (var item in Items)
                //{
                //    await file.WriteLineAsync($"\"{item.Battery}\",{item.DT.ToString("dd/MM/yyyy HH:mm:ss")},{item.WH},{item.W},{item.A},{item.V}");
                //}
                file.Close();
            }
            await Share.RequestAsync(new ShareFileRequest
            {
                File = new ShareFile(path),
                Title = "Reading Export"
            });
        }

        private async void OnDelete()
        {
            string mess = "Confirm to empty the Whole database?";
            string bat = null;
            if (Items.Count == 1)
            {
                bat = Items[0].BatteryG;
            }
            if (SelectedGroup != null)
            {
                bat = SelectedGroup.Battery;
            }

            if (bat != null)
            {
                mess = $"Confirmn delete records of Battery - {bat}?";
            }
            bool answer = await App.Current.MainPage.DisplayAlert("Clean up", mess, "Yes", "No");
            if (answer)
            {
                await ReadingStore.ClearDBAsync(bat);
            }
        }

        private async void OnImport()
        {
            bool answer = await App.Current.MainPage.DisplayAlert("Recover DB", "Do you want to recover from export?", "Yes", "No");
            if (answer)
            {
                try
                {
                    await ReadingStore.LoadDB();
                } catch(Exception e)
                {
                    await App.Current.MainPage.DisplayAlert("Error", e.Message, "OK");
                }
            }

        }

        private async void OnAnalyze()
        {
            string bat = null;
            if (Items.Count == 1)
            {
                //SelectedGroup = Items[0][0];
                //bat = SelectedGroup.Battery;
                bat = Items[0].BatteryG;
            }
            if (SelectedGroup != null)
            {
                bat = SelectedGroup.Battery;
            }
            await Shell.Current.GoToAsync($"{ nameof(AnalysisPage)}?BatteryName={bat}");
        }
    }
}
