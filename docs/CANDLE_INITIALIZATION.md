# 数据初始化指南

## 概述

本指南介绍如何初始化和维护市场数据缓存系统的历史数据，确保系统具有足够的数据用于回测、分析和实时交易决策。

## 初始化策略

### 推荐数据量

根据时间周期确定初始化的K线数量：

| 时间周期 | 推荐数量 | 覆盖时间 | 存储大小 |
|----------|----------|----------|----------|
| M1 | 1,000 | ~16小时 | ~50KB |
| M5 | 2,000 | ~1周 | ~100KB |
| M15 | 2,000 | ~3周 | ~100KB |
| M30 | 1,500 | ~1个月 | ~75KB |
| H1 | 1,000 | ~6周 | ~50KB |
| H4 | 500 | ~3个月 | ~25KB |
| D1 | 200 | ~200交易日 | ~10KB |

**总计（5品种 × 7周期）**: 约 15MB，月成本 < $1

## 初始化方法

### 方法1：通过API初始化

#### 初始化所有配置的品种

```bash
curl -X POST https://your-api/api/marketdata/initialize
```

这将初始化 `appsettings.json` 中配置的所有品种和时间周期。

#### 初始化特定品种和周期

```bash
# 初始化 XAUUSD 的 M5 和 H1 数据
curl -X POST "https://your-api/api/marketdata/initialize?symbols=XAUUSD&timeFrames=M5,H1"

# 初始化多个品种
curl -X POST "https://your-api/api/marketdata/initialize?symbols=XAUUSD,XAGUSD,EURUSD&timeFrames=M5,M15,H1"
```

### 方法2：通过代码初始化

```csharp
public class DataInitializer
{
    private readonly DataInitializationService _initService;
    private readonly ILogger<DataInitializer> _logger;

    public async Task InitializeAllDataAsync()
    {
        try
        {
            _logger.LogInformation("开始初始化历史数据...");

            // 初始化所有配置的品种
            await _initService.InitializeHistoricalDataAsync();

            _logger.LogInformation("历史数据初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化失败");
            throw;
        }
    }

    public async Task InitializeSpecificDataAsync()
    {
        // 只初始化特定品种和周期
        var symbols = new List<string> { "XAUUSD", "XAGUSD" };
        var timeFrames = new List<string> { "M5", "M15", "H1" };

        await _initService.InitializeHistoricalDataAsync(symbols, timeFrames);
    }
}
```

### 方法3：启动时自动初始化

在 `Program.cs` 中添加启动初始化：

```csharp
var app = builder.Build();

// 初始化存储
await app.InitializeStorageAsync();

// 初始化市场数据（可选，首次运行时）
if (args.Contains("--init-data"))
{
    using var scope = app.Services.CreateScope();
    var initService = scope.ServiceProvider.GetRequiredService<DataInitializationService>();
    await initService.InitializeHistoricalDataAsync();
}

app.Run();
```

运行时：
```bash
dotnet run --init-data
```

## 数据更新策略

### 增量更新

增量更新只获取最新的数据，避免重复下载已有数据。

#### 更新所有品种

```bash
curl -X POST https://your-api/api/marketdata/update
```

#### 更新特定品种

```bash
curl -X POST "https://your-api/api/marketdata/update?symbol=XAUUSD&timeFrame=M5"
```

#### 定时增量更新

使用 cron 或 Windows Task Scheduler 定时运行：

```bash
# 每小时更新一次
0 * * * * curl -X POST https://your-api/api/marketdata/update
```

### 自动刷新

启用自动刷新后，系统会定期补充最新数据：

```json
{
  "MarketDataCache": {
    "AutoRefreshEnabled": true,
    "RefreshIntervalMinutes": 5
  }
}
```

## 初始化流程

### 完整初始化流程图

```
┌─────────────────────────┐
│ 1. 配置检查             │
│   - Azure连接字符串      │
│   - 品种和周期列表       │
└────────┬────────────────┘
         │
         ▼
┌─────────────────────────┐
│ 2. 检查已有数据         │
│   - 查询最新时间         │
│   - 判断是否需要初始化   │
└────────┬────────────────┘
         │
         ▼
┌─────────────────────────┐
│ 3. 从OANDA获取数据      │
│   - 按品种和周期获取     │
│   - 避免API速率限制      │
└────────┬────────────────┘
         │
         ▼
┌─────────────────────────┐
│ 4. 保存到Azure Table    │
│   - 批量写入（100条/批） │
│   - 记录日志             │
└────────┬────────────────┘
         │
         ▼
┌─────────────────────────┐
│ 5. 验证数据完整性       │
│   - 检查数据量           │
│   - 生成报告             │
└─────────────────────────┘
```

