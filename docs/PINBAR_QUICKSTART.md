# PinBar策略监控快速入门

## 功能概述

PinBar策略监控系统会自动检测市场中的PinBar形态信号，当满足策略条件时通过Telegram发送交易提醒。

## 已实现的功能

### 1. 后端服务
- ✅ **数据模型**: `PinBarMonitoringConfig`、`PinBarSignalHistory`
- ✅ **数据持久化**: CosmosDB存储配置和信号历史
- ✅ **Repository层**: `IPinBarMonitorRepository`、`PinBarMonitorRepository`
- ✅ **监控服务**: `PinBarMonitoringService` - 后台定时检测信号
- ✅ **API接口**: `PinBarMonitorController` - 配置管理和信号查询

### 2. 前端界面
- ✅ **配置页面**: `pinbar-config.html` - 完整的策略参数配置界面
- ✅ **三个标签页**:
  - 基础配置（品种、周期）
  - 策略参数（EMA、PinBar条件、风险管理）
  - 历史信号（实时查看已发送的信号）

## 快速启动

### 1. 启动Web服务

```bash
cd /d/source/richdad/src/Trading.Web
dotnet run
```

服务将在 `http://localhost:5000` 启动。

### 2. 访问配置页面

打开浏览器访问：`http://localhost:5000/pinbar-config.html`

### 3. 配置监控参数

#### 基础配置
- **交易品种**: 例如 `XAUUSD,XAGUSD`
- **K线周期**: 例如 `M5,M15,H1`
- **历史数据倍数**: 默认 `3`（用于计算指标）

#### 策略参数

**EMA设置**:
- 基准EMA周期: `200`
- 检查EMA列表: `20,50,100`
- 靠近EMA阈值: `0.001`

**PinBar判断条件**:
- 最小下影线/ATR比率: `1.2`
- 最大实体百分比: `35`
- 最小长影线百分比: `50`
- 最大短影线百分比: `25`
- ☑ 要求PinBar方向匹配

**风险管理**:
- 最小ADX值: `0`（0表示不限制）
- 低ADX盈亏比: `0`（0表示低ADX不开仓）
- 标准盈亏比: `2.0`
- 止损策略: `PinBar端点+ATR`
- 止损ATR倍数: `0.3`

**交易时间限制**:
- ☑ 不限制交易时间
- 开始交易时间: `0` (UTC小时)
- 结束交易时间: `23` (UTC小时)

### 4. 启用监控

点击右上角的 **"启用监控"** 按钮，状态变为绿色表示监控已激活。

## API接口

### 获取配置
```http
GET /api/PinBarMonitor/config
```

### 更新配置
```http
POST /api/PinBarMonitor/config
Content-Type: application/json

{
  "enabled": true,
  "symbols": ["XAUUSD", "XAGUSD"],
  "timeFrames": ["M5", "M15"],
  ...
}
```

### 启用/禁用监控
```http
POST /api/PinBarMonitor/toggle
Content-Type: application/json

{
  "enabled": true
}
```

### 获取最近信号
```http
GET /api/PinBarMonitor/signals?count=50
```

### 获取指定品种信号
```http
GET /api/PinBarMonitor/signals/XAUUSD?count=50
```

## 信号格式

当检测到PinBar信号时，Telegram将收到如下消息：

```
🟢 **PinBar 做多信号**

**品种**: XAUUSD
**周期**: M15
**信号时间**: 2026-02-06 08:30:00 UTC

📊 **交易参数**:
• 入场价: 2650.50
• 止损价: 2645.20
• 止盈价: 2661.10
• 盈亏比: 2.00
• ADX: 0.00

📍 **PinBar K线**:
• 时间: 2026-02-06 08:15
• 开盘: 2648.00
• 最高: 2651.50
• 最低: 2644.00
• 收盘: 2650.20

⚠️ 请结合实际市场情况进行判断！
```

## 工作原理

1. **定时检测**: 每分钟检查一次配置的品种和周期
2. **获取数据**: 从市场数据源（Oanda/TradeLocker）获取历史K线
3. **信号判断**: 使用PinBarStrategy判断是否满足开仓条件
   - 检查是否是有效的PinBar形态
   - 验证价格与EMA的关系
   - 确认突破信号
   - 检查交易时间
4. **去重处理**: 同一K线的信号只发送一次
5. **发送通知**: 通过Telegram发送信号详情
6. **记录历史**: 保存到CosmosDB供查询

## 数据库结构

### PinBarMonitorConfig容器
- **分区键**: `/id`（固定为"default"）
- **用途**: 存储监控配置

### PinBarSignalHistory容器
- **分区键**: `/symbol`
- **用途**: 存储历史信号记录

## 文件清单

### 后端
- `src/Trading.AlertSystem.Data/Models/PinBarMonitoringConfig.cs` - 配置模型
- `src/Trading.AlertSystem.Data/Repositories/IPinBarMonitorRepository.cs` - Repository接口
- `src/Trading.AlertSystem.Data/Repositories/PinBarMonitorRepository.cs` - Repository实现
- `src/Trading.AlertSystem.Service/Services/PinBarMonitoringService.cs` - 监控服务
- `src/Trading.AlertSystem.Web/Controllers/PinBarMonitorController.cs` - API控制器

### 前端
- `src/Trading.AlertSystem.Web/wwwroot/pinbar-config.html` - 配置页面

### 数据库
- `src/Trading.AlertSystem.Data/Infrastructure/CosmosDbContext.cs` - 添加了PinBar容器支持

### 程序入口
- `src/Trading.AlertSystem.Web/Program.cs` - 注册了PinBar相关服务

## 监控日志

查看监控服务的运行日志：

```bash
# 在Trading.AlertSystem.Web运行时查看控制台输出
# 会显示：
# - PinBar监控服务已启动
# - 开始检查PinBar信号
# - ✅ PinBar信号已发送: XAUUSD M15 Long
# - 检查PinBar信号失败: 错误信息
```

## 注意事项

1. **首次使用**: 需要先配置CosmosDB和Telegram Bot Token
2. **数据源**: 需要配置Oanda或TradeLocker的API密钥
3. **指标计算**: 确保历史数据倍数足够大（建议3倍）以准确计算EMA
4. **信号频率**: 根据品种和周期不同，信号频率差异较大
5. **网络依赖**: 需要稳定的网络连接以获取市场数据和发送Telegram消息

## 故障排查

### 没有收到信号
1. 检查监控是否已启用（状态显示绿色）
2. 确认品种和周期配置正确
3. 查看日志是否有错误信息
4. 验证市场数据是否正常获取

### Telegram未收到消息
1. 检查Telegram Bot Token配置
2. 确认Chat ID正确
3. 验证网络连接

### 数据库错误
1. 确认CosmosDB连接字符串正确
2. 检查容器是否自动创建
3. 查看数据库权限

## 下一步计划

- [ ] 添加更多止损策略选项
- [ ] 实现ADX指标计算
- [ ] 支持自定义禁止交易时间
- [ ] 添加信号统计分析
- [ ] 支持邮件通知
- [ ] 实现信号回测验证

## 技术支持

如有问题，请查看：
- 日志文件
- API返回的错误信息
- CosmosDB数据浏览器
- 网络连接状态
