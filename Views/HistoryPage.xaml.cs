namespace BatMan2.Views
{
    public partial class HistoryPage : ContentPage
    {
        HistoryViewModel _viewModel;

        public HistoryPage(HistoryViewModel vm)
        {
            InitializeComponent();

            BindingContext = _viewModel = vm;
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
        }
    }
}
