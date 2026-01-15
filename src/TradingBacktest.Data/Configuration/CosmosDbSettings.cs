namespace TradingBacktest.Data.Configuration;

public class CosmosDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string BacktestContainerName { get; set; } = string.Empty;
    public string ConfigContainerName { get; set; } = string.Empty;
}
