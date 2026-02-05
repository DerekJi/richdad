// API 调用模块
const AlertAPI = {
    // 获取所有监控规则
    async getAll() {
        const response = await fetch(`${API_BASE}/pricemonitor`);
        return await response.json();
    },

    // 获取单个监控规则
    async getById(id) {
        const response = await fetch(`${API_BASE}/pricemonitor/${id}`);
        return await response.json();
    },

    // 创建监控规则
    async create(data) {
        const response = await fetch(`${API_BASE}/pricemonitor`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        return response;
    },

    // 更新监控规则
    async update(id, data) {
        const response = await fetch(`${API_BASE}/pricemonitor/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        return response;
    },

    // 删除监控规则
    async delete(id) {
        const response = await fetch(`${API_BASE}/pricemonitor/${id}`, {
            method: 'DELETE'
        });
        return response;
    },

    // 重置监控规则
    async reset(id) {
        const response = await fetch(`${API_BASE}/pricemonitor/${id}/reset`, {
            method: 'POST'
        });
        return response;
    }
};

const SystemAPI = {
    // 测试 Telegram
    async testTelegram() {
        const response = await fetch(`${API_BASE}/system/test-telegram`, {
            method: 'POST'
        });
        return await response.json();
    },

    // 测试 TradeLocker
    async testTradeLocker() {
        const response = await fetch(`${API_BASE}/system/test-tradelocker`, {
            method: 'POST'
        });
        return await response.json();
    },

    // 立即检查
    async checkNow() {
        const response = await fetch(`${API_BASE}/system/check-now`, {
            method: 'POST'
        });
        return await response.json();
    }
};
