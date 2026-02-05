using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trading.AlertSystem.Mobile.Models;
using Trading.AlertSystem.Mobile.Services;

namespace Trading.AlertSystem.Mobile.ViewModels;

[QueryProperty(nameof(RuleId), "RuleId")]
[QueryProperty(nameof(IsNew), "IsNew")]
public partial class AlertDetailViewModel : ObservableObject
{
    private readonly IAlertApiClient _apiClient;

    [ObservableProperty]
    private string _ruleId = string.Empty;

    [ObservableProperty]
    private bool _isNew;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _pageTitle = "新建告警";

    // 表单字段
    [ObservableProperty]
    private string _symbol = "XAUUSD";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _selectedTypeIndex;

    [ObservableProperty]
    private string _targetPrice = string.Empty;

    [ObservableProperty]
    private int _selectedDirectionIndex;

    [ObservableProperty]
    private string _emaPeriod = "20";

    [ObservableProperty]
    private string _maPeriod = "20";

    [ObservableProperty]
    private int _selectedTimeFrameIndex = 1; // M5

    [ObservableProperty]
    private bool _enabled = true;

    // 类型选项
    public List<string> TypeOptions { get; } = new() { "固定价格", "EMA", "MA" };

    // 方向选项
    public List<string> DirectionOptions { get; } = new() { "上穿", "下穿" };

    // 时间周期选项
    public List<string> TimeFrameOptions { get; } = new() { "M1", "M5", "M15", "M30", "H1", "H4", "D1" };

    // 常用品种
    public List<string> SymbolOptions { get; } = new() { "XAUUSD", "XAGUSD", "EURUSD", "GBPUSD", "USDJPY" };

    public AlertDetailViewModel(IAlertApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    partial void OnRuleIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) && !IsNew)
        {
            _ = LoadRuleAsync();
        }
    }

    partial void OnIsNewChanged(bool value)
    {
        PageTitle = value ? "新建告警" : "编辑告警";
    }

    private async Task LoadRuleAsync()
    {
        if (string.IsNullOrEmpty(RuleId)) return;

        try
        {
            IsLoading = true;
            var rule = await _apiClient.GetRuleByIdAsync(RuleId);
            if (rule != null)
            {
                Symbol = rule.Symbol;
                Name = rule.Name;
                SelectedTypeIndex = (int)rule.Type;
                TargetPrice = rule.TargetPrice?.ToString() ?? string.Empty;
                SelectedDirectionIndex = (int)rule.Direction;
                EmaPeriod = rule.EmaPeriod?.ToString() ?? "20";
                MaPeriod = rule.MaPeriod?.ToString() ?? "20";
                SelectedTimeFrameIndex = TimeFrameOptions.IndexOf(rule.TimeFrame);
                if (SelectedTimeFrameIndex < 0) SelectedTimeFrameIndex = 1;
                Enabled = rule.Enabled;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // 验证
        if (string.IsNullOrWhiteSpace(Symbol))
        {
            await Shell.Current.DisplayAlert("验证失败", "请选择交易品种", "确定");
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlert("验证失败", "请输入告警名称", "确定");
            return;
        }

        var alertType = (AlertType)SelectedTypeIndex;

        if (alertType == AlertType.FixedPrice)
        {
            if (!decimal.TryParse(TargetPrice, out var price) || price <= 0)
            {
                await Shell.Current.DisplayAlert("验证失败", "请输入有效的目标价格", "确定");
                return;
            }
        }

        try
        {
            IsSaving = true;

            if (IsNew)
            {
                var request = new CreateRuleRequest
                {
                    Symbol = Symbol,
                    Name = Name,
                    Type = alertType,
                    TargetPrice = decimal.TryParse(TargetPrice, out var tp) ? tp : null,
                    Direction = (PriceDirection)SelectedDirectionIndex,
                    EmaPeriod = int.TryParse(EmaPeriod, out var ep) ? ep : null,
                    MaPeriod = int.TryParse(MaPeriod, out var mp) ? mp : null,
                    TimeFrame = TimeFrameOptions[SelectedTimeFrameIndex],
                    Enabled = Enabled
                };

                var result = await _apiClient.CreateRuleAsync(request);
                if (result != null)
                {
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert("错误", "创建失败", "确定");
                }
            }
            else
            {
                var request = new UpdateRuleRequest
                {
                    Symbol = Symbol,
                    Name = Name,
                    Type = alertType,
                    TargetPrice = decimal.TryParse(TargetPrice, out var tp) ? tp : null,
                    Direction = (PriceDirection)SelectedDirectionIndex,
                    EmaPeriod = int.TryParse(EmaPeriod, out var ep) ? ep : null,
                    MaPeriod = int.TryParse(MaPeriod, out var mp) ? mp : null,
                    TimeFrame = TimeFrameOptions[SelectedTimeFrameIndex],
                    Enabled = Enabled
                };

                var result = await _apiClient.UpdateRuleAsync(RuleId, request);
                if (result != null)
                {
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert("错误", "更新失败", "确定");
                }
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
