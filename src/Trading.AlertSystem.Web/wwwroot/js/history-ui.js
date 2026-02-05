// 告警历史 UI 渲染
const HistoryUI = {
    // 显示加载状态
    showLoading() {
        const container = document.getElementById('historyList');
        container.innerHTML = `
            <div class="loading">
                <p>⏳ 加载中...</p>
            </div>
        `;
    },

    // 显示错误信息
    showError(message) {
        const container = document.getElementById('historyList');
        container.innerHTML = `
            <div class="empty-state">
                <div class="empty-state-icon">⚠️</div>
                <div class="empty-state-text">${message}</div>
            </div>
        `;
    },

    // 渲染告警历史列表
    renderHistory(items) {
        const container = document.getElementById('historyList');

        if (!items || items.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <div class="empty-state-icon">📭</div>
                    <div class="empty-state-text">暂无触发记录</div>
                    <div class="empty-state-hint">当价格监控触发时，记录会显示在这里</div>
                </div>
            `;
            return;
        }

        const html = items.map(item => this.renderHistoryItem(item)).join('');
        container.innerHTML = html;
    },

    // 渲染单个告警历史项
    renderHistoryItem(item) {
        // type 可能是字符串 "PriceAlert"/"EmaCross" 或数字 0/1
        const isPriceAlert = item.type === 'PriceAlert' || item.type === 0;
        const typeClass = isPriceAlert ? 'price' : 'ema';
        const typeText = isPriceAlert ? '💰 价格规则' : '📊 EMA穿越';
        const time = new Date(item.alertTime).toLocaleString('zh-CN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });

        let detailsHtml = '';
        if (item.details) {
            try {
                const details = JSON.parse(item.details);
                if (isPriceAlert) {
                    // 价格告警
                    const targetPrice = details.TargetPrice || details.targetPrice;
                    const currentPrice = details.CurrentPrice || details.currentPrice;
                    const direction = details.Direction || details.direction;

                    // 构建详情显示
                    let detailParts = [];
                    detailParts.push(`目标价: ${targetPrice != null ? Number(targetPrice).toFixed(2) : 'N/A'}`);
                    if (currentPrice != null) {
                        detailParts.push(`触发价: ${Number(currentPrice).toFixed(2)}`);
                    }
                    detailParts.push(`方向: ${direction === 'Above' ? '上穿 ⬆️' : '下穿 ⬇️'}`);

                    detailsHtml = `
                        <div class="history-details">
                            ${detailParts.join(' | ')}
                        </div>
                    `;
                } else {
                    // EMA穿越
                    detailsHtml = `
                        <div class="history-details">
                            周期: ${details.timeFrame || details.TimeFrame || 'N/A'} |
                            EMA${details.emaPeriod || details.EmaPeriod || 'N/A'}: ${details.emaValue?.toFixed(4) || details.EmaValue?.toFixed(4) || 'N/A'} |
                            收盘价: ${details.closePrice?.toFixed(4) || details.ClosePrice?.toFixed(4) || 'N/A'} |
                            ${(details.crossType || details.CrossType) === 'CrossAbove' ? '上穿 ⬆️' : '下穿 ⬇️'}
                        </div>
                    `;
                }
            } catch (e) {
                console.error('解析详情失败:', e);
            }
        }

        return `
            <div class="history-item" onclick="HistoryManager.viewDetail('${item.id}')">
                <div class="history-header">
                    <div>
                        <span class="history-type ${typeClass}">${typeText}</span>
                        <span class="history-symbol">${item.symbol}</span>
                    </div>
                    <div class="history-time">🕐 ${time}</div>
                </div>
                ${detailsHtml}
                <div class="history-message">${this.escapeHtml(item.message)}</div>
            </div>
        `;
    },

    // 渲染分页
    renderPagination(result) {
        const container = document.getElementById('pagination');
        if (!result || result.totalPages <= 1) {
            container.innerHTML = '';
            return;
        }

        const { pageNumber, totalPages, totalCount } = result;
        let html = '';

        // 上一页
        html += `
            <button class="page-btn" onclick="HistoryManager.loadHistory(${pageNumber - 1})"
                ${pageNumber <= 1 ? 'disabled' : ''}>
                « 上一页
            </button>
        `;

        // 页码
        const startPage = Math.max(1, pageNumber - 2);
        const endPage = Math.min(totalPages, pageNumber + 2);

        if (startPage > 1) {
            html += `<button class="page-btn" onclick="HistoryManager.loadHistory(1)">1</button>`;
            if (startPage > 2) {
                html += `<span class="page-info">...</span>`;
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            html += `
                <button class="page-btn ${i === pageNumber ? 'active' : ''}"
                    onclick="HistoryManager.loadHistory(${i})">
                    ${i}
                </button>
            `;
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                html += `<span class="page-info">...</span>`;
            }
            html += `<button class="page-btn" onclick="HistoryManager.loadHistory(${totalPages})">${totalPages}</button>`;
        }

        // 下一页
        html += `
            <button class="page-btn" onclick="HistoryManager.loadHistory(${pageNumber + 1})"
                ${pageNumber >= totalPages ? 'disabled' : ''}>
                下一页 »
            </button>
        `;

        html += `<span class="page-info">共 ${totalCount} 条记录</span>`;

        container.innerHTML = html;
    },

    // 渲染统计信息
    renderStats(stats) {
        document.getElementById('statTotal').textContent = stats.totalCount || 0;
        document.getElementById('statPrice').textContent = stats.priceAlertCount || 0;
        document.getElementById('statEma').textContent = stats.emaCrossCount || 0;
        document.getElementById('statsCards').style.display = 'grid';
    },

    // 显示详情模态框
    showDetailModal(item) {
        const modal = document.getElementById('detailModal');
        const content = document.getElementById('detailContent');

        const typeText = item.type === 0 ? '💰 价格告警' : '📊 EMA穿越';
        const time = new Date(item.alertTime).toLocaleString('zh-CN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });

        let detailsHtml = '';
        if (item.details) {
            try {
                const details = JSON.parse(item.details);
                if (item.type === 0) {
                    detailsHtml = `
                        <div class="detail-row">
                            <div class="detail-label">目标价格:</div>
                            <div class="detail-value">${details.targetPrice?.toFixed(4) || 'N/A'}</div>
                        </div>
                        <div class="detail-row">
                            <div class="detail-label">当前价格:</div>
                            <div class="detail-value">${details.currentPrice?.toFixed(4) || 'N/A'}</div>
                        </div>
                        <div class="detail-row">
                            <div class="detail-label">触发方向:</div>
                            <div class="detail-value">${details.direction === 'Above' ? '上穿 ⬆️' : '下穿 ⬇️'}</div>
                        </div>
                    `;
                } else {
                    detailsHtml = `
                        <div class="detail-row">
                            <div class="detail-label">K线周期:</div>
                            <div class="detail-value">${details.timeFrame || 'N/A'}</div>
                        </div>
                        <div class="detail-row">
                            <div class="detail-label">EMA周期:</div>
                            <div class="detail-value">EMA${details.emaPeriod || 'N/A'}</div>
                        </div>
                        <div class="detail-row">
                            <div class="detail-label">EMA值:</div>
                            <div class="detail-value">${details.emaValue?.toFixed(4) || 'N/A'}</div>
                        </div>
                        <div class="detail-row">
                            <div class="detail-label">收盘价:</div>
                            <div class="detail-value">${details.closePrice?.toFixed(4) || 'N/A'}</div>
                        </div>
                        <div class="detail-row">
                            <div class="detail-label">穿越类型:</div>
                            <div class="detail-value">${details.crossType === 'CrossAbove' ? '上穿 ⬆️' : '下穿 ⬇️'}</div>
                        </div>
                    `;
                }
            } catch (e) {
                console.error('解析详情失败:', e);
            }
        }

        content.innerHTML = `
            <div class="detail-row">
                <div class="detail-label">告警类型:</div>
                <div class="detail-value">${typeText}</div>
            </div>
            <div class="detail-row">
                <div class="detail-label">品种:</div>
                <div class="detail-value">${item.symbol}</div>
            </div>
            <div class="detail-row">
                <div class="detail-label">触发时间:</div>
                <div class="detail-value">${time}</div>
            </div>
            ${detailsHtml}
            <div class="detail-row">
                <div class="detail-label">消息内容:</div>
            </div>
            <div class="detail-message">${this.escapeHtml(item.message)}</div>
        `;

        modal.style.display = 'flex';
    },

    // 关闭详情模态框
    closeDetailModal() {
        document.getElementById('detailModal').style.display = 'none';
    },

    // HTML转义
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
};

// 点击模态框外部关闭
window.onclick = function(event) {
    const modal = document.getElementById('detailModal');
    if (event.target === modal) {
        HistoryUI.closeDetailModal();
    }
};
