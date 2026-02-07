# Azure Table Storage 集成指南

## 概述

为了大幅降低存储成本，系统已从 Cosmos DB 迁移到 **Azure Table Storage**。Azure Table Storage 提供了 NoSQL 键值存储，性能优异且成本极低。

## 💰 成本对比

### Cosmos DB vs Azure Table Storage

| 指标 | Cosmos DB | Azure Table Storage | 节省 |
|------|-----------|---------------------|------|
| 存储成本 | $0.25/GB/月 | $0.045/GB/月 | **82%** |
| 写入操作 | $0.25/百万 RU | $0.05/10万次 | **80%** |
| 读取操作 | $0.25/百万 RU | $0.004/10万次 | **98%** |
| 最小费用 | ~$25/月 | ~$0.50/月 | **98%** |

**典型使用场景月成本：**
- Cosmos DB: $30-50/月
- Azure Table Storage: **$1-3/月** ⚡

## 🚀 快速开始

### 1. 创建 Azure Storage Account

```bash
# 使用 Azure CLI
az storage account create \
  --name yourstorageaccount \
  --resource-group your-resource-group \
  --location eastus \
  --sku Standard_LRS

# 获取连接字符串
az storage account show-connection-string \
  --name yourstorageaccount \
  --resource-group your-resource-group
```

或在 Azure Portal：
1. 创建 Storage Account
2. 选择 Performance: **Standard**
3. 选择 Replication: **LRS (本地冗余)** 最便宜
4. 复制连接字符串

### 2. 配置应用

#### 方式 1: appsettings.json（不推荐生产环境）

```json
{
  "AzureTableStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...",
    "Enabled": true
  }
}
```

#### 方式 2: User Secrets（推荐开发环境）

```bash
cd src/Trading.AlertSystem.Web
dotnet user-secrets set "AzureTableStorage:ConnectionString" "YOUR_CONNECTION_STRING"
dotnet user-secrets set "AzureTableStorage:Enabled" "true"
```

#### 方式 3: 环境变量（推荐生产环境）

```bash
export AzureTableStorage__ConnectionString="YOUR_CONNECTION_STRING"
export AzureTableStorage__Enabled="true"
```

### 3. 启动应用

```bash
cd src/Trading.AlertSystem.Web
dotnet run
```

启动日志会显示：
```
✅ Azure Table Storage 客户端已初始化
🔄 开始初始化 Azure Table Storage 表...
✅ 表已创建或已存在: PriceMonitor
✅ 表已创建或已存在: AlertHistory
...
✅ 使用 Azure Table Storage 作为持久化存储
```

## 📊 数据结构设计

### 表结构

Azure Table Storage 使用 **PartitionKey** 和 **RowKey** 来组织数据：

| 表名 | PartitionKey | RowKey | 用途 |
|------|--------------|--------|------|
| PriceMonitor | "PriceMonitor" | RuleId | 价格监控规则 |
| AlertHistory | "Alert_YYYYMMDD" | AlertId | 告警历史（按日期分区） |
| PinBarSignal | "PinBar_Symbol" | SignalId | Pin Bar 信号 |
| AIAnalysisHistory | "AI_YYYYMMDD" | AnalysisId | AI 分析历史 |

### 优化策略

1. **日期分区**：AlertHistory 和 AIAnalysisHistory 按日期分区，提高查询性能
2. **符号分区**：PinBar 信号按交易品种分区
3. **批量操作**：使用 Table Batch Operations 提高写入性能

## 🔄 从 Cosmos DB 迁移

### Cosmos DB 已禁用

系统已将 `CosmosDb:ConnectionString` 清空，确保 Cosmos DB 不会被使用。

```json
{
  "CosmosDb": {
    "ConnectionString": "",  // 已清空
    "_comment": "Cosmos DB 已禁用以降低成本，请使用 Azure Table Storage"
  }
}
```

### 数据迁移（可选）

如果你有现有的 Cosmos DB 数据需要迁移：

```bash
# TODO: 创建迁移脚本
dotnet run -- migrate --from cosmosdb --to azuretable
```

## 📝 配置说明

### 完整配置选项

```json
{
  "AzureTableStorage": {
    "ConnectionString": "YOUR_CONNECTION_STRING",
    "Enabled": true,
    "PriceMonitorTableName": "PriceMonitor",
    "AlertHistoryTableName": "AlertHistory",
    "EmaMonitorTableName": "EmaMonitor",
    "DataSourceConfigTableName": "DataSourceConfig",
    "EmailConfigTableName": "EmailConfig",
    "PinBarMonitorTableName": "PinBarMonitor",
    "PinBarSignalTableName": "PinBarSignal",
    "AIAnalysisHistoryTableName": "AIAnalysisHistory"
  }
}
```

### 表名自定义

如果需要自定义表名（例如多环境共用一个 Storage Account）：

