// 管理页面功能

// 页面加载时获取当前数据源
document.addEventListener('DOMContentLoaded', async () => {
    await loadCurrentDataSource();
});

// 加载当前数据源
async function loadCurrentDataSource() {
    try {
        const response = await fetch('/api/datasource');
        if (response.ok) {
            const result = await response.json();
            document.getElementById('currentProvider').textContent = result.provider;
            document.getElementById('dataSourceSelect').value = result.provider;
        }
    } catch (error) {
        document.getElementById('currentProvider').textContent = '加载失败';
        console.error('加载数据源失败:', error);
    }
}

// 切换数据源
async function switchDataSource() {
    const button = event.target;
    const select = document.getElementById('dataSourceSelect');
    const provider = select.value;

    button.disabled = true;
    button.textContent = '切换中...';

    try {
        const response = await fetch('/api/datasource', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ provider: provider })
        });

        const result = await response.json();

        if (response.ok) {
            showResult('dataSourceResult',
                `✅ ${result.message}\n\n⚠️ ${result.note}\n\n🔄 页面将在3秒后重新加载...`,
                'success');

            // 更新显示
            document.getElementById('currentProvider').textContent = provider;

            // 3秒后重新加载页面
            setTimeout(() => {
                window.location.reload();
            }, 3000);
        } else {
            showResult('dataSourceResult', `❌ ${result.message}`, 'error');
        }
    } catch (error) {
        showResult('dataSourceResult', `❌ 请求失败: ${error.message}`, 'error');
    } finally {
        button.disabled = false;
        button.textContent = '切换并重启';
    }
}

// 显示结果
function showResult(elementId, message, type = 'info') {
    const resultBox = document.getElementById(elementId);
    resultBox.textContent = message;
    resultBox.className = `result-box show ${type}`;

    // 5秒后自动隐藏（除非是错误）
    if (type !== 'error') {
        setTimeout(() => {
            resultBox.classList.remove('show');
        }, 5000);
    }
}

// 测试 Telegram
async function testTelegram() {
    const button = event.target;
    button.disabled = true;
    button.textContent = '测试中...';

    try {
        const response = await fetch('/api/system/test-telegram', {
            method: 'POST'
        });

        const contentType = response.headers.get('content-type');
        let result;

        if (contentType && contentType.includes('application/json')) {
            result = await response.json();
        } else {
            result = await response.text();
        }

        if (response.ok) {
            const message = typeof result === 'object' ? result.message : result;
            showResult('telegramResult', `✅ ${message}`, 'success');
        } else {
            const errorMsg = typeof result === 'object' ? result.message : result;
            showResult('telegramResult', `❌ ${errorMsg}\n\n💡 常见原因：\n• BotToken 未配置或无效\n• 网络连接问题（某些地区需要代理访问 Telegram）\n• 防火墙阻止了 Telegram API (api.telegram.org)\n\n请运行: dotnet user-secrets set "Telegram:BotToken" "YOUR_BOT_TOKEN"`, 'error');
        }
    } catch (error) {
        showResult('telegramResult', `❌ 请求失败: ${error.message}`, 'error');
    } finally {
        button.disabled = false;
        button.textContent = '测试 Telegram';
    }
}

// 测试 TradeLocker
async function testTradeLocker() {
    const button = event.target;
    button.disabled = true;
    button.textContent = '测试中...';

    try {
        const response = await fetch('/api/system/test-tradelocker', {
            method: 'POST'
        });

        if (response.ok) {
            const result = await response.text();
            showResult('tradelockerResult', `✅ ${result}`, 'success');
        } else {
            const error = await response.text();
            showResult('tradelockerResult', `❌ ${error}`, 'error');
        }
    } catch (error) {
        showResult('tradelockerResult', `❌ 请求失败: ${error.message}`, 'error');
    } finally {
        button.disabled = false;
        button.textContent = '测试 TradeLocker';
    }
}

