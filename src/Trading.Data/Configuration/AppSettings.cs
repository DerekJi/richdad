namespace Trading.Data.Configuration;

public class AppSettings
{
    public CosmosDbSettings CosmosDb { get; set; } = new();
    public string DataPath { get; set; } = string.Empty;
    public Dictionary<string, StrategySettings> Strategies { get; set; } = new();
}
