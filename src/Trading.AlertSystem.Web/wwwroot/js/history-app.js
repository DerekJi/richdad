// 告警历史应用初始化
document.addEventListener('DOMContentLoaded', async () => {
    console.log('告警历史页面初始化...');

    try {
        await HistoryManager.init();
        console.log('告警历史页面初始化完成');
    } catch (error) {
        console.error('初始化失败:', error);
        HistoryUI.showError('初始化失败: ' + error.message);
    }
});