### 示例：首次部署初始化

```bash
#!/bin/bash

echo "开始初始化市场数据..."

# 1. 配置检查
echo "检查配置..."
curl https://your-api/api/marketdata/stats

# 2. 初始化核心品种
echo "初始化 XAUUSD..."
curl -X POST "https://your-api/api/marketdata/initialize?symbols=XAUUSD&timeFrames=M5,M15,H1,H4,D1"

sleep 30

echo "初始化 XAGUSD..."
curl -X POST "https://your-api/api/marketdata/initialize?symbols=XAGUSD&timeFrames=M5,M15,H1,H4"

sleep 30

echo "初始化外汇对..."
curl -X POST "https://your-api/api/marketdata/initialize?symbols=EURUSD,USDJPY,AUDUSD&timeFrames=M5,H1,D1"

# 3. 验证数据
echo "验证数据完整性..."
curl https://your-api/api/marketdata/integrity > integrity_report.json

# 4. 查看统计
echo "查看统计信息..."
curl https://your-api/api/marketdata/stats

echo "初始化完成！"
```

## 数据维护

### 每日维护脚本

```bash
#!/bin/bash
# daily_update.sh

echo "$(date): 开始每日数据更新"

# 增量更新所有数据
curl -X POST https://your-api/api/marketdata/update

# 检查完整性
curl https://your-api/api/marketdata/integrity > "integrity_$(date +%Y%m%d).json"

echo "$(date): 每日更新完成"
```

### 每周维护任务

```bash
#!/bin/bash
# weekly_maintenance.sh

echo "$(date): 开始每周维护"

# 1. 完整性检查
curl https://your-api/api/marketdata/integrity > weekly_integrity.json

# 2. 重新初始化有问题的品种
# 根据 weekly_integrity.json 的结果手动处理

# 3. 清理统计
curl https://your-api/api/marketdata/stats > weekly_stats.json

echo "$(date): 每周维护完成"
```

## 数据完整性检查

### 检查数据完整性

```bash
curl https://your-api/api/marketdata/integrity
```

### 完整性报告解读

```json
{
  "XAUUSD_M5": {
    "earliestTime": "2026-01-01T00:00:00Z",
    "latestTime": "2026-02-09T10:15:00Z",
    "expectedCount": 11520,
    "actualCount": 11450,
    "completeness": "99.39%"
  },
  "issues": [
    "EURUSD M15: 数据完整性 85.23%（低于90%）"
  ]
}
```

**处理建议：**
- **完整性 > 95%**：正常，无需处理
- **90-95%**：可接受，建议增量更新
- **< 90%**：需要重新初始化

### 修复数据缺失

```bash
# 对于完整性低的品种，重新初始化
curl -X POST "https://your-api/api/marketdata/initialize?symbols=EURUSD&timeFrames=M15"
```

## 性能优化

### 批量初始化优化

```csharp
public async Task BatchInitializeAsync()
{
    var tasks = new List<Task>();

    // 并行初始化不同品种（注意API速率限制）
    foreach (var symbol in symbols)
    {
        tasks.Add(Task.Run(async () =>
        {
            await _initService.InitializeHistoricalDataAsync(
                new List<string> { symbol },
                timeFrames
            );
            await Task.Delay(1000); // 避免速率限制
        }));
    }

    await Task.WhenAll(tasks);
}
```

### API速率限制处理

OANDA API 限制：
- **Practice环境**: 120 请求/秒
- **Live环境**: 100 请求/秒

**建议策略：**
- 在请求之间添加延迟（500-1000ms）
- 使用批量请求而非逐个请求
- 监控API响应，检测速率限制错误

## 故障处理

### 问题1：初始化失败

**症状：**
```
初始化 XAUUSD M5 失败: API connection timeout
```

