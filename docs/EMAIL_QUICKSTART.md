# 邮件通知配置快速指南

## 功能介绍

系统现在支持在Telegram发送失败时，自动通过邮件发送通知。也可以配置为同时发送Telegram和邮件双重通知。

## 快速配置

### 1. 配置邮件设置（使用User Secrets）

```bash
cd src/Trading.AlertSystem.Web

# 配置SMTP服务器
dotnet user-secrets set "Email:Enabled" "true"
dotnet user-secrets set "Email:SmtpServer" "smtp.gmail.com"
dotnet user-secrets set "Email:SmtpPort" "587"
dotnet user-secrets set "Email:UseSsl" "true"

# 配置发件人信息
dotnet user-secrets set "Email:FromEmail" "your-email@gmail.com"
dotnet user-secrets set "Email:FromName" "Trading Alert System"
dotnet user-secrets set "Email:Username" "your-email@gmail.com"
dotnet user-secrets set "Email:Password" "your-app-password"

# 配置收件人
dotnet user-secrets set "Email:ToEmails:0" "recipient@example.com"

# 配置备用模式（true=仅Telegram失败时发送，false=始终同时发送）
dotnet user-secrets set "Email:OnlyOnTelegramFailure" "true"
```

### 2. Gmail应用专用密码设置

1. 访问 https://myaccount.google.com/security
2. 启用"两步验证"
3. 搜索"应用专用密码"
4. 生成新密码（选择"邮件"类型）
5. 将生成的16位密码用于上面的 `Email:Password` 配置

### 3. 其他邮箱服务器

#### 163邮箱
```bash
dotnet user-secrets set "Email:SmtpServer" "smtp.163.com"
dotnet user-secrets set "Email:SmtpPort" "465"
# 需要在163邮箱设置中生成授权码
```

#### QQ邮箱
```bash
dotnet user-secrets set "Email:SmtpServer" "smtp.qq.com"
dotnet user-secrets set "Email:SmtpPort" "587"
# 需要在QQ邮箱设置中生成授权码
```

## 使用场景

### 场景1：Telegram失败时发送邮件（推荐）
```bash
dotnet user-secrets set "Email:OnlyOnTelegramFailure" "true"
```
- Telegram成功 → 仅发送Telegram
- Telegram失败 → 自动发送邮件

### 场景2：同时发送Telegram和邮件
```bash
dotnet user-secrets set "Email:OnlyOnTelegramFailure" "false"
```
- 每次告警都会同时发送Telegram和邮件

### 场景3：仅使用邮件
```bash
dotnet user-secrets set "Telegram:Enabled" "false"
dotnet user-secrets set "Email:Enabled" "true"
```

## 验证配置

启动应用后检查日志：
- 成功：`成功发送Telegram消息...`
- 备用邮件：`尝试通过邮件发送通知...`
- 失败：`邮件备用通知也失败了`

## 邮件格式

- **纯文本告警**：邮件正文为告警内容
- **图表告警**：邮件正文为说明，PNG图表作为附件
- **主题格式**：`[Trading Alert] {主题}`

## 常见问题

Q: 邮件发送失败怎么办？
A: 检查User Secrets配置，确保SMTP服务器、端口、用户名、密码都正确

Q: Gmail报错"535 Authentication failed"？
A: 需要启用两步验证并使用应用专用密码，不能使用账号密码

Q: 如何添加多个收件人？
```bash
dotnet user-secrets set "Email:ToEmails:0" "first@example.com"
dotnet user-secrets set "Email:ToEmails:1" "second@example.com"
dotnet user-secrets set "Email:ToEmails:2" "third@example.com"
```

完整配置说明请查看: [docs/EMAIL_SETUP.md](EMAIL_SETUP.md)
