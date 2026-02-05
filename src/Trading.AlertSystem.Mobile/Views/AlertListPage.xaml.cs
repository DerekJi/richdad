using Trading.AlertSystem.Mobile.ViewModels;

namespace Trading.AlertSystem.Mobile.Views;

public partial class AlertListPage : ContentPage
{
    private readonly AlertListViewModel _viewModel;

    public AlertListPage(AlertListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
    }
}
