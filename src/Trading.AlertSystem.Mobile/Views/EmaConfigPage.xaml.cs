using Trading.AlertSystem.Mobile.ViewModels;

namespace Trading.AlertSystem.Mobile.Views;

public partial class EmaConfigPage : ContentPage
{
    private readonly EmaConfigViewModel _viewModel;

    public EmaConfigPage(EmaConfigViewModel viewModel)
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
