using Trading.Data.Models;
using Trading.Data.Configuration;

namespace Trading.Backtest.Console.Services;

/// <summary>
/// 配置服务
/// 职责：从appsettings提供策略配置和路径信息
/// </summary>
public class ConfigurationService
{
    private readonly AppSettings _appSettings;

    public ConfigurationService(AppSettings appSettings)
    {
        _appSettings = appSettings;
    }

    /// <summary>
    /// 获取可用的策略名称列表
    /// </summary>
    public List<string> GetAvailableStrategies()
    {
        return _appSettings.Strategies.Keys.ToList();
    }

    /// <summary>
    /// 根据策略名称获取策略配置
    /// </summary>
    public StrategyConfig GetStrategyConfig(string strategyName)
    {
        if (!_appSettings.Strategies.TryGetValue(strategyName, out var settings))
        {
            throw new ArgumentException($"找不到策略配置: {strategyName}");
        }

        // 从策略配置中获取EmaList（PinBar策略特有）
        var emaList = new List<int>(settings.EmaList ?? new List<int>());
        
        // 确保BaseEma在列表中
        if (_appSettings.Indicators.BaseEma > 0 && !emaList.Contains(_appSettings.Indicators.BaseEma))
        {
            emaList.Add(_appSettings.Indicators.BaseEma);
        }

        return new StrategyConfig
        {
            StrategyName = strategyName,
            Symbol = settings.Symbol,
            ContractSize = settings.Symbol == "XAUUSD" ? 100 : 
                           settings.Symbol == "XAGUSD" ? 1000 : 100, // 默认100
            BaseEma = _appSettings.Indicators.BaseEma,
            AtrPeriod = _appSettings.Indicators.AtrPeriod,
            RiskRewardRatio = (decimal)settings.RiskRewardRatio,
            StopLossAtrRatio = (decimal)settings.StopLossAtrRatio,
            MinLowerWickAtrRatio = (decimal)settings.MinLowerWickAtrRatio,
            Threshold = (decimal)settings.Threshold,
            NearEmaThreshold = (decimal)settings.NearEmaThreshold,
            StartTradingHour = settings.StartTradingHour,
            EndTradingHour = settings.EndTradingHour,
            NoTradingHoursLimit = settings.NoTradingHoursLimit,
            RequirePinBarDirectionMatch = settings.RequirePinBarDirectionMatch,
            MaxBodyPercentage = (decimal)settings.MaxBodyPercentage,
            MinLongerWickPercentage = (decimal)settings.MinLongerWickPercentage,
            MaxShorterWickPercentage = (decimal)settings.MaxShorterWickPercentage,
            EmaList = emaList
        };
    }

    /// <summary>
    /// 获取账户配置
    /// </summary>
    public AccountSettings GetAccountSettings()
    {
        return _appSettings.Account;
    }

    /// <summary>
    /// 获取数据目录路径
    /// </summary>
    public string GetDataDirectory()
    {
        var dataPath = _appSettings.DataPath;
        
        // 处理相对路径
        if (!Path.IsPathRooted(dataPath))
        {
            dataPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dataPath));
        }

        if (!Directory.Exists(dataPath))
        {
            throw new DirectoryNotFoundException($"数据目录不存在: {dataPath}");
        }

        return dataPath;
    }
}
