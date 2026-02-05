using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Trading.AlertSystem.Mobile.Models;
using Trading.AlertSystem.Mobile.Services;

namespace Trading.AlertSystem.Mobile.ViewModels;

public partial class EmaConfigViewModel : ObservableObject
{
    private readonly IAlertApiClient _apiClient;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    // 配置字段
    [ObservableProperty]
    private bool _enabled;

    [ObservableProperty]
    private string _symbols = string.Empty;

    [ObservableProperty]
    private string _timeFrames = string.Empty;

    [ObservableProperty]
    private string _emaPeriods = string.Empty;

    [ObservableProperty]
    private int _historyMultiplier = 3;

    [ObservableProperty]
    private string _lastUpdated = string.Empty;

    public EmaConfigViewModel(IAlertApiClient apiClient)
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

            var config = await _apiClient.GetEmaConfigAsync();
            if (config != null)
            {
                Enabled = config.Enabled;
                Symbols = string.Join(", ", config.Symbols);
                TimeFrames = string.Join(", ", config.TimeFrames);
                EmaPeriods = string.Join(", ", config.EmaPeriods);
                HistoryMultiplier = config.HistoryMultiplier;
                LastUpdated = config.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                // 默认值
                Enabled = false;
                Symbols = "XAUUSD, XAGUSD";
                TimeFrames = "M5, M15, H1";
                EmaPeriods = "20, 60, 120";
                HistoryMultiplier = 3;
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
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // 解析输入
        var symbolList = ParseStringList(Symbols);
        var timeFrameList = ParseStringList(TimeFrames);
        var emaPeriodList = ParseIntList(EmaPeriods);

        // 验证
        if (symbolList.Count == 0)
        {
            await Shell.Current.DisplayAlert("验证失败", "请至少输入一个交易品种", "确定");
            return;
        }

        if (timeFrameList.Count == 0)
        {
            await Shell.Current.DisplayAlert("验证失败", "请至少选择一个时间周期", "确定");
            return;
        }

        var validTimeFrames = new[] { "M1", "M5", "M15", "M30", "H1", "H4", "D1" };
        foreach (var tf in timeFrameList)
        {
            if (!validTimeFrames.Contains(tf))
            {
                await Shell.Current.DisplayAlert("验证失败", $"无效的时间周期: {tf}", "确定");
                return;
            }
        }

        if (emaPeriodList.Count == 0)
        {
            await Shell.Current.DisplayAlert("验证失败", "请至少输入一个EMA周期", "确定");
            return;
        }

        if (HistoryMultiplier < 1 || HistoryMultiplier > 10)
        {
            await Shell.Current.DisplayAlert("验证失败", "历史数据倍数必须在1-10之间", "确定");
            return;
        }

        try
        {
            IsSaving = true;

            var config = new EmaMonitoringConfig
            {
                Id = "default",
                Enabled = Enabled,
                Symbols = symbolList,
                TimeFrames = timeFrameList,
                EmaPeriods = emaPeriodList,
                HistoryMultiplier = HistoryMultiplier
            };

            var result = await _apiClient.UpdateEmaConfigAsync(config);
            if (result != null)
            {
                LastUpdated = result.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                await Shell.Current.DisplayAlert("成功", "EMA配置已保存", "确定");
            }
            else
            {
                await Shell.Current.DisplayAlert("错误", "保存失败", "确定");
            }
        }
        finally
        {
            IsSaving = false;
        }
    }

    private List<string> ParseStringList(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<string>();

        return input.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim().ToUpperInvariant())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .ToList();
    }

    private List<int> ParseIntList(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<int>();

        var result = new List<int>();
        var parts = input.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out var value) && value > 0 && value <= 500)
            {
                result.Add(value);
            }
        }
        return result.Distinct().OrderBy(x => x).ToList();
    }
}
