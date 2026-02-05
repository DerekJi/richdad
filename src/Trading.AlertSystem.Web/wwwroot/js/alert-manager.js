// 告警管理模块
const AlertManager = {
    // 加载所有告警
    async loadAlerts() {
        try {
            const alerts = await AlertAPI.getAll();
            const alertList = document.getElementById('alertList');

            if (alerts.length === 0) {
                alertList.innerHTML = `
                    <div class="empty-state">
                        <div class="empty-state-icon">📭</div>
                        <h3>还没有告警</h3>
                        <p>点击"创建规则"按钮开始设置价格监控</p>
                    </div>
                `;
                return;
            }

            alertList.innerHTML = alerts.map(alert => this.createAlertCard(alert)).join('');
        } catch (error) {
            console.error('加载告警失败:', error);
            alert('加载告警失败: ' + error.message);
        }
    },

    // 创建告警卡片HTML
    createAlertCard(alert) {
        // 处理枚举字符串转数字
        let alertType = alert.type;
        if (typeof alertType === 'string') {
            // 枚举字符串映射: FixedPrice=0, EMA=1, MA=2
            const typeMap = { 'FixedPrice': 0, 'EMA': 1, 'MA': 2 };
            alertType = typeMap[alertType] ?? 0;
        } else {
            alertType = parseInt(alertType ?? 0);
        }

        let direction = alert.direction;
        if (typeof direction === 'string') {
            // 枚举字符串映射: Above=0, Below=1
            direction = direction === 'Above' ? 0 : 1;
        } else {
            direction = parseInt(direction ?? 0);
        }

        const typeText = ['固定价格', 'EMA', 'MA'][alertType] || '未知';
        const directionText = direction === 0 ? '上穿' : '下穿';
        const statusClass = alert.isTriggered ? 'triggered' : (alert.enabled ? '' : 'disabled');
        const statusBadge = alert.isTriggered
            ? '<span class="status-badge status-triggered">已触发</span>'
            : (alert.enabled
                ? '<span class="status-badge status-active">启用</span>'
                : '<span class="status-badge status-disabled">禁用</span>');

        let targetText = '';
        if (alertType === 0 && alert.targetPrice) {
            targetText = alert.targetPrice.toString();
        } else if (alertType === 1 && alert.emaPeriod) {
            targetText = `EMA(${alert.emaPeriod})`;
        } else if (alertType === 2 && alert.maPeriod) {
            targetText = `MA(${alert.maPeriod})`;
        } else {
            targetText = '-';
        }

        return `
            <div class="alert-card ${statusClass}">
                <div class="alert-header">
                    <div>
                        <div class="alert-title">${alert.name}</div>
                        <div style="margin-top: 5px;">${statusBadge}</div>
                    </div>
                    <div class="alert-symbol">${alert.symbol}</div>
                </div>

                <div class="alert-details">
                    <div class="detail-item">
                        <span class="detail-label">告警类型</span>
                        <span class="detail-value">${typeText}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">目标值</span>
                        <span class="detail-value">${targetText}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">方向</span>
                        <span class="detail-value">${directionText}</span>
                    </div>
                    <div class="detail-item">
                        <span class="detail-label">时间周期</span>
                        <span class="detail-value">${alert.timeFrame || 'M5'}</span>
                    </div>
                </div>

                ${alert.lastTriggeredAt ? `
                    <div style="margin-top: 10px; color: #6c757d; font-size: 0.9em;">
                        最后触发: ${new Date(alert.lastTriggeredAt).toLocaleString('zh-CN')}
                    </div>
                ` : ''}

                <div class="alert-actions">
                    <button class="btn btn-primary" onclick="UI.editAlert('${alert.id}')">编辑</button>
                    ${alert.isTriggered ? `
                        <button class="btn btn-success" onclick="AlertManager.resetAlert('${alert.id}')">重置</button>
                    ` : ''}
                    <button class="btn btn-danger" onclick="AlertManager.deleteAlert('${alert.id}')">删除</button>
                </div>
            </div>
        `;
    },

    // 删除告警
    async deleteAlert(id) {
        if (!confirm('确定要删除这个告警吗？')) return;

        try {
            const response = await AlertAPI.delete(id);
            if (response.ok) {
                this.loadAlerts();
                alert('告警删除成功');
            } else {
                alert('删除失败');
            }
        } catch (error) {
            alert('删除失败: ' + error.message);
        }
    },

    // 重置告警
    async resetAlert(id) {
        try {
            const response = await AlertAPI.reset(id);
            if (response.ok) {
                this.loadAlerts();
                alert('告警已重置');
            } else {
                alert('重置失败');
            }
        } catch (error) {
            alert('重置失败: ' + error.message);
        }
    }
};
