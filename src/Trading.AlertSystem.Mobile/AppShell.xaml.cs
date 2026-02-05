using Trading.AlertSystem.Mobile.Views;

namespace Trading.AlertSystem.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // 注册路由（用于导航到详情页等）
        Routing.RegisterRoute(nameof(AlertDetailPage), typeof(AlertDetailPage));
    }
}
