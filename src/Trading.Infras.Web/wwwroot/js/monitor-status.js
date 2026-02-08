// 监控状态页面 JavaScript

let isLoading = false;

async function loadData() {
    if (isLoading) return;

    isLoading = true;
    const btn = document.getElementById('refreshBtn');
    btn.classList.add('loading');
    btn.disabled = true;

    try {
        const response = await fetch('/api/monitorstatus');
        if (!response.ok) {
            throw new Error('获取数据失败');
        }

        const data = await response.json();
        renderTable(data);
        updateCounts(data);
    } catch (error) {
        console.error('加载数据失败:', error);
        document.getElementById('statusContainer').innerHTML = `
            <div class="empty-state">
                <p>❌ 加载数据失败: ${error.message}</p>
                <button class="btn btn-primary" onclick="loadData()" style="margin-top: 15px;">重试</button>
            </div>
        `;
    } finally {
        isLoading = false;
        btn.classList.remove('loading');
        btn.disabled = false;
    }
}

function updateCounts(data) {
    const priceCount = data.filter(d => d.type === 'PriceMonitor').length;
    const emaCount = data.filter(d => d.type === 'EmaMonitor').length;

    document.getElementById('totalCount').textContent = data.length;
    document.getElementById('priceCount').textContent = priceCount;
    document.getElementById('emaCount').textContent = emaCount;
}

function renderTable(data) {
    if (!data || data.length === 0) {
        document.getElementById('statusContainer').innerHTML = `
            <div class="empty-state">
                <p>📭 暂无有效的监控规则</p>
                <p style="margin-top: 10px; font-size: 14px;">
                    请先在 <a href="index.html">价格监控</a> 或 <a href="ema-config.html">EMA监控</a> 页面添加监控规则
                </p>
            </div>
        `;
        return;
    }

    const html = `
        <table class="status-table">
            <thead>
                <tr>
                    <th>类型</th>
                    <th>品种</th>
                    <th>名称</th>
                    <th>周期</th>
                    <th>当前价格</th>
                    <th>目标价格/EMA</th>
                    <th>距离</th>
                    <th>状态</th>
                </tr>
            </thead>
            <tbody>
                ${data.map(item => renderRow(item)).join('')}
            </tbody>
        </table>
    `;

    document.getElementById('statusContainer').innerHTML = html;
}

function renderRow(item) {
    const typeClass = item.type === 'PriceMonitor' ? 'price' : 'ema';
    const typeLabel = item.type === 'PriceMonitor' ? '💰 价格' : '📈 EMA';
    const distanceClass = item.distance >= 0 ? 'distance-positive' : 'distance-negative';
    const distanceSign = item.distance >= 0 ? '+' : '';

    return `
        <tr>
            <td><span class="type-badge ${typeClass}">${typeLabel}</span></td>
            <td class="symbol-cell">${item.symbol}</td>
            <td>${item.name}</td>
            <td class="timeframe-cell">${item.timeFrame || '-'}</td>
            <td class="price-cell">${formatPrice(item.currentPrice)}</td>
            <td class="price-cell">${formatPrice(item.targetPrice)}</td>
            <td class="${distanceClass}">${distanceSign}${item.distance.toFixed(2)}%</td>
            <td>${item.direction}</td>
        </tr>
    `;
}

function formatPrice(price) {
    if (!price) return '-';
    // 根据价格大小决定小数位数
    if (price >= 1000) {
        return price.toFixed(2);
    } else if (price >= 1) {
        return price.toFixed(4);
    } else {
        return price.toFixed(5);
    }
}

function refreshData() {
    loadData();
}

// 页面加载时获取数据
document.addEventListener('DOMContentLoaded', loadData);

// 每60秒自动刷新（减少对数据源的请求压力）
setInterval(loadData, 60000);
