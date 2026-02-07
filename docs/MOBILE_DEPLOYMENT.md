# 移动端部署指南 - 无 Cosmos DB 模式

## 概述

本系统支持在没有 Cosmos DB 的环境下运行（例如移动设备），此时系统会自动切换到**内存存储模式**。

## ✅ 自动降级机制

### 检测逻辑

系统在启动时会检查 `CosmosDb:ConnectionString` 配置：

```csharp
if (string.IsNullOrEmpty(connectionString))
{
    // 自动切换到内存存储模式
    // 所有数据保存在内存中，应用重启后会丢失
}
```

### 内存存储组件

当没有 Cosmos DB 时，系统自动使用以下内存仓储：

- ✅ `InMemoryPriceMonitorRepository` - 价格监控规则
- ✅ `InMemoryAlertHistoryRepository` - 告警历史
- ✅ `InMemoryEmaMonitorRepository` - EMA 监控配置
- ✅ `InMemoryDataSourceConfigRepository` - 数据源配置
- ✅ `InMemoryEmailConfigRepository` - 邮件配置
- ✅ `InMemoryPinBarMonitorRepository` - PinBar 监控
- ✅ `InMemoryAIAnalysisRepository` - AI 分析历史

## 📱 移动端配置

### 1. appsettings.json 配置

移除或留空 Cosmos DB 配置：

```json
{
  "CosmosDb": {
    "ConnectionString": "",
    "DatabaseName": "TradingSystem"
  }
}
```

或完全删除 `CosmosDb` 配置节。

### 2. 必需的配置

确保以下配置存在：

```json
{
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN",
    "DefaultChatId": "YOUR_CHAT_ID"
  },
  "Oanda": {
    "ApiKey": "YOUR_API_KEY",
    "AccountId": "YOUR_ACCOUNT_ID",
    "Environment": "Practice"
  },
  "PinBarMonitoring": {
    "Enabled": true,
    "Symbols": ["XAUUSD"],
    "TimeFrames": ["M15", "H1"]
  }
}
```

### 3. 启动应用

```bash
cd src/Trading.AlertSystem.Web
dotnet run
```

启动日志会显示：

```
⚠️ Cosmos DB 未配置，使用内存存储模式
使用内存存储 - 数据在应用重启后会丢失
```

## ⚠️ 限制和注意事项

### 内存模式的限制

1. **数据不持久化**
   - 所有配置和历史数据仅保存在内存中
   - 应用重启后数据会丢失

2. **默认配置**
   - 使用预设的默认配置
   - 配置修改不会永久保存

3. **历史记录限制**
   - 告警历史：最多 1000 条
   - PinBar 信号：最多 500 条
   - AI 分析记录：最多 500 条

### 核心功能仍然可用

✅ **完全支持的功能：**
- Telegram 实时通知
- PinBar 信号检测
- EMA 监控
- 价格告警
- AI 信号验证（如果配置了 Azure OpenAI）
- 双级 AI 过滤架构

❌ **不可用的功能：**
- 历史数据查询 API（数据重启后丢失）
- 配置持久化（需手动在 appsettings.json 中配置）
- 跨设备数据同步

## 🚀 移动端优化建议

### 1. 精简监控配置

减少监控品种和时间周期以降低资源消耗：

```json
{
  "PinBarMonitoring": {
    "Enabled": true,
    "Symbols": ["XAUUSD"],        // 只监控1-2个品种
    "TimeFrames": ["M15"]          // 只监控1个时间周期
  }
}
```

### 2. 调整检查间隔

增加检查间隔以节省电量和流量：

```csharp
// 在 DualTierPinBarMonitoringService.cs 中
private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30); // 改为30分钟
```

### 3. 禁用可选功能

```json
{
  "EmaMonitoring": {
    "Enabled": false  // 手机上可以禁用 EMA 监控
  },
  "Email": {
    "Enabled": false  // 禁用邮件通知，只用 Telegram
  }
}
```

## 🔧 故障排查

### 问题：应用启动失败

**检查：**
1. 确认 `CosmosDb:ConnectionString` 为空或不存在
2. 查看启动日志是否显示内存模式启动

### 问题：配置修改不生效

**原因：** 内存模式下配置不持久化

**解决：** 直接修改 `appsettings.json` 并重启应用

### 问题：历史数据丢失

**原因：** 内存存储模式，重启后数据丢失

**解决：** 这是预期行为。如需持久化，请配置 Cosmos DB

## 📊 性能对比

| 指标 | Cosmos DB 模式 | 内存模式 |
|------|---------------|----------|
| 启动时间 | 5-10秒 | 1-2秒 ⚡ |
| 内存占用 | 150-200MB | 50-80MB ⚡ |
| 配置持久化 | ✅ | ❌ |
| 历史查询 | ✅ | ❌ |
| 适用场景 | 生产环境 | 测试/移动 |

## 🎯 推荐使用场景

### ✅ 适合内存模式

- 移动设备测试
- 临时监控任务
- 演示和试用
- 资源受限环境

### ❌ 不适合内存模式

- 生产环境长期运行
- 需要历史数据分析
- 多设备协同工作
- 需要数据备份

## 🔄 迁移到 Cosmos DB

如果将来需要持久化存储：

1. **配置 Cosmos DB**
   ```json
   {
     "CosmosDb": {
       "ConnectionString": "YOUR_CONNECTION_STRING",
       "DatabaseName": "TradingSystem"
     }
   }
   ```

2. **重启应用**
   - 系统会自动检测并切换到 Cosmos DB 模式
   - 当前内存中的数据不会自动迁移

3. **配置初始化**
   - Cosmos DB 会自动创建容器和默认配置

## 📝 相关文档

- [Azure OpenAI 配置](./AZURE_OPENAI_SETUP.md)
- [双级AI架构快速开始](./DUAL_TIER_AI_QUICKSTART.md)
- [用户密钥配置](./USER_SECRETS_SETUP.md)

---

**版本**: 1.0
**更新时间**: 2026-02-07