// 测试 OANDA
async function testOanda() {
    const button = event.target;
    button.disabled = true;
    button.textContent = '测试中...';

    try {
        const response = await fetch('/api/system/test-oanda', {
            method: 'POST'
        });

        const result = await response.json();

        if (response.ok) {
            let details = '';
            if (result.accountInfo) {
                details = `\n\n账户信息：\n` +
                    `• 账户ID: ${result.accountInfo.accountId}\n` +
                    `• 账户名: ${result.accountInfo.accountName}\n` +
                    `• 余额: ${result.accountInfo.balance} ${result.accountInfo.currency}\n` +
                    `• 净值: ${result.accountInfo.equity}\n` +
                    `• 已用保证金: ${result.accountInfo.margin}\n` +
                    `• 可用保证金: ${result.accountInfo.freeMargin}`;
            }
            if (result.testPrice) {
                details += `\n\n测试价格 (${result.testPrice.symbol})：\n` +
                    `• Bid: ${result.testPrice.bid}\n` +
                    `• Ask: ${result.testPrice.ask}`;
            }
            showResult('oandaResult', `✅ ${result.message}${details}`, 'success');
        } else {
            showResult('oandaResult', `❌ ${result.message}\n\n💡 请确保已配置OANDA API密钥和账户ID`, 'error');
        }
    } catch (error) {
        showResult('oandaResult', `❌ 请求失败: ${error.message}`, 'error');
    } finally {
        button.disabled = false;
        button.textContent = '测试 OANDA';
    }
}

// 测试 K线图
async function testChart() {
    const button = event.target;
    const symbol = document.getElementById('chartSymbol').value.trim().toUpperCase() || 'XAUUSD';

    button.disabled = true;
    button.textContent = '生成中...';
    showResult('chartResult', '⏳ 正在生成K线图并发送到Telegram，请稍候...', 'info');

    try {
        const response = await fetch(`/api/system/test-chart?symbol=${encodeURIComponent(symbol)}`, {
            method: 'POST'
        });

        const result = await response.json();

        if (response.ok) {
            showResult('chartResult', `✅ ${result.message}\n\n📱 请查看Telegram接收的图片（包含M5、M15、H1、H4四个时间周期）`, 'success');
        } else {
            showResult('chartResult', `❌ ${result.message}`, 'error');
        }
    } catch (error) {
        showResult('chartResult', `❌ 请求失败: ${error.message}`, 'error');
    } finally {
        button.disabled = false;
        button.textContent = '发送K线图';
    }
}

// 立即检查
async function checkNow() {
    const button = event.target;
    button.disabled = true;
    button.textContent = '检查中...';

    try {
        const response = await fetch('/api/system/check-now', {
            method: 'POST'
        });

        if (response.ok) {
            const result = await response.text();
            showResult('checkResult', `✅ ${result}`, 'success');
            // 刷新统计数据
            setTimeout(loadStats, 1000);
        } else {
            const error = await response.text();
            showResult('checkResult', `❌ ${error}`, 'error');
        }
    } catch (error) {
        showResult('checkResult', `❌ 请求失败: ${error.message}`, 'error');
    } finally {
        button.disabled = false;
        button.textContent = '立即检查';
    }
}

// 清理历史
async function cleanupHistory() {
    if (!confirm('确定要清理90天前的历史记录吗？此操作不可撤销。')) {
        return;
    }

    const button = event.target;
    button.disabled = true;
    button.textContent = '清理中...';

    try {
        const response = await fetch('/api/alerthistory/cleanup?days=90', {
            method: 'DELETE'
        });

        if (response.ok) {
            const result = await response.json();
            showResult('cleanupResult',
                `✅ 成功删除 ${result.deletedCount} 条历史记录`,
                'success');
            // 刷新统计数据
            setTimeout(loadStats, 1000);
        } else {
            const error = await response.text();
            showResult('cleanupResult', `❌ ${error}`, 'error');
        }
    } catch (error) {
        showResult('cleanupResult', `❌ 请求失败: ${error.message}`, 'error');
    } finally {
        button.disabled = false;
        button.textContent = '清理历史';
    }
}

// 加载系统状态
async function loadStats() {
    try {
        // 获取活跃告警数量
        const alertsResponse = await fetch('/api/alerts');
        if (alertsResponse.ok) {
            const alerts = await alertsResponse.json();
            document.getElementById('activeAlerts').textContent =
                Array.isArray(alerts) ? alerts.length : '0';
        }

        // 获取历史统计
        const statsResponse = await fetch('/api/alerthistory/stats');
        if (statsResponse.ok) {
            const stats = await statsResponse.json();
            document.getElementById('historyCount').textContent =
                stats.totalAlerts || '0';

            const successRate = stats.totalAlerts > 0
                ? ((stats.successCount / stats.totalAlerts) * 100).toFixed(1) + '%'
                : '-';
            document.getElementById('successRate').textContent = successRate;
        }

        // 设置最后检查时间
        document.getElementById('lastCheck').textContent =
            new Date().toLocaleTimeString('zh-CN');

    } catch (error) {
        console.error('加载统计数据失败:', error);
    }
}

