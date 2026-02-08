## Issue 1: 实现 Azure Table Storage 持久化存储

### 标题
💾 Implement Azure Table Storage for Cost-Efficient Data Persistence

### 描述
将系统从Cosmos DB迁移到Azure Table Storage，实现低成本、高性能的NoSQL持久化存储方案。Azure Table Storage提供了与Cosmos DB相当的性能，但成本仅为其2%。

### 背景
当前系统使用内存存储或Cosmos DB作为持久化方案，但存在以下问题：
- 内存存储：数据在应用重启后丢失，无法用于生产环境
- Cosmos DB：成本高昂（$30-50/月），对于小规模应用负担过重

通过集成Azure Table Storage，系统可以实现：
- **98%成本节省**：从$30-50/月降至$1-3/月
- 高性能NoSQL存储
- 按需付费，无最低消费
- 99.9%可用性保证

### 实现功能

#### ✅ 1. 核心基础设施层
**新增项目组件：** `Trading.AlertSystem.Data`

**配置类：**
- `AzureTableStorageSettings` - 统一配置管理
  - ConnectionString
  - 各表名配置（AlertHistory、PriceMonitor、EmaMonitor等）
  - Enabled 开关

**上下文类：**
- `AzureTableStorageContext` - Azure Table Storage 连接管理
  - 初始化所有表（自动创建不存在的表）
  - 提供 TableClient 获取接口
  - 连接状态检查

#### ✅ 2. 告警历史持久化
**新增仓储：** `AzureTableAlertHistoryRepository`

**核心功能：**
- ✅ 添加告警记录 - `AddAsync(AlertHistory)`
- ✅ 按ID查询 - `GetByIdAsync(string)`
- ✅ 分页查询 - `GetAllAsync()` 支持筛选：
  - 按类型筛选（PriceAlert、EmaAlert、PinBar等）
  - 按交易品种筛选
  - 按时间范围筛选
  - 分页和排序
- ✅ 批量添加 - `AddBatchAsync(IEnumerable<AlertHistory>)`
- ✅ 删除记录 - `DeleteAsync(string)`
- ✅ 统计查询 - `GetCountAsync()` 按类型统计

**设计亮点：**
- 使用日期作为 PartitionKey (`Alert_yyyyMMdd`) 优化查询性能
- 支持跨分区查询（按日期范围遍历）
- 批量操作优化（每批最多100条）

#### ✅ 3. 配置和依赖注入
**新增配置类：** `AzureTableStorageConfiguration`

**服务注册：**
```csharp
// 自动检测配置，按需注册
builder.Services.AddAzureTableStorageServices(builder.Configuration);
```

**初始化流程：**
```csharp
// 自动创建所有表
await app.InitializeAzureTableStorageAsync();
```

#### ✅ 4. 存储后备方案（Fallback）
**新增配置类：** `StorageConfiguration`

**智能存储选择：**
1. 优先使用 Azure Table Storage（如果已配置且启用）
2. 降级到内存存储（开发/测试环境）
3. 自动补充缺失的仓储实现

**混合模式支持：**
- Azure Table + InMemory 混合模式
- 当某些仓储未实现 Azure Table 版本时，自动使用内存版本
- 日志清晰标识使用的存储类型

#### ✅ 5. 配置管理
**appsettings.json 配置：**
```json
{
  "AzureTableStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;...",
    "Enabled": true,
    "AlertHistoryTableName": "AlertHistory",
    "PriceMonitorTableName": "PriceMonitor",
    "EmaMonitorTableName": "EmaMonitor",
    "DataSourceConfigTableName": "DataSourceConfig",
    "EmailConfigTableName": "EmailConfig",
    "PinBarMonitorTableName": "PinBarMonitor",
    "PinBarSignalTableName": "PinBarSignal",
    "AIAnalysisHistoryTableName": "AIAnalysisHistory"
  }
}
```

**用户密钥支持（推荐生产环境）：**
```bash
dotnet user-secrets set "AzureTableStorage:ConnectionString" "your-connection-string"
dotnet user-secrets set "AzureTableStorage:Enabled" "true"
```

#### ✅ 6. 分区键设计优化
**告警历史分区策略：**
- PartitionKey: `Alert_yyyyMMdd` （按日期分区）
- RowKey: `{Guid}` （唯一ID）
- 优点：
  - 查询时间范围高效（只查询相关日期分区）
  - 避免热分区（数据均匀分布）
  - 支持高并发写入

