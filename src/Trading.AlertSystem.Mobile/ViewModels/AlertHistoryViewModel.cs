using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Trading.AlertSystem.Mobile.Models;
using Trading.AlertSystem.Mobile.Services;

namespace Trading.AlertSystem.Mobile.ViewModels;

public partial class AlertHistoryViewModel : ObservableObject
{
    private readonly IAlertApiClient _apiClient;
    private int _currentPage = 1;
    private const int PageSize = 50;
    private bool _hasMoreData = true;

    [ObservableProperty]
    private ObservableCollection<AlertHistory> _items = new();

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoadingMore;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private int _totalCount;

    public AlertHistoryViewModel(IAlertApiClient apiClient)
    {
        _apiClient = apiClient;
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
            _currentPage = 1;
            _hasMoreData = true;

            var response = await _apiClient.GetAlertHistoryAsync(_currentPage, PageSize);

            Items.Clear();
            foreach (var item in response.Items)
            {
                Items.Add(item);
            }

            TotalCount = response.TotalCount;
            _hasMoreData = _currentPage < response.TotalPages;
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

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsLoadingMore || !_hasMoreData) return;

        try
        {
            IsLoadingMore = true;
            _currentPage++;

            var response = await _apiClient.GetAlertHistoryAsync(_currentPage, PageSize);

            foreach (var item in response.Items)
            {
                Items.Add(item);
            }

            _hasMoreData = _currentPage < response.TotalPages;
        }
        catch (Exception ex)
        {
            _currentPage--; // 回退页码
            await Shell.Current.DisplayAlert("错误", $"加载更多失败: {ex.Message}", "确定");
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    [RelayCommand]
    private async Task ViewDetailAsync(AlertHistory item)
    {
        var message = $"品种: {item.Symbol}\n" +
                      $"类型: {item.TypeDisplay}\n" +
                      $"时间: {item.TimeDisplay}\n" +
                      $"消息: {item.Message}\n" +
                      $"已发送: {(item.IsSent ? "是" : "否")}";

        await Shell.Current.DisplayAlert("告警详情", message, "关闭");
    }
}
