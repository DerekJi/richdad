using Trading.AlertSystem.Mobile.ViewModels;

namespace Trading.AlertSystem.Mobile.Views;

public partial class MonitorStatusPage : ContentPage
{
    private readonly MonitorStatusViewModel _viewModel;

    public MonitorStatusPage(MonitorStatusViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDataCommand.ExecuteAsync(null);
        _viewModel.StartAutoRefresh();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopAutoRefresh();
    }
}
