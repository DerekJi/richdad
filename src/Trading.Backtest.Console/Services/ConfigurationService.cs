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

        return settings.ToStrategyConfig(
            strategyName,
            _appSettings.Indicators.BaseEma,
            _appSettings.Indicators.AtrPeriod);
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