// 加载配置状态
async function loadConfigStatus() {
    try {
        const response = await fetch('/api/config/status');
        if (!response.ok) {
            throw new Error('无法获取配置状态');
        }

        const config = await response.json();
        const container = document.getElementById('configStatus');

        let html = '';

        // Telegram 配置
        const telegramStatus = config.telegram.botTokenConfigured ? 'ok' : 'warning';
        const telegramBadge = config.telegram.isDemo ?
            '<span class="status-badge warning">演示模式</span>' :
            '<span class="status-badge ok">已配置</span>';

        html += `
            <div class="config-item">
                <h4>📱 Telegram ${telegramBadge}</h4>
                <ul>
                    <li>已启用: ${config.telegram.enabled ? '✅' : '❌'}</li>
                    <li>Bot Token: ${config.telegram.botTokenConfigured ? '✅ 已配置' : '❌ 未配置'}</li>
                    <li>Chat ID: ${config.telegram.chatIdConfigured ? '✅ 已配置 (' + config.telegram.chatId + ')' : '❌ 未配置'}</li>
                </ul>
                ${!config.telegram.botTokenConfigured ? `
                    <div class="help-text">
                        💡 配置 Telegram：<br>
                        <code>dotnet user-secrets set "Telegram:BotToken" "YOUR_TOKEN"</code><br>
                        <code>dotnet user-secrets set "Telegram:DefaultChatId" "YOUR_CHAT_ID"</code>
                    </div>
                ` : ''}
            </div>
        `;

        // TradeLocker 配置
        const tradeLockerStatus = config.tradeLocker.emailConfigured ? 'ok' : 'warning';
        const tradeLockerBadge = config.tradeLocker.isDemo ?
            '<span class="status-badge warning">演示模式</span>' :
            '<span class="status-badge ok">已配置</span>';

        html += `
            <div class="config-item">
                <h4>📈 TradeLocker ${tradeLockerBadge}</h4>
                <ul>
                    <li>环境: ${config.tradeLocker.environment || '未设置'}</li>
                    <li>邮箱: ${config.tradeLocker.emailConfigured ? '✅ 已配置' : '❌ 未配置'}</li>
                    <li>密码: ${config.tradeLocker.passwordConfigured ? '✅ 已配置' : '❌ 未配置'}</li>
                    <li>服务器: ${config.tradeLocker.serverConfigured ? '✅ 已配置' : '❌ 未配置'}</li>
                    <li>账户ID: ${config.tradeLocker.accountIdConfigured ? '✅ 已配置' : '❌ 未配置'}</li>
                </ul>
                ${!config.tradeLocker.emailConfigured ? `
                    <div class="help-text">
                        💡 配置 TradeLocker：<br>
                        <code>dotnet user-secrets set "TradeLocker:Environment" "demo"</code><br>
                        <code>dotnet user-secrets set "TradeLocker:Email" "YOUR_EMAIL"</code><br>
                        查看完整文档: <a href="https://github.com/..." target="_blank">USER_SECRETS_SETUP.md</a>
                    </div>
                ` : ''}
            </div>
        `;

        container.innerHTML = html;

    } catch (error) {
        console.error('加载配置状态失败:', error);
        document.getElementById('configStatus').innerHTML = `
            <div class="error">❌ 无法加载配置状态: ${error.message}</div>
        `;
    }
}


// 页面加载时初始化
document.addEventListener('DOMContentLoaded', () => {
    loadConfigStatus();
    loadStats();
    loadSmtpPresets();

    // 每30秒自动刷新统计数据
    setInterval(loadStats, 30000);
});

// ========== 邮件配置功能 ==========

// 打开邮件配置弹窗
async function openEmailConfig() {
    try {
        const response = await fetch('/api/emailconfig');
        if (response.ok) {
            const config = await response.json();

            document.getElementById('emailEnabled').checked = config.enabled;
            document.getElementById('smtpServer').value = config.smtpServer;
            document.getElementById('smtpPort').value = config.smtpPort;
            document.getElementById('useSsl').checked = config.useSsl;
            document.getElementById('fromEmail').value = config.fromEmail;
            document.getElementById('fromName').value = config.fromName;
            document.getElementById('username').value = config.username;
            document.getElementById('password').value = ''; // 不显示密码
            document.getElementById('toEmails').value = config.toEmails.join('\n');
            document.getElementById('onlyOnTelegramFailure').checked = config.onlyOnTelegramFailure;
        }
    } catch (error) {
        console.error('加载邮件配置失败:', error);
    }

    document.getElementById('emailConfigModal').style.display = 'block';
}

