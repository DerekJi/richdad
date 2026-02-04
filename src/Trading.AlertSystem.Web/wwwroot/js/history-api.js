// 告警历史 API 接口
const HistoryAPI = {
    // 获取所有告警历史（分页）
    async getAll(pageNumber = 1, pageSize = 50, filters = {}) {
        const params = new URLSearchParams({
            pageNumber: pageNumber.toString(),
            pageSize: pageSize.toString()
        });

        if (filters.type !== undefined && filters.type !== '') {
            params.append('type', filters.type);
        }
        if (filters.symbol) {
            params.append('symbol', filters.symbol);
        }
        if (filters.startTime) {
            params.append('startTime', filters.startTime);
        }
        if (filters.endTime) {
            params.append('endTime', filters.endTime);
        }

        const response = await fetch(`${API_BASE_URL}/alerthistory?${params}`);
        if (!response.ok) {
            throw new Error('获取告警历史失败');
        }
        return await response.json();
    },

    // 获取最近的告警历史
    async getRecent(count = 100) {
        const response = await fetch(`${API_BASE_URL}/alerthistory/recent?count=${count}`);
        if (!response.ok) {
            throw new Error('获取最近告警历史失败');
        }
        return await response.json();
    },

    // 根据ID获取单个告警历史
    async getById(id) {
        const response = await fetch(`${API_BASE_URL}/alerthistory/${id}`);
        if (!response.ok) {
            throw new Error('获取告警历史详情失败');
        }
        return await response.json();
    },

    // 根据品种获取告警历史
    async getBySymbol(symbol, limit = 100) {
        const response = await fetch(`${API_BASE_URL}/alerthistory/symbol/${symbol}?limit=${limit}`);
        if (!response.ok) {
            throw new Error('获取品种告警历史失败');
        }
        return await response.json();
    },

    // 根据类型获取告警历史
    async getByType(type, limit = 100) {
        const response = await fetch(`${API_BASE_URL}/alerthistory/type/${type}?limit=${limit}`);
        if (!response.ok) {
            throw new Error('获取类型告警历史失败');
        }
        return await response.json();
    },

    // 获取统计信息
    async getStats(days = 7) {
        const response = await fetch(`${API_BASE_URL}/alerthistory/stats?days=${days}`);
        if (!response.ok) {
            throw new Error('获取统计信息失败');
        }
        return await response.json();
    },

    // 清理旧记录
    async cleanupOldRecords(daysToKeep = 30) {
        const response = await fetch(`${API_BASE_URL}/alerthistory/cleanup?daysToKeep=${daysToKeep}`, {
            method: 'DELETE'
        });
        if (!response.ok) {
            throw new Error('清理旧记录失败');
        }
        return await response.json();
    }
};
