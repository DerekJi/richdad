# 邮件通知配置指南

## 功能说明

系统支持在Telegram发送失败时自动发送邮件通知，或者同时发送Telegram和邮件双重通知。

## 配置项

在 `appsettings.json` 或 User Secrets 中配置：

```json
{
  "Email": {
    "Enabled": false,                    // 是否启用邮件通知
    "SmtpServer": "smtp.gmail.com",      // SMTP服务器地址
    "SmtpPort": 587,                     // SMTP端口（通常587或465）
    "UseSsl": true,                      // 是否使用SSL/TLS加密
    "FromEmail": "your-email@gmail.com", // 发件人邮箱
    "FromName": "Trading Alert System",  // 发件人名称
    "Username": "your-email@gmail.com",  // SMTP登录用户名
    "Password": "your-app-password",     // SMTP密码或应用专用密码
    "ToEmails": [                        // 收件人列表
      "recipient1@example.com",
      "recipient2@example.com"
    ],
    "OnlyOnTelegramFailure": true        // true=仅Telegram失败时发送，false=始终同时发送
  }
}
```

## 常见邮箱配置

### Gmail

```json
{
  "SmtpServer": "smtp.gmail.com",
  "SmtpPort": 587,
  "UseSsl": true,
  "Username": "your-email@gmail.com",
  "Password": "应用专用密码"  // 需要启用两步验证并生成应用专用密码
}
```

**获取Gmail应用专用密码：**
1. 访问 https://myaccount.google.com/security
2. 启用两步验证
3. 搜索"应用专用密码"
4. 生成一个新的应用密码（用于邮件）
5. 使用生成的16位密码替换配置中的 `Password`

### Outlook/Hotmail

```json
{
  "SmtpServer": "smtp-mail.outlook.com",
  "SmtpPort": 587,
  "UseSsl": true,
  "Username": "your-email@outlook.com",
  "Password": "your-password"
}
```

### 163邮箱

```json
{
  "SmtpServer": "smtp.163.com",
  "SmtpPort": 465,
  "UseSsl": true,
  "Username": "your-email@163.com",
  "Password": "授权码"  // 需要在163邮箱设置中生成授权码
}
```

### QQ邮箱

```json
{
  "SmtpServer": "smtp.qq.com",
  "SmtpPort": 587,
  "UseSsl": true,
  "Username": "your-email@qq.com",
  "Password": "授权码"  // 需要在QQ邮箱设置中生成授权码
}
```

## 使用场景

### 场景1：仅在Telegram失败时发送邮件（推荐）

```json
{
  "Telegram": {
    "Enabled": true,
    "BotToken": "your-bot-token",
    "DefaultChatId": 123456789
  },
  "Email": {
    "Enabled": true,
    "OnlyOnTelegramFailure": true,
    "SmtpServer": "smtp.gmail.com",
    "FromEmail": "alerts@example.com",
    "ToEmails": ["your-email@example.com"]
  }
}
```

**效果：**
- Telegram发送成功 → 仅发送Telegram
- Telegram发送失败 → 自动发送邮件

### 场景2：同时发送Telegram和邮件

```json
{
  "Email": {
    "Enabled": true,
    "OnlyOnTelegramFailure": false,  // 改为false
    ...
  }
}
```

**效果：**
- 每次告警同时发送Telegram和邮件
- Telegram失败时仍会发送邮件

### 场景3：仅使用邮件通知

```json
{
  "Telegram": {
    "Enabled": false
  },
  "Email": {
    "Enabled": true,
    ...
  }
}
```

## 安全建议

1. **使用User Secrets存储敏感信息**

   ```bash
   cd src/Trading.AlertSystem.Web
   dotnet user-secrets set "Email:Username" "your-email@gmail.com"
   dotnet user-secrets set "Email:Password" "your-app-password"
   dotnet user-secrets set "Email:ToEmails:0" "recipient@example.com"
   ```

2. **不要在appsettings.json中存储密码**
   - 仅在appsettings.json中保留结构配置
   - 将实际的邮箱和密码存储在User Secrets或环境变量中

3. **使用应用专用密码**
   - Gmail、Yahoo等邮箱建议使用应用专用密码而非主密码
   - 更安全，也避免因账号密码修改导致通知失败

## 测试邮件配置

系统启动后，访问Swagger界面：
- 测试Telegram：`GET /api/system/test-telegram`
- 未来可添加测试邮件的端点

## 故障排查

### 邮件发送失败

1. **检查日志**
   ```
   发送邮件失败: SMTP authentication failed
   ```
   → 检查用户名和密码是否正确

2. **连接超时**
   ```
   发送邮件失败: Connection timeout
   ```
   → 检查SMTP服务器地址和端口
   → 确认防火墙未阻止出站连接

3. **SSL错误**
   ```
   发送邮件失败: SSL handshake failed
   ```
   → 尝试切换 `UseSsl` 设置
   → 尝试不同的端口（587或465）

4. **未配置收件人**
   ```
   未配置收件人邮箱
   ```
   → 检查 `ToEmails` 数组是否为空

## 邮件内容格式

- **普通文字告警**：纯文本邮件
- **图表告警**：邮件正文为告警信息，图表作为PNG附件

示例邮件主题：
- `[Trading Alert] Telegram发送失败`
- `[Trading Alert] 交易提醒`
- `[Trading Alert] 交易图表提醒`