#### ✅ 7. NuGet 包依赖
**已添加包：**
```xml
<PackageReference Include="Azure.Data.Tables" Version="12.9.1" />
```

### 测试验证

#### ✅ 功能测试
- ✅ 连接字符串配置正确性
- ✅ 表自动创建功能
- ✅ CRUD 操作完整性
- ✅ 分页查询准确性
- ✅ 筛选条件正确性
- ✅ 批量操作性能

#### ✅ 集成测试
- ✅ 与现有告警系统集成
- ✅ 存储后备方案切换
- ✅ 配置开关功能
- ✅ 错误处理和日志记录

### 性能和成本

**成本对比：**
| 指标 | Cosmos DB | Azure Table Storage | 节省 |
|------|-----------|---------------------|------|
| 存储成本 | $0.25/GB/月 | $0.045/GB/月 | **82%** |
| 写入操作 | $0.25/百万 RU | $0.05/10万次 | **80%** |
| 读取操作 | $0.25/百万 RU | $0.004/10万次 | **98%** |
| 典型月成本 | $30-50 | **$1-3** | **98%** |

**性能特点：**
- 单表操作延迟：< 10ms
- 支持每秒数千次操作
- 自动扩展，无需预配置吞吐量
- 99.9% SLA 可用性保证

### 部署指南

**1. 创建 Storage Account（Azure Portal）：**
```
性能层级: Standard
复制: LRS (本地冗余存储)
```

**2. 配置连接字符串：**
```bash
# 使用用户密钥（推荐）
cd src/Trading.AlertSystem.Web
dotnet user-secrets set "AzureTableStorage:ConnectionString" "your-connection-string"
dotnet user-secrets set "AzureTableStorage:Enabled" "true"
```

**3. 运行应用：**
```bash
dotnet run --project src/Trading.AlertSystem.Web
```

应用启动时会自动：
- 检测 Azure Table Storage 配置
- 创建所需的表
- 记录使用的存储类型

### 未来扩展

**待实现的 Repository：**
- [ ] `AzureTablePriceMonitorRepository` - 价格监控配置
- [ ] `AzureTableEmaMonitorRepository` - EMA监控配置
- [ ] `AzureTablePinBarMonitorRepository` - Pin Bar监控配置
- [ ] `AzureTableDataSourceConfigRepository` - 数据源配置
- [ ] `AzureTableEmailConfigRepository` - 邮件配置

**性能优化：**
- [ ] 实现二级缓存（Redis）
- [ ] 批量写入优化
- [ ] 分区键策略调优
- [ ] 查询性能监控

**高级功能：**
- [ ] 数据备份和恢复
- [ ] 跨区域复制
- [ ] 数据归档策略
- [ ] 监控和告警集成

### 相关文件

**核心代码：**
- [AzureTableStorageContext.cs](src/Trading.AlertSystem.Data/Infrastructure/AzureTableStorageContext.cs) - 连接管理
- [AzureTableStorageSettings.cs](src/Trading.AlertSystem.Data/Configuration/AzureTableStorageSettings.cs) - 配置模型
- [AzureTableAlertHistoryRepository.cs](src/Trading.AlertSystem.Data/Repositories/AzureTableAlertHistoryRepository.cs) - 告警历史仓储
- [AzureTableStorageConfiguration.cs](src/Trading.AlertSystem.Web/Configuration/AzureTableStorageConfiguration.cs) - 服务配置
- [StorageConfiguration.cs](src/Trading.AlertSystem.Web/Configuration/StorageConfiguration.cs) - 存储后备方案
- [Program.cs](src/Trading.AlertSystem.Web/Program.cs) - 应用启动配置

**文档：**
- [AZURE_TABLE_STORAGE_GUIDE.md](docs/AZURE_TABLE_STORAGE_GUIDE.md) - 完整配置和使用指南
- [USER_SECRETS_SETUP.md](docs/USER_SECRETS_SETUP.md) - 用户密钥配置指南

**配置文件：**
- [appsettings.json](src/Trading.AlertSystem.Web/appsettings.json) - 应用配置
- [Trading.AlertSystem.Data.csproj](src/Trading.AlertSystem.Data/Trading.AlertSystem.Data.csproj) - 项目依赖

### 标签
`enhancement`, `database`, `cost-optimization`, `azure`, `storage`

---

