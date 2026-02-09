# 文档索引

## 📚 文档概览

本目录包含智能交易系统的完整文档。

## 🚀 快速开始

如果你是新用户，建议按以下顺序阅读：

1. [项目README](../README.md) - 了解项目概况
2. [快速开始指南](../QUICKSTART.md) - 快速上手
3. [告警系统快速入门](ALERT_SYSTEM_QUICKSTART.md) - 配置第一个告警
4. [用户密钥配置](setup/USER_SECRETS_SETUP.md) - 安全配置敏感信息

## 📋 文档分类

### 快速入门指南

- [告警系统快速入门](ALERT_SYSTEM_QUICKSTART.md) - 价格监控和告警配置
- [Pin Bar监控快速入门](PINBAR_QUICKSTART.md) - Pin Bar形态自动监控
- [双级AI快速入门](DUAL_TIER_AI_QUICKSTART.md) - AI智能分析配置
- [邮件快速入门](EMAIL_QUICKSTART.md) - 邮件通知配置
- [Telegram按钮快速入门](TELEGRAM_BUTTONS_QUICKSTART.md) - 交互式Telegram Bot
- [邮件Web配置](EMAIL_WEB_CONFIG.md) - Web界面邮件配置

### 配置指南 (setup/)

#### AI服务配置
- [Azure OpenAI配置](setup/AZURE_OPENAI_SETUP.md) - GPT-4o模型配置
- [DeepSeek集成指南](setup/DEEPSEEK_INTEGRATION_GUIDE.md) - DeepSeek API配置

#### 数据存储配置
- [Azure Table Storage指南](setup/AZURE_TABLE_STORAGE_GUIDE.md) - 低成本NoSQL存储（推荐）
- [配置指南](setup/CONFIG_GUIDE.md) - 通用配置说明

#### 安全配置
- [用户密钥配置](setup/USER_SECRETS_SETUP.md) - 安全存储敏感信息
- [邮件配置](setup/EMAIL_SETUP.md) - SMTP邮件服务配置

### 使用指南 (guides/)

#### AI功能
- [双级AI架构指南](guides/DUAL_TIER_AI_GUIDE.md) - 双级AI决策系统详解
- [双级AI实现](guides/DUAL_TIER_AI_IMPLEMENTATION.md) - 技术实现细节

#### Telegram功能
- [Telegram按钮指南](guides/TELEGRAM_BUTTONS_GUIDE.md) - 交互式按钮功能
- [Telegram按钮实现](guides/TELEGRAM_BUTTONS_IMPLEMENTATION.md) - 技术实现

#### 功能指南
- [仓位计算器](guides/POSITION_CALCULATOR.md) - 智能仓位管理
- [指标路线图](guides/INDICATOR_ROADMAP.md) - 技术指标扩展计划
- [移动端部署](guides/MOBILE_DEPLOYMENT.md) - 无数据库运行模式

### 策略文档 (strategies/)

- [Pin Bar策略](strategies/pin-bar.strategy.md) - Pin Bar交易策略详解

### 项目管理 (issues/)

- [Issues概览](issues/README.md) - 所有功能需求和实现记录
- [已完成](issues/completed/) - 已完成的功能
- [进行中](issues/in-progress/) - 正在开发的功能
- [计划中](issues/planned/) - 计划开发的功能

### 维护文档 (maintenance/)

- [代码清理计划](maintenance/CLEANUP_PLAN.md) - 代码库清理和优化

## 🔍 按功能查找

### 实时监控
- [告警系统快速入门](ALERT_SYSTEM_QUICKSTART.md)
- [Pin Bar监控](PINBAR_QUICKSTART.md)

### AI分析
- [双级AI快速入门](DUAL_TIER_AI_QUICKSTART.md)
- [Azure OpenAI配置](setup/AZURE_OPENAI_SETUP.md)
- [DeepSeek集成](setup/DEEPSEEK_INTEGRATION_GUIDE.md)

### 通知服务
- [Telegram配置](setup/USER_SECRETS_SETUP.md)
- [邮件配置](setup/EMAIL_SETUP.md)
- [Telegram按钮](guides/TELEGRAM_BUTTONS_GUIDE.md)

### 数据存储
- [Azure Table Storage](setup/AZURE_TABLE_STORAGE_GUIDE.md)
- [移动端部署（内存模式）](guides/MOBILE_DEPLOYMENT.md)

### 风险管理
- [仓位计算器](guides/POSITION_CALCULATOR.md)

## 📝 文档维护

本文档目录结构：

```
docs/
├── README.md                          # 本文档索引
├── ALERT_SYSTEM_QUICKSTART.md        # 告警系统快速入门
├── PINBAR_QUICKSTART.md               # Pin Bar监控快速入门
├── DUAL_TIER_AI_QUICKSTART.md         # 双级AI快速入门
├── EMAIL_QUICKSTART.md                # 邮件快速入门
├── EMAIL_WEB_CONFIG.md                # 邮件Web配置
├── TELEGRAM_BUTTONS_QUICKSTART.md     # Telegram按钮快速入门
├── setup/                             # 配置指南
│   ├── AZURE_OPENAI_SETUP.md
│   ├── AZURE_TABLE_STORAGE_GUIDE.md
│   ├── CONFIG_GUIDE.md
│   ├── DEEPSEEK_INTEGRATION_GUIDE.md
│   ├── EMAIL_SETUP.md
│   └── USER_SECRETS_SETUP.md
├── guides/                            # 使用指南
│   ├── DUAL_TIER_AI_GUIDE.md
│   ├── DUAL_TIER_AI_IMPLEMENTATION.md
│   ├── TELEGRAM_BUTTONS_GUIDE.md
│   ├── TELEGRAM_BUTTONS_IMPLEMENTATION.md
│   ├── INDICATOR_ROADMAP.md
│   ├── POSITION_CALCULATOR.md
│   └── MOBILE_DEPLOYMENT.md
├── strategies/                        # 策略文档
│   └── pin-bar.strategy.md
├── issues/                            # 项目管理
│   ├── README.md
│   ├── completed/
│   ├── in-progress/
│   └── planned/
└── maintenance/                       # 维护文档
    └── CLEANUP_PLAN.md
```

## 🤝 贡献

如果你发现文档有错误或需要改进，欢迎：
1. 提交Issue报告问题
2. 提交Pull Request改进文档

## 📧 联系

如有问题，请在GitHub Issues中提问。
