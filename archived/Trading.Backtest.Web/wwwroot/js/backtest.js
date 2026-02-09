// 回测执行相关功能

// 安全地格式化数字，防止 undefined.toFixed 错误
function formatNumber(value, decimals = 2) {
    if (value === null || value === undefined || isNaN(value)) {
        return '0.00';
    }
    return parseFloat(value).toFixed(decimals);
}

// 运行回测
async function runBacktest() {
    if (!currentStrategy) {
        alert('请先选择一个策略');
        return;
    }
    
    // 隐藏之前的错误消息
    document.getElementById('errorMessage').style.display = 'none';
    
    // 收集表单数据
    const emaList = document.getElementById('emaList').value
        .split(',')
        .map(s => parseInt(s.trim()))
        .filter(n => !isNaN(n));
    
    const request = {
        strategyName: currentStrategy.config.strategyName,
        symbol: document.getElementById('symbol').value,
        csvFilter: document.getElementById('csvFilter').value,
        baseEma: parseInt(document.getElementById('baseEma').value),
        atrPeriod: parseInt(document.getElementById('atrPeriod').value),
        emaList: emaList,
        maxBodyPercentage: parseFloat(document.getElementById('maxBodyPercentage').value),
        minLongerWickPercentage: parseFloat(document.getElementById('minLongerWickPercentage').value),
        maxShorterWickPercentage: parseFloat(document.getElementById('maxShorterWickPercentage').value),
        minLowerWickAtrRatio: parseFloat(document.getElementById('minLowerWickAtrRatio').value),
        threshold: parseFloat(document.getElementById('threshold').value),
        nearEmaThreshold: parseFloat(document.getElementById('nearEmaThreshold').value),
        stopLossAtrRatio: parseFloat(document.getElementById('stopLossAtrRatio').value),
        riskRewardRatio: parseFloat(document.getElementById('riskRewardRatio').value),
        noTradingHoursLimit: document.getElementById('noTradingHoursLimit').checked,
        startTradingHour: parseInt(document.getElementById('startTradingHour').value),
        endTradingHour: parseInt(document.getElementById('endTradingHour').value),
        requirePinBarDirectionMatch: document.getElementById('requirePinBarDirectionMatch').checked,
        // 扁平化的 Account 参数
        initialCapital: parseFloat(document.getElementById('initialCapital').value),
        leverage: parseFloat(document.getElementById('leverage').value),
        maxLossPerTradePercent: parseFloat(document.getElementById('maxLossPerTradePercent').value),
        maxDailyLossPercent: parseFloat(document.getElementById('maxDailyLossPercent').value)
    };
    
    console.log('发送回测请求:', request);
    
    try {
        const response = await fetch('/api/backtest/run', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(request)
        });
        
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`HTTP ${response.status}: ${errorText}`);
        }
        
        const data = await response.json();
        console.log('回测结果:', data);
        displayResults(data);
    } catch (error) {
        console.error('回测失败:', error);
        const errorDiv = document.getElementById('errorMessage');
        errorDiv.textContent = '回测失败: ' + error.message;
        errorDiv.style.display = 'block';
    }
}

// 显示结果
function displayResults(data) {
    console.log('收到的完整数据:', data);
    
    // 兼容大小写
    const result = data.result || data.Result;
    const account = data.account || data.Account;
    
    if (!result) {
        console.error('没有找到 result 数据');
        const errorDiv = document.getElementById('errorMessage');
        errorDiv.textContent = '回测返回数据格式错误';
        errorDiv.style.display = 'block';
        return;
    }
    
    const metrics = result.overallMetrics || result.OverallMetrics;
    
    if (!metrics) {
        console.error('没有找到 metrics 数据');
        const errorDiv = document.getElementById('errorMessage');
        errorDiv.textContent = '回测返回的指标数据为空';
        errorDiv.style.display = 'block';
        return;
    }
    
    console.log('Metrics:', metrics);
    console.log('Account:', account);
    
    // 显示关键指标
    const metricsGrid = document.getElementById('metricsGrid');
    const finalCapital = account.initialCapital + (metrics.totalProfit || 0);
    
    const formatDateRange = (start, end) => {
        if (!start || !end) return '';
        if (start === end) return `<div class="metric-date">${start}</div>`;
        return `<div class="metric-date">${start} ~ ${end}</div>`;
    };
    
    metricsGrid.innerHTML = `
        <div class="metric-card">
            <div class="metric-label">总交易数</div>
            <div class="metric-value">${metrics.totalTrades || 0}</div>
        </div>
        <div class="metric-card">
            <div class="metric-label">胜率</div>
            <div class="metric-value">${formatNumber(metrics.winRate)}%</div>
        </div>
        <div class="metric-card">
            <div class="metric-label">总收益</div>
            <div class="metric-value ${(metrics.totalProfit || 0) >= 0 ? 'positive' : 'negative'}">
                $${formatNumber(metrics.totalProfit)}
            </div>
        </div>
        <div class="metric-card">
            <div class="metric-label">收益率</div>
            <div class="metric-value ${(metrics.totalReturnRate || 0) >= 0 ? 'positive' : 'negative'}">
                ${formatNumber(metrics.totalReturnRate)}%
            </div>
        </div>
        <div class="metric-card">
            <div class="metric-label">最终资金</div>
            <div class="metric-value">$${formatNumber(finalCapital)}</div>
        </div>
        <div class="metric-card">
            <div class="metric-label">盈亏比</div>
            <div class="metric-value">${formatNumber(metrics.profitFactor)}</div>
        </div>
        <div class="metric-card">
            <div class="metric-label">最大回撤</div>
            <div class="metric-value negative">$${formatNumber(metrics.maxDrawdown)}</div>
            ${formatDateRange(metrics.maxDrawdownStartTime, metrics.maxDrawdownEndTime)}
        </div>
        <div class="metric-card">
            <div class="metric-label">最大连续盈利</div>
            <div class="metric-value">${metrics.maxConsecutiveWins || 0}</div>
            ${formatDateRange(metrics.maxConsecutiveWinsStartTime, metrics.maxConsecutiveWinsEndTime)}
        </div>
        <div class="metric-card">
            <div class="metric-label">最大连续亏损</div>
            <div class="metric-value">${metrics.maxConsecutiveLosses || 0}</div>
            ${formatDateRange(metrics.maxConsecutiveLossesStartTime, metrics.maxConsecutiveLossesEndTime)}
        </div>
    `;
    
    // 绘制收益曲线
    drawEquityChart(result.equityCurve || result.EquityCurve);
    
    // 绘制年度收益
    drawYearlyChart(result.yearlyMetrics || result.YearlyMetrics);
    
    // 绘制月度收益
    drawMonthlyChart(result.monthlyMetrics || result.MonthlyMetrics);
    
    // 显示交易记录
    displayTrades(result.allTrades || result.AllTrades);
    
    // 显示结果区域
    document.getElementById('results').classList.add('show');
}
