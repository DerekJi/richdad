using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trading.AlertSystem.Mobile.Services;

namespace Trading.AlertSystem.Mobile.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IAlertApiClient _apiClient;

    [ObservableProperty]
    private string _serverUrl = string.Empty;

    [ObservableProperty]
    private bool _autoRefresh;

    [ObservableProperty]
    private int _refreshIntervalSeconds;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private string _connectionStatus = string.Empty;

    [ObservableProperty]
    private Color _connectionStatusColor = Colors.Gray;

    public SettingsViewModel(ISettingsService settings, IAlertApiClient apiClient)
    {
        _settings = settings;
        _apiClient = apiClient;
        LoadSettings();
    }

    private void LoadSettings()
    {
        ServerUrl = _settings.ServerUrl;
        AutoRefresh = _settings.AutoRefresh;
        RefreshIntervalSeconds = _settings.RefreshIntervalSeconds;
    }

    [RelayCommand]
    private void Save()
    {
        _settings.ServerUrl = ServerUrl;
        _settings.AutoRefresh = AutoRefresh;
        _settings.RefreshIntervalSeconds = RefreshIntervalSeconds;
        _settings.Save();

        Shell.Current.DisplayAlert("成功", "设置已保存", "确定");
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (IsTesting) return;

        try
        {
            IsTesting = true;
            ConnectionStatus = "测试中...";
            ConnectionStatusColor = Colors.Gray;

            // 先临时保存 URL
            var originalUrl = _settings.ServerUrl;
            _settings.ServerUrl = ServerUrl;

            var success = await _apiClient.TestConnectionAsync();

            // 恢复原 URL（如果用户没有点保存）
            _settings.ServerUrl = originalUrl;

            if (success)
            {
                ConnectionStatus = "✓ 连接成功";
                ConnectionStatusColor = Colors.Green;
            }
            else
            {
                ConnectionStatus = "✗ 连接失败";
                ConnectionStatusColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"✗ 错误: {ex.Message}";
            ConnectionStatusColor = Colors.Red;
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        ServerUrl = "http://10.0.2.2:5000";
        AutoRefresh = true;
        RefreshIntervalSeconds = 30;
    }
}