```json
{
  "AzureTableStorage": {
    "PriceMonitorTableName": "DevPriceMonitor",
    "AlertHistoryTableName": "DevAlertHistory"
  }
}
```

## 🎯 性能优化

### 查询优化

1. **使用 PartitionKey 过滤**（最快）
```csharp
filter: $"PartitionKey eq 'Alert_20260207'"
```

2. **使用 RowKey 范围查询**（快）
```csharp
filter: $"PartitionKey eq 'PinBar_XAUUSD' and RowKey ge '{startId}' and RowKey le '{endId}'"
```

3. **避免全表扫描**（慢）
```csharp
// 避免这样做
filter: $"Symbol eq 'XAUUSD'"  // 如果 Symbol 不在 PartitionKey 中
```

### 批量操作

```csharp
// 批量写入（最多 100 个实体）
var batch = new List<TableTransactionAction>();
foreach (var entity in entities)
{
    batch.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
}
await tableClient.SubmitTransactionAsync(batch);
```

## ⚠️ 限制和注意事项

### Azure Table Storage 限制

1. **实体大小**: 最大 1 MB
2. **属性数量**: 最多 252 个属性
3. **批量操作**: 同一个 PartitionKey，最多 100 个实体
4. **事务**: 仅支持同一 PartitionKey 内的事务

### 不适合的场景

❌ **不推荐使用 Azure Table Storage 的场景：**
- 需要复杂查询（多条件、JOIN）
- 需要聚合查询（SUM, AVG, GROUP BY）
- 需要全文搜索
- 数据量 > 100GB 且需要复杂查询

✅ **适合 Azure Table Storage 的场景：**
- 键值查询（本系统的主要场景）
- 时序数据存储
- 日志和监控数据
- 配置存储
- 简单的 CRUD 操作

## 🔧 故障排查

### 问题：连接失败

**错误**: `The remote name could not be resolved`

**解决**:
1. 检查连接字符串是否正确
2. 检查网络连接
3. 确认 Storage Account 存在

### 问题：表不存在

**错误**: `Table not found`

**解决**: 系统会自动创建表，确保应用有足够权限

### 问题：写入失败

**错误**: `Entity already exists`

**解决**: 检查 RowKey 是否重复，使用 `UpdateEntityAsync` 而非 `AddEntityAsync`

## 📈 监控和日志

### 启用诊断日志

在 Azure Portal 中：
1. 打开 Storage Account
2. 进入 **Monitoring** > **Diagnostic settings**
3. 启用 **Table logs**
4. 选择日志目标（Log Analytics 或 Storage Account）

### 性能指标

关键指标：
- **Availability**: 可用性
- **E2E Latency**: 端到端延迟
- **Server Latency**: 服务器延迟
- **Success Rate**: 成功率

## 🔐 安全最佳实践

### 1. 使用 Managed Identity（推荐）

```csharp
// 使用 Azure 托管标识，无需连接字符串
var tableClient = new TableClient(
    new Uri($"https://{accountName}.table.core.windows.net"),
    tableName,
    new DefaultAzureCredential()
);
```

### 2. 连接字符串加密

永远不要将连接字符串提交到源代码管理：
- ✅ 使用 User Secrets（开发）
- ✅ 使用环境变量（生产）
- ✅ 使用 Azure Key Vault
- ❌ 不要写在 appsettings.json 中

### 3. 网络安全

启用防火墙规则：
```bash
az storage account update \
  --name yourstorageaccount \
  --default-action Deny

az storage account network-rule add \
  --account-name yourstorageaccount \
  --ip-address YOUR_IP
```

## 💡 最佳实践

1. **使用有意义的 PartitionKey**
   - 按日期分区历史数据
   - 按品种分区交易数据
   - 避免热分区（单一 PartitionKey 过多数据）

2. **合理设计 RowKey**
   - 使用 GUID 确保唯一性
   - 或使用时间戳倒序（最新数据在前）

3. **数据清理策略**
   - 定期删除旧数据（如 90 天前的告警历史）
   - 或归档到 Blob Storage

4. **成本监控**
   - 设置成本告警
   - 定期检查存储使用量
   - 优化查询模式

## 📚 相关文档

- [Azure Table Storage 官方文档](https://docs.microsoft.com/azure/storage/tables/)
- [定价详情](https://azure.microsoft.com/pricing/details/storage/tables/)
- [.NET SDK 参考](https://docs.microsoft.com/dotnet/api/azure.data.tables)

## 🔄 回退到 Cosmos DB

如果将来需要回退到 Cosmos DB：

1. 在 `Program.cs` 中取消注释 Cosmos DB 代码
2. 设置 `CosmosDb:ConnectionString`
3. 设置 `AzureTableStorage:Enabled` 为 `false`
4. 重启应用

---

**版本**: 1.0
**更新时间**: 2026-02-08
**成本节省**: ~98% 💰
