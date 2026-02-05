using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Trading.AlertSystem.Mobile.Models;
using Trading.AlertSystem.Mobile.Services;

namespace Trading.AlertSystem.Mobile.ViewModels;

public partial class MonitorStatusViewModel : ObservableObject
{
    private readonly IAlertApiClient _apiClient;
    private readonly ISettingsService _settings;
    private CancellationTokenSource? _autoRefreshCts;

    [ObservableProperty]
    private ObservableCollection<MonitorStatusItem> _items = new();

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _lastUpdateTime = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public MonitorStatusViewModel(IAlertApiClient apiClient, ISettingsService settings)
    {
        _apiClient = apiClient;
        _settings = settings;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            var data = await _apiClient.GetMonitorStatusAsync();

            Items.Clear();
            foreach (var item in data)
            {
                Items.Add(item);
            }

            LastUpdateTime = $"更新于 {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadDataAsync();
    }

    public void StartAutoRefresh()
    {
        StopAutoRefresh();

        if (!_settings.AutoRefresh) return;

        _autoRefreshCts = new CancellationTokenSource();
        var token = _autoRefreshCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.RefreshIntervalSeconds), token);
                if (!token.IsCancellationRequested)
                {
                    MainThread.BeginInvokeOnMainThread(async () => await LoadDataAsync());
                }
            }
        }, token);
    }

    public void StopAutoRefresh()
    {
        _autoRefreshCts?.Cancel();
        _autoRefreshCts?.Dispose();
        _autoRefreshCts = null;
    }
}
