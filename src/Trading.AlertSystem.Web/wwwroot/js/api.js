// API 调用模块
const AlertAPI = {
    // 获取所有告警
    async getAll() {
        const response = await fetch(`${API_BASE}/alerts`);
        return await response.json();
    },

    // 获取单个告警
    async getById(id) {
        const response = await fetch(`${API_BASE}/alerts/${id}`);
        return await response.json();
    },

    // 创建告警
    async create(data) {
        const response = await fetch(`${API_BASE}/alerts`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        return response;
    },

    // 更新告警
    async update(id, data) {
        const response = await fetch(`${API_BASE}/alerts/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        return response;
    },

    // 删除告警
    async delete(id) {
        const response = await fetch(`${API_BASE}/alerts/${id}`, {
            method: 'DELETE'
        });
        return response;
    },

    // 重置告警
    async reset(id) {
        const response = await fetch(`${API_BASE}/alerts/${id}/reset`, {
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
