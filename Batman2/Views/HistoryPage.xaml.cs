using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Batman2.Models;
using Batman2.Views;
using Batman2.ViewModels;
using Plugin.DeviceOrientation;
using Plugin.DeviceOrientation.Abstractions;

namespace Batman2.Views
{
    public partial class HistoryPage : ContentPage
    {
        HistoryViewModel _viewModel;

        public HistoryPage()
        {
            InitializeComponent();

            BindingContext = _viewModel = new HistoryViewModel();
            ItemsListView.SetBinding(ItemsView.ItemsSourceProperty, "Items");
            //ItemsListView.SetBinding(SelectableItemsView.SelectedItemProperty, "SelectedGroup");

            _viewModel.onBindingAction = ((o) =>
            {
                if (o)
                {
                    ItemsListView.SetBinding(ItemsView.ItemsSourceProperty, "Items");
                    //ItemsListView.SetBinding(SelectableItemsView.SelectedItemProperty, "SelectedGroup");
                }
                else
                {
                    ItemsListView.RemoveBinding(ItemsView.ItemsSourceProperty);
                }
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.OnAppearing();
            CrossDeviceOrientation.Current.UnlockOrientation();
        }
    }
}
