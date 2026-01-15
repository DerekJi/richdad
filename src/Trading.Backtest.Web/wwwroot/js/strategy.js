// 策略管理相关功能
let currentStrategy = null;

// 加载策略列表
async function loadStrategies() {
    try {
        const response = await fetch('/api/backtest/strategies');
        const strategies = await response.json();
        
        const list = document.getElementById('strategyList');
        
        if (!strategies || strategies.length === 0) {
            list.innerHTML = '<li class="strategy-item">暂无策略</li>';
            return;
        }
        
        list.innerHTML = strategies.map((s, i) => 
            `<li class="strategy-item ${i === 0 ? 'active' : ''}" onclick="loadStrategy('${s.name}')">${s.name}</li>`
        ).join('');
        
        // 默认加载第一个策略
        if (strategies.length > 0) {
            loadStrategy(strategies[0].name);
        }
    } catch (error) {
        document.getElementById('strategyList').innerHTML = '<li class="strategy-item">加载失败</li>';
        console.error('加载策略失败:', error);
    }
}

// 加载策略配置
async function loadStrategy(name) {
    try {
        const response = await fetch(`/api/backtest/strategies/${name}`);
        const data = await response.json();
        currentStrategy = data;
        
        const config = data.config;
        const account = data.account;
        const indicators = data.indicators;
        
        // 更新活动状态
        document.querySelectorAll('.strategy-item').forEach(item => {
            item.classList.toggle('active', item.textContent === name);
        });
        
        // 填充表单 - 基础配置
        document.getElementById('symbol').value = config.symbol || '';
        document.getElementById('csvFilter').value = config.csvFilter || '';
        document.getElementById('contractSize').value = config.contractSize || 100;
        
        // Pin Bar 参数
        document.getElementById('maxBodyPercentage').value = config.maxBodyPercentage || 0;
        document.getElementById('minLongerWickPercentage').value = config.minLongerWickPercentage || 0;
        document.getElementById('maxShorterWickPercentage').value = config.maxShorterWickPercentage || 0;
        document.getElementById('minLowerWickAtrRatio').value = config.minLowerWickAtrRatio || 0;
        document.getElementById('threshold').value = config.threshold || 0;
        
        // EMA 参数
        document.getElementById('baseEma').value = config.baseEma || 200;
        document.getElementById('emaList').value = (config.emaList || []).join(', ');
        document.getElementById('nearEmaThreshold').value = config.nearEmaThreshold || 0;
        
        // 风险管理
        document.getElementById('atrPeriod').value = config.atrPeriod || 14;
        document.getElementById('stopLossAtrRatio').value = config.stopLossAtrRatio || 1.0;
        document.getElementById('riskRewardRatio').value = config.riskRewardRatio || 1.5;
        
        // 交易时段
        document.getElementById('noTradingHoursLimit').checked = config.noTradingHoursLimit || false;
        document.getElementById('startTradingHour').value = config.startTradingHour || 0;
        document.getElementById('endTradingHour').value = config.endTradingHour || 23;
        
        // 高级参数
        document.getElementById('requirePinBarDirectionMatch').checked = config.requirePinBarDirectionMatch || false;
        
        // 指标设置
        document.getElementById('globalBaseEma').value = indicators?.baseEma || 200;
        document.getElementById('globalAtrPeriod').value = indicators?.atrPeriod || 14;
        document.getElementById('emaFastPeriod').value = indicators?.emaFastPeriod || 20;
        document.getElementById('emaSlowPeriod').value = indicators?.emaSlowPeriod || 60;
        
        // 账户设置
        document.getElementById('initialCapital').value = account?.initialCapital || 100000;
        document.getElementById('leverage').value = account?.leverage || 30;
        document.getElementById('maxLossPerTradePercent').value = account?.maxLossPerTradePercent || 0.5;
        document.getElementById('maxDailyLossPercent').value = account?.maxDailyLossPercent || 3.0;
        
    } catch (error) {
        console.error('加载策略配置失败:', error);
        alert('加载策略配置失败: ' + error.message);
    }
}

// 切换高级参数显示
function toggleAdvanced() {
    const advancedDiv = document.getElementById('advancedParams');
    const toggleText = document.getElementById('advancedToggleText');
    
    if (advancedDiv.style.display === 'none') {
        advancedDiv.style.display = 'block';
        toggleText.textContent = '隐藏';
    } else {
        advancedDiv.style.display = 'none';
        toggleText.textContent = '显示';
    }
}

// 切换指标设置显示
function toggleIndicators() {
    const indicatorsDiv = document.getElementById('indicatorsParams');
    const toggleText = document.getElementById('indicatorsToggleText');
    
    if (indicatorsDiv.style.display === 'none') {
        indicatorsDiv.style.display = 'block';
        toggleText.textContent = '隐藏';
    } else {
        indicatorsDiv.style.display = 'none';
        toggleText.textContent = '显示';
    }
}

// 切换账户设置显示
function toggleAccount() {
    const accountDiv = document.getElementById('accountParams');
    const toggleText = document.getElementById('accountToggleText');
    
    if (accountDiv.style.display === 'none') {
        accountDiv.style.display = 'block';
        toggleText.textContent = '隐藏';
    } else {
        accountDiv.style.display = 'none';
        toggleText.textContent = '显示';
    }
}
