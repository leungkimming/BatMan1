using System;
using System.Collections.Generic;
using Batman2.ViewModels;
using Batman2.Views;
using Xamarin.Forms;

namespace Batman2
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();
//            Routing.RegisterRoute(nameof(DevicePage), typeof(DevicePage));
            Routing.RegisterRoute(nameof(UpdatePage), typeof(UpdatePage));
            Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
            Routing.RegisterRoute(nameof(GaugePage), typeof(GaugePage));
            Routing.RegisterRoute(nameof(SetupPage), typeof(SetupPage));
            Routing.RegisterRoute(nameof(AnalysisPage), typeof(AnalysisPage));
            Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
        }

        //private async void OnMenuItemClicked(object sender, EventArgs e)
        //{
        //    return;
        //    await Shell.Current.GoToAsync("//LoginPage");
        //}
    }
}