**解决方案：**
1. 检查网络连接
2. 验证 OANDA API 密钥
3. 减少并发请求数
4. 重试失败的品种：
   ```bash
   curl -X POST "https://your-api/api/marketdata/initialize?symbols=XAUUSD&timeFrames=M5"
   ```

### 问题2：数据不完整

**症状：**
完整性报告显示某些时间段缺失数据

**解决方案：**
```bash
# 1. 手动刷新缺失时间段
curl -X POST "https://your-api/api/marketdata/refresh?symbol=XAUUSD&timeFrame=M5&startTime=2026-01-15&endTime=2026-01-20"

# 2. 或重新初始化
curl -X POST "https://your-api/api/marketdata/initialize?symbols=XAUUSD&timeFrames=M5"
```

### 问题3：Azure Table Storage 连接失败

**症状：**
```
Azure Table Storage connection error
```

**检查：**
1. 验证连接字符串
2. 检查 Azure 账户状态
3. 确认防火墙规则
4. 测试连接：
   ```bash
   curl https://your-api/api/marketdata/stats
   ```

## 监控与告警

### 关键指标监控

创建监控脚本 `monitor_data.sh`：

```bash
#!/bin/bash

# 1. 检查最新数据时间
latest=$(curl -s "https://your-api/api/marketdata/latest?symbol=XAUUSD&timeFrame=M5" | jq -r '.latestTime')
echo "XAUUSD M5 最新时间: $latest"

# 2. 检查数据完整性
integrity=$(curl -s https://your-api/api/marketdata/integrity | jq -r '.totalIssues')
echo "数据完整性问题数: $integrity"

# 3. 告警（如果有问题）
if [ "$integrity" -gt 0 ]; then
    echo "警告：发现数据完整性问题！"
    # 发送告警邮件或通知
fi
```

### 日志分析

```bash
# 查看初始化日志
grep "DataInitializationService" logs/*.log | grep "ERROR"

# 统计各品种数据量
grep "成功初始化" logs/*.log | awk '{print $6, $7}' | sort | uniq -c
```

## 最佳实践

### 1. 分阶段初始化

**阶段1：核心品种**（优先级最高）
```bash
curl -X POST "https://your-api/api/marketdata/initialize?symbols=XAUUSD,XAGUSD&timeFrames=M5,H1"
```

**阶段2：外汇主要品种**
```bash
curl -X POST "https://your-api/api/marketdata/initialize?symbols=EURUSD,USDJPY,AUDUSD&timeFrames=M5,H1"
```

**阶段3：其他周期**
```bash
curl -X POST "https://your-api/api/marketdata/initialize?symbols=XAUUSD,XAGUSD,EURUSD&timeFrames=M15,H4,D1"
```

### 2. 定期数据审计

每月执行：
```bash
# 生成完整性报告
curl https://your-api/api/marketdata/integrity > monthly_audit_$(date +%Y%m).json

# 分析存储使用情况
curl https://your-api/api/marketdata/stats > monthly_stats_$(date +%Y%m).json
```

### 3. 备份策略

虽然 Azure Table Storage 有内置冗余，建议定期导出关键数据：

```bash
# 导出 XAUUSD M5 数据
curl "https://your-api/api/marketdata/candles?symbol=XAUUSD&timeFrame=M5&count=5000" > backup_xauusd_m5_$(date +%Y%m%d).json
```

## 成本估算

### 初始化成本

| 品种数 | 周期数 | K线数/周期 | 总K线数 | 存储 | 操作成本 | 总成本 |
|--------|--------|------------|---------|------|----------|--------|
| 5 | 5 | 1000 | 25,000 | 1.25MB | $0.01 | $0.01 |
| 10 | 7 | 1500 | 105,000 | 5.25MB | $0.04 | $0.04 |
| 20 | 7 | 2000 | 280,000 | 14MB | $0.11 | $0.12 |

### 月度维护成本

- **存储**: $0.045/GB/月
- **操作**: $0.0004/万次
- **预计**: 10GB 数据 + 100万次/月 = **$0.85/月**

## 下一步

- 参考 [市场数据缓存使用指南](MARKET_DATA_CACHE_GUIDE.md) 了解日常使用
- 查看 API 文档了解所有可用端点
- 设置自动化维护脚本

## 支持

遇到问题？
- 查看系统日志
- 运行完整性检查
- 联系技术支持
