using Trading.AlertSystem.Mobile.ViewModels;

namespace Trading.AlertSystem.Mobile.Views;

public partial class AlertHistoryPage : ContentPage
{
    private readonly AlertHistoryViewModel _viewModel;

    public AlertHistoryPage(AlertHistoryViewModel viewModel)
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
