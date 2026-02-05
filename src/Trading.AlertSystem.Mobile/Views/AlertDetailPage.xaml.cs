using Trading.AlertSystem.Mobile.ViewModels;

namespace Trading.AlertSystem.Mobile.Views;

public partial class AlertDetailPage : ContentPage
{
    public AlertDetailPage(AlertDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
