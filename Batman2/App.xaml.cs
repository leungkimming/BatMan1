using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Batman2.Services;
using Batman2.Views;
using Batman2.ViewModels;

namespace Batman2
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            IBattery realBattery = new Battery();
            DependencyService.RegisterSingleton<IBattery>(realBattery);

            DependencyService.Register<ReadingStore>();

            DependencyService.Register<GaugeViewModel>();

            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
