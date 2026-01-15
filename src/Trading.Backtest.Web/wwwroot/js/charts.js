// 图表绘制相关功能
let equityChart = null;
let monthlyChart = null;
let yearlyChart = null;

// 绘制收益曲线
function drawEquityChart(equityCurve) {
    if (!equityCurve || equityCurve.length === 0) {
        console.warn('收益曲线数据为空');
        return;
    }
    
    const ctx = document.getElementById('equityChart');
    
    if (equityChart) {
        equityChart.destroy();
    }
    
    equityChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: equityCurve.map(p => p.time || p.Time),
            datasets: [{
                label: '累计收益 (USD)',
                data: equityCurve.map(p => p.cumulativeProfit || p.CumulativeProfit),
                borderColor: '#667eea',
                backgroundColor: 'rgba(102, 126, 234, 0.1)',
                fill: true,
                tension: 0.4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: { display: true }
            },
            scales: {
                y: {
                    beginAtZero: true
                }
            }
        }
    });
}

// 绘制年度收益
function drawYearlyChart(yearlyMetrics) {
    if (!yearlyMetrics || yearlyMetrics.length === 0) {
        console.warn('年度收益数据为空');
        return;
    }
    
    console.log('年度收益数据:', yearlyMetrics);
    
    const ctx = document.getElementById('yearlyChart');
    if (!ctx) {
        console.error('找不到 yearlyChart canvas 元素');
        return;
    }
    
    if (yearlyChart) {
        yearlyChart.destroy();
    }
    
    const labels = yearlyMetrics.map(y => y.period || y.Period);
    const profits = yearlyMetrics.map(y => y.profitLoss || y.ProfitLoss);
    
    console.log('年度标签:', labels);
    console.log('年度收益:', profits);
    
    try {
        yearlyChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: '年度收益 (USD)',
                    data: profits,
                    backgroundColor: profits.map(p => p >= 0 ? 'rgba(40, 167, 69, 0.7)' : 'rgba(220, 53, 69, 0.7)'),
                    borderColor: profits.map(p => p >= 0 ? '#28a745' : '#dc3545'),
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: { display: true }
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
        console.log('年度收益图表创建成功');
    } catch (error) {
        console.error('创建年度收益图表失败:', error);
    }
}

// 绘制月度收益
function drawMonthlyChart(monthlyMetrics) {
    if (!monthlyMetrics || monthlyMetrics.length === 0) {
        console.warn('月度收益数据为空');
        return;
    }
    
    console.log('月度收益数据:', monthlyMetrics);
    
    const ctx = document.getElementById('monthlyChart');
    if (!ctx) {
        console.error('找不到 monthlyChart canvas 元素');
        return;
    }
    
    if (monthlyChart) {
        monthlyChart.destroy();
    }
    
    // 正确的属性名：Period 和 ProfitLoss
    const labels = monthlyMetrics.map(m => m.period || m.Period);
    const profits = monthlyMetrics.map(m => m.profitLoss || m.ProfitLoss);
    
    console.log('月度标签:', labels);
    console.log('月度收益:', profits);
    
    try {
        monthlyChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: '月度收益 (USD)',
                    data: profits,
                    backgroundColor: profits.map(p => p >= 0 ? 'rgba(40, 167, 69, 0.7)' : 'rgba(220, 53, 69, 0.7)'),
                    borderColor: profits.map(p => p >= 0 ? '#28a745' : '#dc3545'),
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: { display: true }
                },
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
        console.log('月度收益图表创建成功');
    } catch (error) {
        console.error('创建月度收益图表失败:', error);
    }
}
