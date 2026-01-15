import { strategyManager, toggleAdvanced, toggleIndicators, toggleAccount } from './components/StrategyManager';
import { backtestRunner } from './components/BacktestRunner';
import { tradeTable } from './components/TradeTable';

// 等待DOM加载完成
document.addEventListener('DOMContentLoaded', async () => {
  // 加载策略列表
  await strategyManager.loadStrategies();

  // 绑定运行回测按钮
  const runButton = document.querySelector('.run-button');
  if (runButton) {
    runButton.addEventListener('click', () => backtestRunner.runBacktest());
  }

  // 绑定分页按钮
  const prevButton = document.getElementById('prevPage');
  if (prevButton) {
    prevButton.addEventListener('click', () => tradeTable.prevPage());
  }

  const nextButton = document.getElementById('nextPage');
  if (nextButton) {
    nextButton.addEventListener('click', () => tradeTable.nextPage());
  }

  // 绑定排序按钮
  document.querySelectorAll('.sortable').forEach(th => {
    th.addEventListener('click', () => {
      const column = th.getAttribute('data-column');
      if (column) {
        tradeTable.sortTrades(column);
      }
    });
  });

  // 绑定每页显示数量变化
  const pageSizeSelect = document.getElementById('pageSize') as HTMLSelectElement;
  if (pageSizeSelect) {
    pageSizeSelect.addEventListener('change', () => {
      tradeTable.changePageSize(parseInt(pageSizeSelect.value));
    });
  }
});

// 导出全局函数供HTML onclick使用
(window as any).strategyManager = strategyManager;
(window as any).backtestRunner = backtestRunner;
(window as any).tradeTable = tradeTable;
(window as any).toggleAdvanced = toggleAdvanced;
(window as any).toggleIndicators = toggleIndicators;
(window as any).toggleAccount = toggleAccount;
(window as any).runBacktest = () => backtestRunner.runBacktest();
(window as any).sortTrades = (column: string) => tradeTable.sortTrades(column);
