// 表单提交处理
document.addEventListener('DOMContentLoaded', () => {
    // 提交表单
    document.getElementById('alertForm').addEventListener('submit', async (e) => {
        e.preventDefault();

        const id = document.getElementById('alertId').value;
        const type = parseInt(document.getElementById('alertType').value);

        const data = {
            name: document.getElementById('alertName').value,
            symbol: document.getElementById('alertSymbol').value,
            type: type,
            direction: parseInt(document.getElementById('direction').value),
            timeFrame: document.getElementById('timeFrame').value,
            messageTemplate: document.getElementById('messageTemplate').value || null,
            telegramChatId: null, // 使用系统默认的 Telegram Chat ID
            enabled: document.getElementById('enabled').checked
        };

        if (type === 0) {
            data.targetPrice = parseFloat(document.getElementById('targetPrice').value);
        } else if (type === 1) {
            data.emaPeriod = parseInt(document.getElementById('emaPeriod').value);
        } else if (type === 2) {
            data.maPeriod = parseInt(document.getElementById('maPeriod').value);
        }

        try {
            const response = id ? await AlertAPI.update(id, data) : await AlertAPI.create(data);

            if (response.ok) {
                UI.closeModal();
                AlertManager.loadAlerts();
                alert(id ? '告警更新成功' : '告警创建成功');
            } else {
                const error = await response.text();
                alert('操作失败: ' + error);
            }
        } catch (error) {
            alert('操作失败: ' + error.message);
        }
    });

    // 页面加载时自动加载告警
    AlertManager.loadAlerts();
});
