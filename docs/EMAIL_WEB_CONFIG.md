# 邮件通知配置指南（数据库版）

## ✨ 新特性

现在可以在Web界面配置邮件通知，配置保存到数据库，无需修改配置文件！

## 🚀 快速开始

### 1. 启动系统

```bash
cd src/Trading.AlertSystem.Web
dotnet watch
```

### 2. 在Web界面配置邮件

1. 访问 http://localhost:5000/management.html
2. 点击 **"配置邮件"** 按钮
3. 填写邮件配置：

#### Hotmail/Outlook配置（推荐）

- **SMTP预设模板**：选择 "Hotmail/Outlook"（自动填充服务器和端口）
- **发件人邮箱**：your-email@outlook.com 或 @hotmail.com
- **SMTP用户名**：your-email@outlook.com（与发件人邮箱相同）
- **SMTP密码**：你的Outlook/Hotmail密码
- **收件人邮箱**：输入接收告警的邮箱地址（每行一个）

#### 其他可选配置

- **启用邮件通知**：勾选启用
- **使用SSL/TLS加密**：默认勾选（推荐）
- **仅在Telegram失败时发送**：
  - ✅ 勾选 = 只有Telegram失败时才发邮件（推荐）
  - ❌ 不勾选 = 每次告警都同时发送Telegram和邮件

### 3. 测试配置

点击 **"测试连接"** 按钮，系统会发送测试邮件到配置的收件箱。

### 4. 保存配置

点击 **"保存配置"** 按钮，配置会保存到Cosmos DB数据库。

⚠️ **重要**：保存后建议重启应用以应用新配置。

## 📧 支持的邮箱服务

Web界面提供常用邮箱的预设模板：

| 邮箱服务 | SMTP服务器 | 端口 | SSL |
|---------|-----------|------|-----|
| **Hotmail/Outlook** | smtp-mail.outlook.com | 587 | ✅ |
| Gmail | smtp.gmail.com | 587 | ✅ |
| 163邮箱 | smtp.163.com | 465 | ✅ |
| QQ邮箱 | smtp.qq.com | 587 | ✅ |
| Yahoo | smtp.mail.yahoo.com | 587 | ✅ |

### Gmail特别说明

如需使用Gmail，需要：
1. 启用两步验证
2. 生成应用专用密码：https://myaccount.google.com/security
3. 使用应用专用密码而非账号密码

### 163/QQ邮箱特别说明

需要在邮箱设置中生成"授权码"，使用授权码而非登录密码。

## 🎯 使用场景

### 场景1：Telegram备用（推荐）

```
✅ 启用邮件通知
✅ 仅在Telegram失败时发送邮件
```

**效果**：
- Telegram成功 → 只发Telegram
- Telegram失败 → 自动发邮件

### 场景2：双重通知

```
✅ 启用邮件通知
❌ 仅在Telegram失败时发送邮件
```

**效果**：
- 每次告警同时发送Telegram和邮件

### 场景3：仅邮件

在Telegram配置中禁用Telegram，只启用邮件通知。

## 🔐 安全性

- **密码加密**：配置界面显示密码为 `********`
- **数据库存储**：配置保存在Cosmos DB（建议启用加密）
- **修改密码**：
  - 留空 = 保持原密码不变
  - 填写 = 更新为新密码

## 🛠️ API接口

系统提供以下REST API：

### 获取邮件配置

```http
GET /api/emailconfig
```

### 更新邮件配置

```http
POST /api/emailconfig
Content-Type: application/json

{
  "enabled": true,
  "smtpServer": "smtp-mail.outlook.com",
  "smtpPort": 587,
  "useSsl": true,
  "fromEmail": "your-email@outlook.com",
  "fromName": "Trading Alert System",
  "username": "your-email@outlook.com",
  "password": "your-password",
  "toEmails": ["recipient@example.com"],
  "onlyOnTelegramFailure": true
}
```

### 测试邮件

```http
POST /api/emailconfig/test
Content-Type: application/json

{
  "testEmail": "test@example.com"  // 可选
}
```

### 获取SMTP预设

```http
GET /api/emailconfig/presets
```

## 📊 数据库结构

邮件配置存储在Cosmos DB的 `EmailConfig` 容器：

```json
{
  "id": "email-config",
  "partitionKey": "email-config",
  "enabled": true,
  "smtpServer": "smtp-mail.outlook.com",
  "smtpPort": 587,
  "useSsl": true,
  "fromEmail": "your-email@outlook.com",
  "fromName": "Trading Alert System",
  "username": "your-email@outlook.com",
  "password": "your-password",
  "toEmails": ["recipient@example.com"],
  "onlyOnTelegramFailure": true,
  "lastUpdated": "2026-02-05T10:30:00Z"
}
```

## ❓ 常见问题

**Q: 配置保存后什么时候生效？**
A: 需要重启应用。未来可考虑使用 `IOptionsMonitor` 实现热更新。

**Q: 可以配置多个收件人吗？**
A: 可以，在"收件人邮箱"文本框中每行填写一个邮箱地址。

**Q: 如何只修改收件人而不修改密码？**
A: 密码字段留空，系统会保持原密码不变。

**Q: Outlook邮件发送失败怎么办？**
A: 检查：
1. 用户名和密码是否正确
2. 账号是否启用了SMTP访问
3. 防火墙是否阻止了出站连接

**Q: 邮件配置丢失了怎么办？**
A: 配置存储在Cosmos DB，只要数据库存在就不会丢失。如果数据库被清空，系统会自动创建默认配置。

## 🔄 从旧版本迁移

如果之前使用 `appsettings.json` 或 User Secrets 配置邮件，建议：

1. 在Web界面重新配置一次
2. 删除 `appsettings.json` 中的 `Email` 配置（保留模板即可）
3. 删除 User Secrets 中的邮件相关配置

## 📝 技术架构

- **前端**：纯HTML/CSS/JavaScript，无框架依赖
- **后端**：ASP.NET Core 9.0 REST API
- **数据库**：Azure Cosmos DB（本地开发使用模拟器）
- **邮件发送**：System.Net.Mail SMTP客户端

## 🎨 界面预览

配置界面包含：
- SMTP预设模板下拉框（一键选择常用邮箱）
- SMTP服务器和端口配置
- SSL/TLS开关
- 发件人和收件人配置
- 密码输入（显示为 ****）
- 测试连接按钮
- 保存配置按钮

## 💡 提示

1. 首次配置建议使用 Hotmail/Outlook，配置最简单
2. 生产环境建议启用"仅在Telegram失败时发送"以节省邮件配额
3. 可以在Cosmos DB数据浏览器中直接查看和编辑配置
4. 测试时注意检查垃圾邮件文件夹
