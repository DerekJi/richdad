# Trading Alert System (交易告警系统)

实时监控TradeLocker交易品种价格，当价格达到指定条件时通过Telegram发送通知。

## 功能特性

- ✅ **实时价格监控** - 定时从TradeLocker获取实时价格数据
- ✅ **多种告警类型** - 支持固定价格、EMA、MA等多种告警条件
- ✅ **Telegram通知** - 价格触发时即时发送Telegram消息
- ✅ **Web管理界面** - 直观的Web界面管理告警规则
- ✅ **灵活配置** - 可自定义监控间隔、消息模板等

## 项目结构

```
src/
├── Trading.AlertSystem.Data/          # 数据层
│   ├── Configuration/                 # 配置类
│   ├── Models/                        # 数据模型
│   └── Services/                      # 数据服务（TradeLocker、Telegram）
├── Trading.AlertSystem.Service/       # 业务层
│   ├── Configuration/                 # 服务配置
│   ├── Repositories/                  # 数据仓储
│   └── Services/                      # 业务服务（价格监控）
└── Trading.AlertSystem.Web/           # 应用层
    ├── Controllers/                   # API控制器
    └── wwwroot/                       # Web前端
```

## 快速开始

### 1. 配置说明

编辑 `src/Trading.AlertSystem.Web/appsettings.json`：

```json
{
  "TradeLocker": {
    "ApiBaseUrl": "https://api.tradelocker.com",
    "AccessToken": "",           // 方式1: 直接使用AccessToken
    "Username": "",              // 方式2: 或使用用户名/密码登录
    "Password": "",
    "Server": "",
    "AccountId": null
  },
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN",      // 从 @BotFather 获取
    "DefaultChatId": YOUR_CHAT_ID,     // 你的Telegram Chat ID
    "Enabled": true
  },
  "Monitoring": {
    "IntervalSeconds": 60,             // 监控间隔（秒）
    "Enabled": true,
    "RunOnStartup": true,
    "MaxConcurrency": 10
  },
  "CosmosDb": {
    "EndpointUrl": "YOUR_COSMOS_ENDPOINT",
    "PrimaryKey": "YOUR_COSMOS_KEY",
    "DatabaseName": "TradingSystem"
  }
}
```

### 2. 获取Telegram Bot Token

1. 在Telegram中搜索 `@BotFather`
2. 发送 `/newbot` 命令创建新机器人
3. 按提示设置机器人名称
4. 获取Bot Token并填入配置

### 3. 获取Telegram Chat ID

1. 在Telegram中搜索 `@userinfobot`
2. 启动对话，机器人会显示你的Chat ID
3. 将Chat ID填入配置

### 4. 运行应用

```bash
cd src/Trading.AlertSystem.Web
dotnet run
```

访问: `http://localhost:5000` (或配置的端口)

## 使用指南

### 创建告警

1. 打开Web界面
2. 点击"创建新告警"按钮
3. 填写告警信息：
   - **告警名称**: 便于识别的名称
   - **交易品种**: 如 XAUUSD、XAGUSD等
   - **告警类型**: 
     - 固定价格: 监控价格是否达到指定值
     - EMA: 监控价格是否突破EMA线
     - MA: 监控价格是否突破MA线
   - **价格方向**: 上穿或下穿
   - **时间周期**: 用于计算技术指标的K线周期
   - **消息模板**: 自定义通知消息（可选）

### 告警类型说明

#### 1. 固定价格告警
监控价格是否达到指定值。
```
示例: XAUUSD价格上穿2000.00
```

#### 2. EMA告警
监控价格是否突破指数移动平均线。
```
示例: XAUUSD价格上穿EMA(20)
```

#### 3. MA告警
监控价格是否突破简单移动平均线。
```
示例: XAUUSD价格下穿MA(50)
```

### 消息模板变量

可在消息模板中使用以下变量：
- `{Symbol}` - 交易品种
- `{Name}` - 告警名称
- `{Price}` - 当前价格
- `{Target}` - 目标值描述
- `{Direction}` - 方向（上穿/下穿）
- `{Time}` - 触发时间

示例模板:
```
⚠️ 告警触发！
品种: {Symbol}
价格 {Price} 已{Direction} {Target}
时间: {Time}
```

### 管理告警

- **编辑**: 修改告警配置
- **删除**: 移除告警
- **重置**: 重置已触发的告警，使其可以再次触发
- **禁用/启用**: 临时关闭或开启告警

## API接口

### 告警管理

- `GET /api/alerts` - 获取所有告警
- `GET /api/alerts/enabled` - 获取启用的告警
- `GET /api/alerts/{id}` - 获取指定告警
- `POST /api/alerts` - 创建新告警
- `PUT /api/alerts/{id}` - 更新告警
- `DELETE /api/alerts/{id}` - 删除告警
- `POST /api/alerts/{id}/reset` - 重置告警

### 系统管理

- `GET /api/system/health` - 健康检查
- `POST /api/system/test-telegram` - 测试Telegram连接
- `POST /api/system/test-tradelocker` - 测试TradeLocker连接
- `POST /api/system/check-now` - 立即执行监控检查
- `GET /api/system/price/{symbol}` - 获取实时价格

## 技术栈

- **后端**: ASP.NET Core 9.0
- **数据库**: Azure Cosmos DB
- **技术指标**: Skender.Stock.Indicators
- **Telegram**: Telegram.Bot
- **前端**: 原生HTML/CSS/JavaScript

## 注意事项

1. **TradeLocker API**: 请确保正确配置TradeLocker API访问凭证
2. **告警触发**: 告警触发后会自动标记为"已触发"，需要手动重置才能再次触发
3. **监控间隔**: 建议间隔不要太短，避免频繁请求API
4. **并发限制**: 通过MaxConcurrency配置控制并发检查数量

## 故障排除

### Telegram消息发送失败
- 检查Bot Token是否正确
- 确认Chat ID是否正确
- 确保机器人已启动（向机器人发送 `/start`）

### TradeLocker连接失败
- 检查API凭证是否正确
- 确认网络连接正常
- 查看日志获取详细错误信息

### 告警不触发
- 检查告警是否启用
- 确认告警未处于"已触发"状态
- 验证价格数据是否正常获取
- 查看应用日志

## 开发计划

- [ ] 支持更多技术指标（RSI、MACD等）
- [ ] 支持多个Telegram接收者
- [ ] 添加告警历史记录
- [ ] 支持邮件通知
- [ ] 添加价格图表展示
- [ ] 支持自定义脚本条件

## 许可证

与主项目保持一致
