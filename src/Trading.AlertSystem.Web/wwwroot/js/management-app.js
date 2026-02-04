// 管理页面功能

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

    // 每30秒自动刷新统计数据
    setInterval(loadStats, 30000);
});
