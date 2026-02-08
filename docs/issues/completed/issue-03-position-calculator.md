## Issue 2: 实现风险管理和仓位计算系统

### 标题
Risk Management & Position Size Calculator

### 描述
实现交易风险管理系统，根据账户资金和风险参数自动计算最佳开仓头寸。

### 功能需求

**输入参数：**
- 账户资金总额
- 单笔交易最大亏损限额（金额或百分比）
- 单日最大亏损限额（金额或百分比）
- 交易品种（如XAUUSD、XAGUSD等）
- 合约大小（contract size）
- 当前价格
- 计划止损价格
- 已有持仓信息（计算剩余可用风险额度）

**输出结果：**
- 是否允许开仓（布尔值）
- 建议开仓手数（lots）
- 风险金额
- 风险百分比
- 剩余可用风险额度
- 拒绝原因（如果不允许开仓）

**计算逻辑：**
```
止损点数 = |入场价 - 止损价| / 最小变动单位
单手风险 = 止损点数 × 合约大小 × 每点价值
最大允许手数 = min(
    单笔风险限额 / 单手风险,
    (单日限额 - 当日已亏损) / 单手风险
)
```

### 技术实现

**建议目录结构：**
```
src/Trading.RiskManagement/
  ├── Models/
  │   ├── RiskParameters.cs
  │   ├── PositionSizeResult.cs
  │   └── InstrumentSpecification.cs
  ├── Services/
  │   ├── IRiskCalculator.cs
  │   ├── RiskCalculator.cs
  │   └── PositionValidator.cs
  └── Trading.RiskManagement.csproj
```

**集成点：**
- 在 `Trading.AlertSystem.Service` 中集成
- 提供 REST API 供 Web 和移动端调用

### 验收标准
- [ ] 能正确计算不同品种的仓位大小
- [ ] 单笔风险限制生效
- [ ] 单日风险限制生效
- [ ] 考虑已有持仓的影响
- [ ] 有完整的单元测试
- [ ] 有API文档和使用示例

---

## Issue 2: Telegram 双向消息集成

### 标题
Telegram Two-Way Messaging Integration for Trade Confirmation

### 描述
实现与Telegram的双向通信，发送交易信号并等待用户确认回复后执行操作。

### 功能需求

**发送消息功能：**
- 格式化交易信号（品种、方向、入场价、止损、止盈、建议手数等）
- 发送到指定Telegram聊天/频道
- 附带确认按钮（InlineKeyboard）：✅ 确认开单 / ❌ 取消

**接收消息功能：**
- 实现 Telegram Bot Webhook 或 Long Polling
- 监听用户的按钮点击回复
- 关联回复与原始交易信号
- 设置超时机制（如5分钟无回复自动取消）

**消息格式示例：**
```
🔔 交易信号 #12345

📊 品种: XAUUSD
📈 方向: 做多 (BUY)
💵 价格: 2,650.50
🛑 止损: 2,645.00 (-5.5点)
🎯 止盈: 2,665.00 (+14.5点)
📦 建议手数: 0.15 lots
💰 风险: $82.50 (1.0%)

⏰ 有效期: 5分钟
```

### 技术实现

**方案选择：**
- 使用 Telegram Bot API
- Webhook 模式（推荐）或 Long Polling
- 状态管理：Redis 或内存缓存

**建议目录结构：**
```
src/Trading.Telegram/
  ├── Models/
  │   ├── TradeSignalMessage.cs
  │   ├── UserConfirmation.cs
  │   └── TelegramConfig.cs
  ├── Services/
  │   ├── ITelegramService.cs
  │   ├── TelegramBotService.cs
  │   ├── MessageFormatter.cs
  │   └── ConfirmationManager.cs
  └── Trading.Telegram.csproj
```

**配置参数：**
- Bot Token
- Chat ID / Channel ID
- Webhook URL（如使用webhook）
- 确认超时时间

### 验收标准
- [ ] 能成功发送格式化交易信号
- [ ] InlineKeyboard 按钮正常显示
- [ ] 能接收并解析用户点击
- [ ] 超时机制正常工作
- [ ] 消息与确认正确关联
- [ ] 有错误处理和重试机制
- [ ] 有配置文档

---

## Issue 3: Android 交易执行 App

### 标题
Android Trading Executor App for TradeLocker

### 描述
开发Android应用，接收Telegram指令并通过TradeLocker API执行交易操作。

### 功能需求

**核心功能：**
1. **账号管理**
   - 配置 TradeLocker 账号信息（服务器、账号、密码、API密钥）
   - 保存多个账号配置
   - 测试连接状态

2. **命令接收**
   - 监听指定Telegram频道/机器人消息
   - 解析交易指令（开仓、平仓、修改订单等）
   - 显示待执行命令队列

3. **交易执行**
   - 解析指令参数（品种、手数、止损、止盈）
   - 调用 TradeLocker API 下单
   - 显示执行结果和错误信息

4. **持仓管理**
   - 显示当前持仓列表
   - 显示每笔订单的详情（开仓价、盈亏、止损止盈）
   - 手动平仓功能

5. **历史记录**
   - 显示过往交易记录
   - 按日期、品种筛选
   - 统计盈亏

6. **控制选项**
   - 开启/暂停接收命令
   - 仅通知模式（不自动执行）
   - 需要确认模式（手动确认每笔交易）

### UI界面设计

**主要页面：**
1. 首页 - 账号状态、持仓概览、命令开关
2. 设置页 - TradeLocker账号配置、Telegram配置
3. 持仓页 - 当前持仓列表
4. 历史页 - 交易记录
5. 日志页 - 操作日志和错误信息

### 技术栈

**推荐方案：**
- 语言：Kotlin
- UI：Jetpack Compose 或 XML
- 网络：Retrofit + OkHttp
- 数据库：Room
- 后台服务：WorkManager + Foreground Service
- Telegram：Telegram Bot API 或 TDLib

**项目结构：**
```
TradingExecutor/
  ├── app/
  │   ├── src/main/
  │   │   ├── java/com/trading/executor/
  │   │   │   ├── ui/
  │   │   │   │   ├── MainActivity.kt
  │   │   │   │   ├── SettingsActivity.kt
  │   │   │   │   └── ...
  │   │   │   ├── data/
  │   │   │   │   ├── db/
  │   │   │   │   ├── models/
  │   │   │   │   └── repositories/
  │   │   │   ├── services/
  │   │   │   │   ├── TradeLockerService.kt
  │   │   │   │   ├── TelegramService.kt
  │   │   │   │   └── CommandExecutorService.kt
  │   │   │   └── utils/
  │   │   └── res/
  │   └── build.gradle
  └── README.md
```

### TradeLocker API 集成

需要实现的主要接口：
- 登录/认证
- 获取账户信息
- 获取持仓列表
- 下市价单/限价单
- 修改止损止盈
- 平仓

### 安全考虑
- 本地加密存储账号密码
- 使用 Android Keystore
- HTTPS 连接
- 防止误操作的二次确认

### 验收标准
- [ ] 能成功配置 TradeLocker 账号
- [ ] 能接收 Telegram 消息
- [ ] 能解析交易指令
- [ ] 能通过 TradeLocker API 下单
- [ ] 持仓显示正确
- [ ] 历史记录保存完整
- [ ] 有启动/停止控制
- [ ] 有完整的错误处理
- [ ] 界面友好易用
- [ ] 有用户手册

---

---

