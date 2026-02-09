// 交易记录显示和排序相关功能
let allTrades = [];
let currentPage = 1;
let sortField = null;
let sortDirection = 'asc';

// 显示交易记录
function displayTrades(trades) {
    if (!trades || trades.length === 0) {
        console.warn('交易记录数据为空');
        return;
    }

    allTrades = trades;
    currentPage = 1;
    updateTradesDisplay();
}

// 更新交易记录显示（分页）
function updateTradesDisplay() {
    const pageSize = parseInt(document.getElementById('pageSize').value);
    const totalPages = Math.ceil(allTrades.length / pageSize);
    const startIndex = (currentPage - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const pageTrades = allTrades.slice(startIndex, endIndex);

    const tbody = document.getElementById('tradesTable');

    tbody.innerHTML = pageTrades.map(t => {
        const profitLoss = t.profitLoss || t.ProfitLoss || 0;
        const returnRate = t.returnRate || t.ReturnRate || 0;
        const lots = t.lots ?? t.Lots ?? 0.01;

        return `
        <tr>
            <td>${t.openTime || t.OpenTime}</td>
            <td>${t.direction || t.Direction}</td>
            <td>${formatNumber(lots, 2)}</td>
            <td>${formatNumber(t.openPrice || t.OpenPrice, 5)}</td>
            <td>${formatNumber(t.stopLoss || t.StopLoss, 5)}</td>
            <td>${formatNumber(t.stopLossPips || t.StopLossPips)}</td>
            <td>${formatNumber(t.takeProfit || t.TakeProfit, 5)}</td>
            <td>${formatNumber(t.takeProfitPips || t.TakeProfitPips)}</td>
            <td>${(t.closeTime || t.CloseTime) || '-'}</td>
            <td>${(t.closePrice || t.ClosePrice) ? formatNumber(t.closePrice || t.ClosePrice, 5) : '-'}</td>
            <td>${t.closeReason || t.CloseReason || '-'}</td>
            <td class="${profitLoss >= 0 ? 'positive' : 'negative'}">
                ${profitLoss >= 0 ? '+' : ''}${formatNumber(profitLoss)}
            </td>
            <td class="${returnRate >= 0 ? 'positive' : 'negative'}">
                ${returnRate >= 0 ? '+' : ''}${formatNumber(returnRate)}%
            </td>
        </tr>
    `}).join('');

    // 更新分页控件
    updatePagination(totalPages);
}

// 更新分页控件
function updatePagination(totalPages) {
    const pagination = document.getElementById('pagination');

    if (totalPages <= 1) {
        pagination.innerHTML = '';
        return;
    }

    let html = '<div style="display: flex; gap: 5px;">';

    // 上一页
    if (currentPage > 1) {
        html += `<button class="btn btn-secondary" style="padding: 4px 8px;" onclick="changePage(${currentPage - 1})">上一页</button>`;
    }

    // 页码
    const maxButtons = 5;
    let startPage = Math.max(1, currentPage - Math.floor(maxButtons / 2));
    let endPage = Math.min(totalPages, startPage + maxButtons - 1);

    if (endPage - startPage < maxButtons - 1) {
        startPage = Math.max(1, endPage - maxButtons + 1);
    }

    if (startPage > 1) {
        html += `<button class="btn btn-secondary" style="padding: 4px 8px;" onclick="changePage(1)">1</button>`;
        if (startPage > 2) {
            html += '<span style="padding: 4px 8px;">...</span>';
        }
    }

    for (let i = startPage; i <= endPage; i++) {
        const isActive = i === currentPage ? 'btn-primary' : 'btn-secondary';
        html += `<button class="btn ${isActive}" style="padding: 4px 8px;" onclick="changePage(${i})">${i}</button>`;
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
            html += '<span style="padding: 4px 8px;">...</span>';
        }
        html += `<button class="btn btn-secondary" style="padding: 4px 8px;" onclick="changePage(${totalPages})">${totalPages}</button>`;
    }

    // 下一页
    if (currentPage < totalPages) {
        html += `<button class="btn btn-secondary" style="padding: 4px 8px;" onclick="changePage(${currentPage + 1})">下一页</button>`;
    }

    html += `<span style="padding: 4px 8px;">共 ${allTrades.length} 条记录</span>`;
    html += '</div>';

    pagination.innerHTML = html;
}

// 切换页码
function changePage(page) {
    currentPage = page;
    updateTradesDisplay();
}

// 排序交易记录
function sortTrades(field) {
    // 如果点击同一个字段，切换排序方向
    if (sortField === field) {
        sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
        sortField = field;
        sortDirection = 'asc';
    }

    // 排序数据
    allTrades.sort((a, b) => {
        let aVal = a[field] || a[field.charAt(0).toUpperCase() + field.slice(1)] || '';
        let bVal = b[field] || b[field.charAt(0).toUpperCase() + field.slice(1)] || '';

        // 转换为数字或字符串比较
        if (typeof aVal === 'string' && !isNaN(Date.parse(aVal))) {
            aVal = new Date(aVal);
            bVal = new Date(bVal);
        }

        if (aVal < bVal) return sortDirection === 'asc' ? -1 : 1;
        if (aVal > bVal) return sortDirection === 'asc' ? 1 : -1;
        return 0;
    });

    // 更新排序指示器
    document.querySelectorAll('.sort-indicator').forEach(el => {
        el.textContent = '';
    });

    const th = event.target.closest('th');
    if (th) {
        const indicator = th.querySelector('.sort-indicator');
        if (indicator) {
            indicator.textContent = sortDirection === 'asc' ? ' ▲' : ' ▼';
        }
    }

    // 重置到第一页并刷新显示
    currentPage = 1;
    updateTradesDisplay();
}
