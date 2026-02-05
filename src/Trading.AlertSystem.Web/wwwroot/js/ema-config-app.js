// EMA配置管理应用

let currentConfig = null;

// 页面加载时初始化
document.addEventListener('DOMContentLoaded', () => {
    loadConfig();
    setupEventListeners();
});

// 设置事件监听器
function setupEventListeners() {
    // 品种输入
    const symbolInput = document.getElementById('symbolInput');
    symbolInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            addSymbol();
        }
    });

    // EMA周期输入
    const emaPeriodInput = document.getElementById('emaPeriodInput');
    emaPeriodInput.addEventListener('keydown', (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            addEmaPeriod();
        }
    });

    // 标签容器点击聚焦输入框
    document.getElementById('symbolsTags').parentElement.addEventListener('click', () => {
        symbolInput.focus();
    });

    document.getElementById('emaPeriodsTags').parentElement.addEventListener('click', () => {
        emaPeriodInput.focus();
    });
}

// 加载配置
async function loadConfig() {
    try {
        showStatus('正在加载配置...', 'info');

        const response = await fetch('/api/emaconfig');
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${await response.text()}`);
        }

        currentConfig = await response.json();
        renderConfig(currentConfig);
        showStatus('配置加载成功', 'success');
        setTimeout(() => hideStatus(), 2000);
    } catch (error) {
        console.error('加载配置失败:', error);
        showStatus(`加载配置失败: ${error.message}`, 'error');
    }
}

// 渲染配置到页面
function renderConfig(config) {
    // 启用状态
    document.getElementById('enabledSwitch').checked = config.enabled;

    // 品种
    renderTags('symbolsTags', config.symbols, removeSymbol);

    // 时间周期
    const timeframeChecks = document.querySelectorAll('.timeframe-check');
    timeframeChecks.forEach(check => {
        check.checked = config.timeFrames.includes(check.value);
    });

    // EMA周期
    renderTags('emaPeriodsTags', config.emaPeriods, removeEmaPeriod);

    // 历史数据倍数
    document.getElementById('historyMultiplier').value = config.historyMultiplier;

    // 更新信息
    document.getElementById('updatedAt').textContent = new Date(config.updatedAt).toLocaleString('zh-CN');
    document.getElementById('updatedBy').textContent = config.updatedBy || 'System';
}

// 渲染标签
function renderTags(containerId, items, removeCallback) {
    const container = document.getElementById(containerId);
    container.innerHTML = '';

    items.forEach(item => {
        const tag = document.createElement('div');
        tag.className = 'tag';
        tag.innerHTML = `
            <span>${item}</span>
            <span class="tag-remove" onclick="${removeCallback.name}('${item}')">×</span>
        `;
        container.appendChild(tag);
    });
}

// 添加品种
function addSymbol() {
    const input = document.getElementById('symbolInput');
    const value = input.value.trim().toUpperCase();

    if (!value) return;

    if (!currentConfig.symbols.includes(value)) {
        currentConfig.symbols.push(value);
        renderTags('symbolsTags', currentConfig.symbols, removeSymbol);
    }

    input.value = '';
}

// 移除品种
function removeSymbol(symbol) {
    currentConfig.symbols = currentConfig.symbols.filter(s => s !== symbol);
    renderTags('symbolsTags', currentConfig.symbols, removeSymbol);
}

// 添加EMA周期
function addEmaPeriod() {
    const input = document.getElementById('emaPeriodInput');
    const value = parseInt(input.value);

    if (!value || value < 1 || value > 500) {
        showStatus('EMA周期必须在1-500之间', 'error');
        return;
    }

    if (!currentConfig.emaPeriods.includes(value)) {
        currentConfig.emaPeriods.push(value);
        currentConfig.emaPeriods.sort((a, b) => a - b);
        renderTags('emaPeriodsTags', currentConfig.emaPeriods, removeEmaPeriod);
    }

    input.value = '';
}

// 移除EMA周期
function removeEmaPeriod(period) {
    currentConfig.emaPeriods = currentConfig.emaPeriods.filter(p => p !== parseInt(period));
    renderTags('emaPeriodsTags', currentConfig.emaPeriods, removeEmaPeriod);
}

// 保存配置
async function saveConfig() {
    try {
        if (!currentConfig) {
            showStatus('配置未加载，无法保存', 'error');
            return;
        }

        // 收集表单数据
        currentConfig.enabled = document.getElementById('enabledSwitch').checked;

        // 时间周期
        const selectedTimeFrames = [];
        document.querySelectorAll('.timeframe-check:checked').forEach(check => {
            selectedTimeFrames.push(check.value);
        });
        currentConfig.timeFrames = selectedTimeFrames;

        // 历史数据倍数
        currentConfig.historyMultiplier = parseInt(document.getElementById('historyMultiplier').value);

        // 验证
        if (currentConfig.symbols.length === 0) {
            showStatus('至少需要配置一个交易品种', 'error');
            return;
        }

        if (currentConfig.timeFrames.length === 0) {
            showStatus('至少需要选择一个时间周期', 'error');
            return;
        }

        if (currentConfig.emaPeriods.length === 0) {
            showStatus('至少需要配置一个EMA周期', 'error');
            return;
        }

        showStatus('正在保存配置...', 'info');

        const response = await fetch('/api/emaconfig', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(currentConfig)
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || `HTTP ${response.status}`);
        }

        const savedConfig = await response.json();
        currentConfig = savedConfig;
        renderConfig(savedConfig);

        showStatus('✅ 配置保存成功，服务已重新加载！', 'success');
    } catch (error) {
        console.error('保存配置失败:', error);
        showStatus(`❌ 保存失败: ${error.message}`, 'error');
    }
}

// 查看状态
async function viewStatus() {
    try {
        showStatus('正在获取状态...', 'info');

        const response = await fetch('/api/emaconfig/status');
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const status = await response.json();

        let message = `
            <strong>EMA监测状态</strong><br>
            启用: ${status.enabled ? '✅ 是' : '❌ 否'}<br>
            监测状态数: ${status.stateCount}<br>
            品种数: ${status.config.symbols.length}<br>
            时间周期: ${status.config.timeFrames.join(', ')}<br>
            EMA周期: ${status.config.emaPeriods.join(', ')}
        `;

        showStatus(message, 'info');
    } catch (error) {
        console.error('获取状态失败:', error);
        showStatus(`获取状态失败: ${error.message}`, 'error');
    }
}

// 显示状态消息
function showStatus(message, type) {
    const statusDiv = document.getElementById('statusMessage');
    statusDiv.innerHTML = message;
    statusDiv.className = `status-message show ${type}`;
}

// 隐藏状态消息
function hideStatus() {
    const statusDiv = document.getElementById('statusMessage');
    statusDiv.classList.remove('show');
}
