# Trading Alert System - 快速启动指南

## 🚀 快速开始

### 1. 准备工作

#### 获取Telegram Bot Token
1. 在Telegram搜索 `@BotFather`
2. 发送 `/newbot` 创建机器人
3. 设置机器人名称
4. 复制获得的Token

#### 获取Telegram Chat ID
1. 在Telegram搜索 `@userinfobot`
2. 启动对话
3. 复制显示的Chat ID

#### 配置TradeLocker
- 准备你的TradeLocker账户信息
- 可以使用AccessToken或用户名/密码

### 2. 配置应用

**推荐使用 User Secrets 存储敏感信息：**

```bash
cd src/Trading.AlertSystem.Web
dotnet user-secrets init
dotnet user-secrets set "TradeLocker:Environment" "demo"
dotnet user-secrets set "TradeLocker:Email" "你的TradeLocker邮箱"
dotnet user-secrets set "TradeLocker:Password" "你的密码"
dotnet user-secrets set "TradeLocker:Server" "你的服务器名称"
dotnet user-secrets set "TradeLocker:AccountId" "123456"
dotnet user-secrets set "TradeLocker:AccountNumber" "1"
dotnet user-secrets set "Telegram:BotToken" "你的Bot Token"
dotnet user-secrets set "Telegram:DefaultChatId" "你的Chat ID"
dotnet user-secrets set "CosmosDb:ConnectionString" "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5..."
```

**或者编辑 `appsettings.json`（不推荐，仅用于开发测试）:**

```json
{
  "Monitoring": {
    "IntervalSeconds": 60,
    "Enabled": true,
    "RunOnStartup": true,
    "MaxConcurrency": 10
  },
  "CosmosDb": {
    "ConnectionString": "",              // 使用 User Secrets 配置
    "DatabaseName": "TradingSystem",
    "AlertContainerName": "PriceAlerts"
  }
}
```

**注意：** TradeLocker和Telegram的配置必须通过User Secrets配置，不要直接写在appsettings.json中！

**获取TradeLocker信息：**
- Environment: demo（测试环境）或 live（实盘环境）
- Email: 你的TradeLocker账户邮箱
- Password: 你的TradeLocker密码
- Server: 登录TradeLocker时选择的服务器名称
- AccountId: 在TradeLocker平台点击账户切换器（圆形图标），找到#后面的数字

**CosmosDB配置（可选）：**
- 本地开发可使用 Cosmos DB Emulator
- ConnectionString示例：`AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==`
- 不配置CosmosDB会自动使用内存存储（重启后数据丢失）

### 3. 运行应用

```bash
cd src/Trading.AlertSystem.Web
dotnet run
```

### 4. 访问Web界面

打开浏览器访问: `http://localhost:5000`

### 5. 创建第一个告警

1. 点击"创建新告警"
2. 填写信息：
   - **名称**: 金价突破2000
   - **品种**: XAUUSD
   - **类型**: 固定价格
   - **目标**: 2000.00
   - **方向**: 上穿
3. 点击保存

### 6. 测试

- 点击"测试Telegram"确认消息可以发送
- 点击"测试TradeLocker"确认连接正常
- 点击"立即检查"手动触发一次监控

## 📊 告警示例

### 示例1: 价格突破告警
```
名称: XAUUSD突破2000
品种: XAUUSD
类型: 固定价格
目标: 2000.00
方向: 上穿
```

### 示例2: EMA突破告警
```
名称: XAUUSD突破EMA20
品种: XAUUSD
类型: EMA
EMA周期: 20
方向: 上穿
时间周期: M15
```

### 示例3: MA跌破告警
```
名称: XAGUSD跌破MA50
品种: XAGUSD
类型: MA
MA周期: 50
方向: 下穿
时间周期: H1
```

## 🔧 常见问题

### Q: Telegram消息发送失败？
A:
- 确认Bot Token正确
- 确认已向机器人发送过 `/start` 命令
- 检查Chat ID是否正确

### Q: TradeLocker连接失败？
A:
- 检查API凭证是否正确
- 确认账户ID正确
- 查看应用日志获取详细错误

### Q: 告警不触发？
A:
- 确认告警已启用
- 检查告警未处于"已触发"状态
- 点击"立即检查"测试
- 查看日志确认价格获取正常

### Q: 如何重置已触发的告警？
A: 在Web界面中点击告警卡片的"重置"按钮

## 📝 注意事项

1. **告警触发机制**: 告警触发后会自动标记为"已触发"，需要手动重置
2. **监控间隔**: 建议设置在30-300秒之间
3. **API限制**: 注意TradeLocker的API调用限制
4. **数据存储**: 告警配置存储在CosmosDB中

## 🎯 最佳实践

1. **合理设置监控间隔**: 避免过于频繁的API调用
2. **使用描述性名称**: 便于识别告警用途
3. **自定义消息模板**: 包含关键信息便于决策
4. **定期检查日志**: 及时发现和解决问题
5. **测试后再启用**: 创建告警后先测试再启用

## 🔗 相关资源

- [完整文档](README.md)
- [TradeLocker API文档](https://tradelocker.com/api)
- [Telegram Bot API文档](https://core.telegram.org/bots/api)

## 💡 提示

- 支持同时监控多个品种
- 可以为不同告警设置不同的Chat ID
- 消息模板支持自定义变量
- 可以临时禁用告警而不删除配置
