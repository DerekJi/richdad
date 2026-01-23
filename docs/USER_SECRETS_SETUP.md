# User Secrets 配置示例

本文档说明如何使用 User Secrets 配置敏感信息。

## 初始化 User Secrets

```bash
cd src/Trading.AlertSystem.Web
dotnet user-secrets init
```

## 配置 TradeLocker

```bash
# 设置环境（demo 或 live）
dotnet user-secrets set "TradeLocker:Environment" "demo"

# 设置登录凭证
dotnet user-secrets set "TradeLocker:Email" "your-email@example.com"
dotnet user-secrets set "TradeLocker:Password" "your-password"
dotnet user-secrets set "TradeLocker:Server" "your-server-name"

# 设置账户信息
dotnet user-secrets set "TradeLocker:AccountId" "123456"
dotnet user-secrets set "TradeLocker:AccountNumber" "1"

# 可选：开发者API密钥（提高速率限制）
dotnet user-secrets set "TradeLocker:DeveloperApiKey" "your-api-key"
```

## 配置 Telegram

```bash
# 设置 Bot Token（从 @BotFather 获取）
dotnet user-secrets set "Telegram:BotToken" "123456789:ABCdefGHIjklMNOpqrsTUVwxyz"

# 设置默认 Chat ID（从 @userinfobot 获取）
dotnet user-secrets set "Telegram:DefaultChatId" "123456789"

# 启用 Telegram 通知
dotnet user-secrets set "Telegram:Enabled" "true"
```

## 配置 CosmosDB

```bash
# 本地 Cosmos DB Emulator 连接字符串
dotnet user-secrets set "CosmosDb:ConnectionString" "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="

# 或使用 Azure Cosmos DB
dotnet user-secrets set "CosmosDb:ConnectionString" "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-primary-key=="

# 数据库名称（可选，默认为 TradingSystem）
dotnet user-secrets set "CosmosDb:DatabaseName" "TradingSystem"

# 容器名称（可选，默认为 PriceAlerts）
dotnet user-secrets set "CosmosDb:AlertContainerName" "PriceAlerts"
```

## 查看已配置的 Secrets

```bash
dotnet user-secrets list
```

## 删除 Secret

```bash
# 删除单个配置
dotnet user-secrets remove "TradeLocker:Email"

# 清空所有配置
dotnet user-secrets clear
```

## 注意事项

1. **安全性**: User Secrets 仅存储在本地开发机器上，不会被提交到代码库
2. **位置**: Windows上secrets文件位于 `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
3. **生产环境**: 生产环境应使用Azure Key Vault等安全存储服务
4. **不配置的后果**:
   - 不配置 TradeLocker/Telegram：会使用演示模式（模拟数据）
   - 不配置 CosmosDB：会使用内存存储（重启后数据丢失）

## 完整配置示例

执行以下所有命令以完成配置：

```bash
cd src/Trading.AlertSystem.Web

# 初始化
dotnet user-secrets init

# TradeLocker 配置
dotnet user-secrets set "TradeLocker:Environment" "demo"
dotnet user-secrets set "TradeLocker:Email" "your-email@example.com"
dotnet user-secrets set "TradeLocker:Password" "your-password"
dotnet user-secrets set "TradeLocker:Server" "your-server"
dotnet user-secrets set "TradeLocker:AccountId" "123456"
dotnet user-secrets set "TradeLocker:AccountNumber" "1"

# Telegram 配置
dotnet user-secrets set "Telegram:BotToken" "123456789:ABC..."
dotnet user-secrets set "Telegram:DefaultChatId" "123456789"
dotnet user-secrets set "Telegram:Enabled" "true"

# CosmosDB 配置（可选）
dotnet user-secrets set "CosmosDb:ConnectionString" "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5..."

# 验证配置
dotnet user-secrets list
```