// 关闭邮件配置弹窗
function closeEmailConfig() {
    document.getElementById('emailConfigModal').style.display = 'none';
}

// 加载SMTP预设
async function loadSmtpPresets() {
    try {
        const response = await fetch('/api/emailconfig/presets');
        if (response.ok) {
            const presets = await response.json();
            const select = document.getElementById('smtpPreset');

            presets.forEach(preset => {
                const option = document.createElement('option');
                option.value = JSON.stringify(preset);
                option.textContent = preset.name;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('加载SMTP预设失败:', error);
    }
}

// 应用SMTP预设
function applySmtpPreset() {
    const select = document.getElementById('smtpPreset');
    const value = select.value;

    if (value) {
        const preset = JSON.parse(value);
        document.getElementById('smtpServer').value = preset.server;
        document.getElementById('smtpPort').value = preset.port;
        document.getElementById('useSsl').checked = preset.useSsl;
    }
}

// 保存邮件配置
async function saveEmailConfig(event) {
    event.preventDefault();

    const button = event.target.querySelector('button[type="submit"]');
    const originalText = button.textContent;
    button.disabled = true;
    button.textContent = '保存中...';

    const toEmailsText = document.getElementById('toEmails').value;
    const toEmails = toEmailsText.split('\n')
        .map(e => e.trim())
        .filter(e => e.length > 0);

    const password = document.getElementById('password').value;

    const config = {
        enabled: document.getElementById('emailEnabled').checked,
        smtpServer: document.getElementById('smtpServer').value,
        smtpPort: parseInt(document.getElementById('smtpPort').value),
        useSsl: document.getElementById('useSsl').checked,
        fromEmail: document.getElementById('fromEmail').value,
        fromName: document.getElementById('fromName').value,
        username: document.getElementById('username').value,
        password: password || '********', // 如果没填密码，发送掩码保持原密码
        toEmails: toEmails,
        onlyOnTelegramFailure: document.getElementById('onlyOnTelegramFailure').checked
    };

    try {
        const response = await fetch('/api/emailconfig', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(config)
        });

        const result = await response.json();

        if (response.ok) {
            showModalResult('emailConfigResult', '✅ 邮件配置已保存！\n\n⚠️ 建议重启应用以应用新配置。', 'success');
        } else {
            showModalResult('emailConfigResult', `❌ 保存失败: ${result.error || result.details}`, 'error');
        }
    } catch (error) {
        showModalResult('emailConfigResult', `❌ 保存失败: ${error.message}`, 'error');
    } finally {
        button.disabled = false;
        button.textContent = originalText;
    }
}

// 测试邮件连接
async function testEmailConnection() {
    const button = event.target;
    button.disabled = true;
    button.textContent = '测试中...';

    try {
        // 先保存配置
        await saveEmailConfigSilently();

        // 然后测试
        const response = await fetch('/api/emailconfig/test', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({})
        });

        const result = await response.json();

        if (response.ok) {
            showModalResult('emailConfigResult', `✅ ${result.message}`, 'success');
        } else {
            showModalResult('emailConfigResult', `❌ 测试失败: ${result.error || result.details}`, 'error');
        }
    } catch (error) {
        showModalResult('emailConfigResult', `❌ 测试失败: ${error.message}`, 'error');
    } finally {
        button.disabled = false;
        button.textContent = '测试连接';
    }
}

// 静默保存配置（不显示结果）
async function saveEmailConfigSilently() {
    const toEmailsText = document.getElementById('toEmails').value;
    const toEmails = toEmailsText.split('\n')
        .map(e => e.trim())
        .filter(e => e.length > 0);

    const password = document.getElementById('password').value;

    const config = {
        enabled: document.getElementById('emailEnabled').checked,
        smtpServer: document.getElementById('smtpServer').value,
        smtpPort: parseInt(document.getElementById('smtpPort').value),
        useSsl: document.getElementById('useSsl').checked,
        fromEmail: document.getElementById('fromEmail').value,
        fromName: document.getElementById('fromName').value,
        username: document.getElementById('username').value,
        password: password || '********',
        toEmails: toEmails,
        onlyOnTelegramFailure: document.getElementById('onlyOnTelegramFailure').checked
    };

    await fetch('/api/emailconfig', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
    });
}

// 显示弹窗内的结果消息
function showModalResult(elementId, message, type = 'info') {
    const element = document.getElementById(elementId);
    element.className = `result-box ${type}`;
    element.textContent = message;
    element.style.display = 'block';
}
