// PinBar 配置管理
class PinBarConfigManager {
    constructor() {
        this.currentConfig = null;
    }

    async init() {
        await this.loadConfig();
        await this.loadSignals();
        this.setupEventListeners();
    }

    setupEventListeners() {
        // 启用开关
        const enabledSwitch = document.getElementById('enabledSwitch');
        if (enabledSwitch) {
            enabledSwitch.addEventListener('change', (e) => {
                this.toggleMonitoring(e.target.checked);
            });
        }

        // 品种输入
        const symbolInput = document.getElementById('symbolInput');
        if (symbolInput) {
            symbolInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    this.addSymbol();
                }
            });
        }

        // EMA周期输入
        const emaPeriodInput = document.getElementById('emaPeriodInput');
        if (emaPeriodInput) {
            emaPeriodInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    e.preventDefault();
                    this.addEmaPeriod();
                }
            });
        }
    }

    async loadConfig() {
        try {
            const response = await fetch('/api/PinBarMonitor/config');
            if (!response.ok) {
                if (response.status === 404) {
                    // 配置不存在，使用默认配置
                    this.displayDefaultConfig();
                    return;
                }
                throw new Error('获取配置失败');
            }

            this.currentConfig = await response.json();
            this.displayConfig(this.currentConfig);
        } catch (error) {
            console.error('加载配置失败:', error);
            this.showError('加载配置失败: ' + error.message);
        }
    }

    displayDefaultConfig() {
        const defaultConfig = {
            enabled: false,
            symbols: ['XAUUSD', 'XAGUSD'],
            timeFrames: ['M5', 'M15', 'H1'],
            strategySettings: {
                emaList: [20, 60, 120],
                minWickRatio: 0.6,
                maxBodyRatio: 0.3,
                maxOppositeWickRatio: 0.3,
                minWickAtrMultiplier: 1.0,
                atrPeriod: 14,
                requireEmaAlignment: true,
                requireVolumeConfirm: false,
                minVolumeMultiplier: 1.2,
                volumeLookbackPeriod: 10,
                stopLossStrategy: 'PinbarEndPlusAtr',
                stopLossAtrMultiplier: 0.5,
                takeProfitRatio: 2.0,
                minRiskRewardRatio: 1.5
            },
            enableTelegramNotification: true,
            includeChart: false
        };
        this.currentConfig = defaultConfig;
        this.displayConfig(defaultConfig);
    }

    displayConfig(config) {
        // 启用状态
        const enabledSwitch = document.getElementById('enabledSwitch');
        if (enabledSwitch) enabledSwitch.checked = config.enabled;

        // 基础配置
        this.updateSymbols(config.symbols || []);
        this.updateTimeFrames(config.timeFrames || []);

        // EMA周期
        const s = config.strategySettings;
        this.updateEmaPeriods(s.emaList || []);

        // PinBar形态参数
        this.setInputValue('minWickRatio', s.minWickRatio);
        this.setInputValue('maxBodyRatio', s.maxBodyRatio);
        this.setInputValue('maxOppositeWickRatio', s.maxOppositeWickRatio);
        this.setInputValue('minWickAtrMultiplier', s.minWickAtrMultiplier);
        this.setInputValue('atrPeriod', s.atrPeriod);

        // 开仓过滤条件
        this.setCheckboxValue('requireEmaAlignment', s.requireEmaAlignment);
        this.setCheckboxValue('requireVolumeConfirm', s.requireVolumeConfirm);
        this.setInputValue('minVolumeMultiplier', s.minVolumeMultiplier);
        this.setInputValue('volumeLookbackPeriod', s.volumeLookbackPeriod);

        // 风险管理参数
        this.setSelectValue('stopLossStrategy', s.stopLossStrategy);
        this.setInputValue('stopLossAtrMultiplier', s.stopLossAtrMultiplier);
        this.setInputValue('takeProfitRatio', s.takeProfitRatio);
        this.setInputValue('minRiskRewardRatio', s.minRiskRewardRatio);

        // Telegram通知
        this.setCheckboxValue('enableTelegramNotification', config.enableTelegramNotification);
        this.setCheckboxValue('includeChart', config.includeChart);

        // 配置信息
        if (config.updatedAt) {
            const updatedAtEl = document.getElementById('updatedAt');
            if (updatedAtEl) updatedAtEl.textContent = new Date(config.updatedAt).toLocaleString('zh-CN');
        }
        if (config.updatedBy) {
            const updatedByEl = document.getElementById('updatedBy');
            if (updatedByEl) updatedByEl.textContent = config.updatedBy;
        }
    }

    setInputValue(id, value) {
        const element = document.getElementById(id);
        if (element && value !== undefined && value !== null) {
            element.value = value;
        }
    }

    setCheckboxValue(id, value) {
        const element = document.getElementById(id);
        if (element && value !== undefined && value !== null) {
            element.checked = !!value; // 确保转换为布尔值
        }
    }

    setSelectValue(id, value) {
        const element = document.getElementById(id);
        if (element && value) {
            element.value = value;
        }
    }

    updateSymbols(symbols) {
        const container = document.getElementById('symbolsTags');
        if (!container) return;

        container.innerHTML = symbols.map(s =>
            `<span class="tag">${s} <span class="remove" onclick="window.pinBarConfig.removeSymbol('${s}')">&times;</span></span>`
        ).join('');
    }

    updateTimeFrames(timeFrames) {
        document.querySelectorAll('.timeframe-check').forEach(cb => {
            cb.checked = timeFrames.includes(cb.value);
        });
    }

    updateEmaPeriods(periods) {
        const container = document.getElementById('emaPeriodsTags');
        if (!container) return;

        container.innerHTML = periods.map(e =>
            `<span class="tag">${e} <span class="remove" onclick="window.pinBarConfig.removeEmaPeriod(${e})">&times;</span></span>`
        ).join('');
    }

    addSymbol() {
        const input = document.getElementById('symbolInput');
        if (!input || !input.value.trim()) return;

        const symbol = input.value.trim().toUpperCase();
        if (!this.currentConfig.symbols) this.currentConfig.symbols = [];

        if (!this.currentConfig.symbols.includes(symbol)) {
            this.currentConfig.symbols.push(symbol);
            this.updateSymbols(this.currentConfig.symbols);
        }
        input.value = '';
    }

    removeSymbol(symbol) {
        if (!this.currentConfig.symbols) return;

        const index = this.currentConfig.symbols.indexOf(symbol);
        if (index > -1) {
            this.currentConfig.symbols.splice(index, 1);
            this.updateSymbols(this.currentConfig.symbols);
        }
    }

    addEmaPeriod() {
        const input = document.getElementById('emaPeriodInput');
        if (!input || !input.value) return;

        const period = parseInt(input.value);
        if (isNaN(period) || period <= 0) return;

        if (!this.currentConfig.strategySettings.emaList) {
            this.currentConfig.strategySettings.emaList = [];
        }

        if (!this.currentConfig.strategySettings.emaList.includes(period)) {
            this.currentConfig.strategySettings.emaList.push(period);
            this.updateEmaPeriods(this.currentConfig.strategySettings.emaList);
        }
        input.value = '';
    }

    removeEmaPeriod(period) {
        if (!this.currentConfig.strategySettings.emaList) return;

        const index = this.currentConfig.strategySettings.emaList.indexOf(period);
        if (index > -1) {
            this.currentConfig.strategySettings.emaList.splice(index, 1);
            this.updateEmaPeriods(this.currentConfig.strategySettings.emaList);
        }
    }

    async toggleMonitoring(enabled) {
        try {
            const response = await fetch('/api/PinBarMonitor/toggle', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ enabled })
            });

            if (!response.ok) throw new Error('切换状态失败');

            this.showSuccess(enabled ? '监控已启用' : '监控已停止');
        } catch (error) {
            console.error('切换监控状态失败:', error);
            this.showError('操作失败: ' + error.message);
            // 恢复开关状态
            const enabledSwitch = document.getElementById('enabledSwitch');
            if (enabledSwitch) enabledSwitch.checked = !enabled;
        }
    }

    async saveConfig() {
        try {
            const config = this.collectConfigFromForm();

            // 调试：打印EMA列表
            console.log('Saving config with emaList:', config.strategySettings.emaList);
            console.log('Full config:', JSON.stringify(config, null, 2));

            const response = await fetch('/api/PinBarMonitor/config', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(config)
            });

            if (!response.ok) throw new Error('保存配置失败');

            this.currentConfig = await response.json();

            // 调试：打印返回的EMA列表
            console.log('Returned emaList:', this.currentConfig.strategySettings.emaList);

            this.showSuccess('✓ 配置已保存');
            // 不需要重新loadConfig，避免重复加载，只需更新显示即可
            this.displayConfig(this.currentConfig);
        } catch (error) {
            console.error('保存配置失败:', error);
            this.showError('保存失败: ' + error.message);
        }
    }

    collectConfigFromForm() {
        // 获取时间框架
        const timeFrames = Array.from(document.querySelectorAll('.timeframe-check:checked'))
            .map(cb => cb.value);

        // 确保所有checkbox值都被正确读取
        const requireEmaAlignment = document.getElementById('requireEmaAlignment');
        const requireVolumeConfirm = document.getElementById('requireVolumeConfirm');
        const enableTelegramNotification = document.getElementById('enableTelegramNotification');
        const includeChart = document.getElementById('includeChart');

        // 从DOM中读取当前的EMA列表，避免重复
        const emaPeriodsContainer = document.getElementById('emaPeriodsTags');
        const emaList = emaPeriodsContainer
            ? Array.from(emaPeriodsContainer.querySelectorAll('.tag')).map(tag => {
                const text = tag.textContent.replace('×', '').trim();
                return parseInt(text);
            }).filter(n => !isNaN(n))
            : [20, 50, 100];

        // 构建完整的配置对象
        return {
            id: "default",
            enabled: document.getElementById('enabledSwitch')?.checked || false,
            symbols: this.currentConfig?.symbols || [],
            timeFrames: timeFrames,
            historyMultiplier: 3,
            strategySettings: {
                strategyName: "PinBar",
                baseEma: 200,
                emaList: emaList,
                nearEmaThreshold: 0.001,
                threshold: 0.0001,
                minLowerWickAtrRatio: parseFloat(document.getElementById('minWickAtrMultiplier')?.value) || 1.0,
                maxBodyPercentage: (parseFloat(document.getElementById('maxBodyRatio')?.value) || 0.3) * 100,
                minLongerWickPercentage: (parseFloat(document.getElementById('minWickRatio')?.value) || 0.6) * 100,
                maxShorterWickPercentage: (parseFloat(document.getElementById('maxOppositeWickRatio')?.value) || 0.3) * 100,
                requirePinBarDirectionMatch: true,
                requireEmaAlignment: requireEmaAlignment ? requireEmaAlignment.checked : true,
                minAdx: 0,
                lowAdxRiskRewardRatio: 0,
                riskRewardRatio: parseFloat(document.getElementById('takeProfitRatio')?.value) || 2.0,
                noTradingHoursLimit: true,
                startTradingHour: 0,
                endTradingHour: 23,
                noTradeHours: null,
                stopLossStrategy: document.getElementById('stopLossStrategy')?.value || 'PinbarEndPlusAtr',
                stopLossAtrRatio: parseFloat(document.getElementById('stopLossAtrMultiplier')?.value) || 0.5,
                atrPeriod: parseInt(document.getElementById('atrPeriod')?.value) || 14,
                requireVolumeConfirm: requireVolumeConfirm ? requireVolumeConfirm.checked : false,
                minVolumeMultiplier: parseFloat(document.getElementById('minVolumeMultiplier')?.value) || 1.2,
                volumeLookbackPeriod: parseInt(document.getElementById('volumeLookbackPeriod')?.value) || 10
            },
            enableTelegramNotification: enableTelegramNotification ? enableTelegramNotification.checked : true,
            includeChart: includeChart ? includeChart.checked : false
        };
    }

    async loadSignals() {
        try {
            const response = await fetch('/api/PinBarMonitor/signals');
            if (!response.ok) {
                if (response.status === 404) {
                    this.displayNoSignals();
                    return;
                }
                throw new Error('获取信号失败');
            }

            const signals = await response.json();
            this.displaySignals(signals);
        } catch (error) {
            console.error('加载信号失败:', error);
            this.displayNoSignals('加载失败');
        }
    }

    displaySignals(signals) {
        const container = document.getElementById('recentSignals');
        if (!container) return;

        if (!signals || signals.length === 0) {
            this.displayNoSignals();
            return;
        }

        container.innerHTML = signals.slice(0, 10).map(signal => `
            <div class="signal-item signal-${signal.direction.toLowerCase()}">
                <div class="signal-header">
                    <span class="signal-symbol">${signal.symbol}</span>
                    <span class="signal-timeframe">${signal.timeFrame}</span>
                    <span class="signal-direction ${signal.direction === 'Long' ? 'long' : 'short'}">
                        ${signal.direction === 'Long' ? '📈 做多' : '📉 做空'}
                    </span>
                </div>
                <div class="signal-details">
                    <div class="signal-price">
                        <span class="label">开仓价:</span>
                        <span class="value">${signal.entryPrice?.toFixed(signal.symbol.includes('JPY') ? 3 : 5) || 'N/A'}</span>
                    </div>
                    <div class="signal-price">
                        <span class="label">止损:</span>
                        <span class="value">${signal.stopLoss?.toFixed(signal.symbol.includes('JPY') ? 3 : 5) || 'N/A'}</span>
                    </div>
                    <div class="signal-price">
                        <span class="label">止盈:</span>
                        <span class="value">${signal.takeProfit?.toFixed(signal.symbol.includes('JPY') ? 3 : 5) || 'N/A'}</span>
                    </div>
                    <div class="signal-time">
                        <span class="label">时间:</span>
                        <span class="value">${new Date(signal.signalTime).toLocaleString('zh-CN')}</span>
                    </div>
                </div>
            </div>
        `).join('');
    }

    displayNoSignals(message = '暂无信号') {
        const container = document.getElementById('recentSignals');
        if (!container) return;

        container.innerHTML = `<p class="no-data">${message}</p>`;
    }

    showSuccess(message) {
        this.showMessage(message, 'success');
    }

    showError(message) {
        this.showMessage(message, 'error');
    }

    showMessage(message, type) {
        const messageDiv = document.getElementById('statusMessage');
        if (!messageDiv) return;

        messageDiv.textContent = message;
        messageDiv.className = `status-message ${type}`;
        messageDiv.style.display = 'block';

        setTimeout(() => {
            messageDiv.style.display = 'none';
        }, 3000);
    }
}
