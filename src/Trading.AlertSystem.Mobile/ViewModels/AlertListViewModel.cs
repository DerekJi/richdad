using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Trading.AlertSystem.Mobile.Models;
using Trading.AlertSystem.Mobile.Services;
using Trading.AlertSystem.Mobile.Views;

namespace Trading.AlertSystem.Mobile.ViewModels;

public partial class AlertListViewModel : ObservableObject
{
    private readonly IAlertApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<PriceMonitorRule> _rules = new();

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public AlertListViewModel(IAlertApiClient apiClient)
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

            var data = await _apiClient.GetAllRulesAsync();

            Rules.Clear();
            foreach (var rule in data.OrderBy(r => r.Symbol).ThenBy(r => r.Name))
            {
                Rules.Add(rule);
            }
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
    private async Task AddRuleAsync()
    {
        await Shell.Current.GoToAsync(nameof(AlertDetailPage), new Dictionary<string, object>
        {
            { "RuleId", string.Empty },
            { "IsNew", true }
        });
    }

    [RelayCommand]
    private async Task EditRuleAsync(PriceMonitorRule rule)
    {
        await Shell.Current.GoToAsync(nameof(AlertDetailPage), new Dictionary<string, object>
        {
            { "RuleId", rule.Id },
            { "IsNew", false }
        });
    }

    [RelayCommand]
    private async Task DeleteRuleAsync(PriceMonitorRule rule)
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "确认删除",
            $"确定要删除告警规则 \"{rule.Name}\" 吗？",
            "删除",
            "取消");

        if (!confirm) return;

        var success = await _apiClient.DeleteRuleAsync(rule.Id);
        if (success)
        {
            Rules.Remove(rule);
        }
        else
        {
            await Shell.Current.DisplayAlert("错误", "删除失败", "确定");
        }
    }

    [RelayCommand]
    private async Task ToggleRuleAsync(PriceMonitorRule rule)
    {
        var request = new UpdateRuleRequest
        {
            Enabled = !rule.Enabled
        };

        var updated = await _apiClient.UpdateRuleAsync(rule.Id, request);
        if (updated != null)
        {
            var index = Rules.IndexOf(rule);
            if (index >= 0)
            {
                Rules[index] = updated;
            }
        }
    }

    [RelayCommand]
    private async Task ResetTriggerAsync(PriceMonitorRule rule)
    {
        if (!rule.IsTriggered)
        {
            await Shell.Current.DisplayAlert("提示", "该规则尚未触发", "确定");
            return;
        }

        var success = await _apiClient.ResetRuleTriggerAsync(rule.Id);
        if (success)
        {
            await LoadDataAsync();
        }
        else
        {
            await Shell.Current.DisplayAlert("错误", "重置失败", "确定");
        }
    }
}
