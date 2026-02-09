# K线数据持久化系统 - 快速验证指南

## ✅ 静态验证（已完成）

运行验证脚本：
```bash
bash scripts/verify-candle-persistence.sh
```

验证结果：
- ✓ 所有核心文件已创建（10个文件）
- ✓ 配置文件正确
- ✓ 编译成功，无错误无警告
- ✓ 文档齐全

## 🚀 动态验证（需要运行应用）

### 方式1：使用本地模拟器（Azurite）

1. **安装 Azurite**（如果还没有安装）：
   ```bash
   npm install -g azurite
   ```

2. **启动 Azurite**：
   ```bash
   azurite --silent --location ./azurite --debug ./azurite/debug.log &
   ```

3. **配置连接字符串**（`src/Trading.Web/appsettings.json`）：
   ```json
   "AzureTableStorage": {
     "ConnectionString": "UseDevelopmentStorage=true",
     "Enabled": true
   }
   ```

4. **启动应用**：
   ```bash
   cd src/Trading.Web
   dotnet run
   ```

5. **测试 API**（在新终端）：
   ```bash
   # 1. 初始化30天历史数据
   curl -X POST "http://localhost:5086/api/candle/initialize" \
        -H "Content-Type: application/json" \
        -d '{"symbol":"XAUUSD","timeFrame":"M5","days":30}'

   # 2. 获取K线数据（应该从缓存读取）
   curl -X GET "http://localhost:5086/api/candle/candles?symbol=XAUUSD&timeFrame=M5&count=100" | jq

   # 3. 查看统计信息
   curl -X GET "http://localhost:5086/api/candle/stats?symbol=XAUUSD&timeFrame=M5" | jq

   # 4. 刷新缓存
   curl -X POST "http://localhost:5086/api/candle/refresh?symbol=XAUUSD&timeFrame=M5"
   ```

### 方式2：使用 Azure Storage（生产环境）

1. **获取 Azure Storage 连接字符串**：
   - 登录 Azure Portal
   - 找到你的 Storage Account
   - 复制连接字符串

2. **配置连接字符串**（`src/Trading.Web/appsettings.json`）：
   ```json
   "AzureTableStorage": {
     "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net",
     "Enabled": true
   }
   ```

3. **启动应用并测试**（同方式1的步骤4-5）

### 验证检查点

#### ✅ API 响应正常
- [ ] `/api/candle/initialize` 返回成功消息
- [ ] `/api/candle/candles` 返回K线数据数组
- [ ] `/api/candle/stats` 返回统计信息（总数、时间范围等）
- [ ] `/api/candle/refresh` 返回成功消息

#### ✅ 智能缓存工作
- [ ] 第一次请求：从 OANDA 获取数据并存储
- [ ] 第二次请求：从数据库读取（速度更快）
- [ ] 日志显示 "从数据库获取到 XX 条数据"

#### ✅ 数据正确存储
使用 Azure Storage Explorer 检查：
- [ ] `Candles` 表存在
- [ ] `CandleIndicators` 表存在
- [ ] 表中有数据记录
- [ ] PartitionKey = Symbol (如 "XAUUSD")
- [ ] RowKey = TimeFrame_DateTime (如 "M5_20260209_1030")

## 📊 性能验证

### 对比测试
```bash
# 1. 清空缓存（删除表数据）
# 2. 测试首次请求（无缓存）
time curl -X GET "http://localhost:5086/api/candle/candles?symbol=XAUUSD&timeFrame=M5&count=100"
# 预期：2-5秒（从 OANDA 获取）

# 3. 测试第二次请求（有缓存）
time curl -X GET "http://localhost:5086/api/candle/candles?symbol=XAUUSD&timeFrame=M5&count=100"
# 预期：< 0.5秒（从数据库读取）
```

### 预期性能指标
- **首次请求（无缓存）**：2-5秒（OANDA API 调用）
- **缓存命中**：< 500ms（Azure Table Storage）
- **本地模拟器**：< 100ms（Azurite）
- **智能补缺**：仅请求缺失的数据段

## 🔍 日志观察

启动应用后，观察日志输出：
```
[CandleCacheService] 查询数据库：XAUUSD M5，从 2026-01-10 到 2026-02-09
[CandleCacheService] 从数据库获取到 8640 条数据
[CandleCacheService] 检测到缺失范围：0 个
[CandleCacheService] 返回 100 条 K 线数据
```

或者：
```
[CandleCacheService] 查询数据库：XAUUSD M5，从 2026-01-10 到 2026-02-09
[CandleCacheService] 从数据库获取到 0 条数据
[CandleCacheService] 检测到缺失范围：1 个
[CandleCacheService] 补充数据：从 OANDA 获取 8640 条
[CandleCacheService] 批量保存：8640 条数据，分 87 批
```

## ⚠️ 常见问题

### Q: ConnectionString 为空，应用能启动吗？
A: 可以，但需要 Azurite 运行。或者在 `appsettings.json` 设置 `Enabled: false` 禁用存储。

### Q: 如何验证数据真的存储了？
A: 使用 Azure Storage Explorer 连接到 Azurite 或 Azure，查看表内容。

### Q: 如何清空测试数据？
A: 在 Azure Storage Explorer 中删除表，或者删除 `./azurite` 目录。

### Q: API 返回 500 错误？
A: 检查：
1. Azurite 是否运行？
2. ConnectionString 是否正确？
3. OANDA API Key 是否配置？

## 📚 相关文档

- [CANDLE_CACHE_GUIDE.md](../docs/CANDLE_CACHE_GUIDE.md) - 详细使用指南
- [CANDLE_INITIALIZATION.md](../docs/CANDLE_INITIALIZATION.md) - 数据初始化指南
- [issue-06-data-persistence.md](../docs/issues/planned/issue-06-data-persistence.md) - 实现文档

## 🎯 验证结论

完成以上验证后，可以确认：
- ✅ Issue 6 功能完整实现
- ✅ 智能缓存正常工作
- ✅ 数据正确持久化
- ✅ API 端点可用
- ✅ 性能达到预期
