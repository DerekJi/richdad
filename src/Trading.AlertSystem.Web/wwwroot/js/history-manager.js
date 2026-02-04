// 告警历史管理器
const HistoryManager = {
    currentPage: 1,
    pageSize: 20,
    filters: {},
    data: null,

    // 初始化
    async init() {
        await this.loadHistory();
    },

    // 加载告警历史
    async loadHistory(page = 1) {
        try {
            this.currentPage = page;
            HistoryUI.showLoading();

            const result = await HistoryAPI.getAll(this.currentPage, this.pageSize, this.filters);
            this.data = result;

            HistoryUI.renderHistory(result.items);
            HistoryUI.renderPagination(result);
        } catch (error) {
            console.error('加载告警历史失败:', error);
            HistoryUI.showError('加载告警历史失败: ' + error.message);
        }
    },

    // 应用筛选
    async applyFilters() {
        const type = document.getElementById('filterType').value;
        const symbol = document.getElementById('filterSymbol').value.trim().toUpperCase();
        const startTime = document.getElementById('filterStartTime').value;
        const endTime = document.getElementById('filterEndTime').value;

        this.filters = {};
        if (type !== '') this.filters.type = type;
        if (symbol) this.filters.symbol = symbol;
        if (startTime) this.filters.startTime = new Date(startTime).toISOString();
        if (endTime) this.filters.endTime = new Date(endTime).toISOString();

        await this.loadHistory(1);
    },

    // 清空筛选
    async clearFilters() {
        document.getElementById('filterType').value = '';
        document.getElementById('filterSymbol').value = '';
        document.getElementById('filterStartTime').value = '';
        document.getElementById('filterEndTime').value = '';

        this.filters = {};
        await this.loadHistory(1);
    },

    // 显示统计信息
    async showStats() {
        try {
            const stats = await HistoryAPI.getStats(7);
            HistoryUI.renderStats(stats);
        } catch (error) {
            console.error('加载统计信息失败:', error);
            alert('加载统计信息失败: ' + error.message);
        }
    },

    // 查看详情
    async viewDetail(id) {
        try {
            const item = await HistoryAPI.getById(id);
            HistoryUI.showDetailModal(item);
        } catch (error) {
            console.error('加载详情失败:', error);
            alert('加载详情失败: ' + error.message);
        }
    }
};
