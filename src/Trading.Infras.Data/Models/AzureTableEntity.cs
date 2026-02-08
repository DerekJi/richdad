using Azure;
using Azure.Data.Tables;

namespace Trading.Infras.Data.Models;

/// <summary>
/// Azure Table Storage 实体基类
/// </summary>
public class AzureTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
