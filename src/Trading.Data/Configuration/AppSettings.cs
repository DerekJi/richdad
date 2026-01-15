namespace Trading.Data.Configuration;

public class AppSettings
{
    public CosmosDbSettings CosmosDb { get; set; } = new();
    public string DataPath { get; set; } = string.Empty;
    public IndicatorSettings Indicators { get; set; } = new();
    public AccountSettings Account { get; set; } = new();
    public Dictionary<string, StrategySettings> Strategies { get; set; } = new();
}
