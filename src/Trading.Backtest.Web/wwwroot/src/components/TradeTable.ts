import type { Trade } from '../types/api';
import { formatNumber, getPropertyValue } from '../utils/helpers';

/**
 * 交易明细表格
 */
export class TradeTable {
  private trades: Trade[] = [];
  private currentPage = 1;
  private pageSize = 20;
  private sortColumn: string | null = null;
  private sortAscending = true;

  /**
   * 显示交易明细
   */
  displayTrades(trades: Trade[]): void {
    this.trades = trades || [];
    this.currentPage = 1;
    this.sortColumn = null;
    this.sortAscending = true;
    this.updateTradesDisplay();
  }

  /**
   * 更新交易明细显示
   */
  private updateTradesDisplay(): void {
    const tbody = document.getElementById('tradesTable');
    if (!tbody) return;

    // 计算分页
    const start = (this.currentPage - 1) * this.pageSize;
    const end = start + this.pageSize;
    const pageTrades = this.trades.slice(start, end);

    // 渲染表格
    tbody.innerHTML = pageTrades.map((trade, index) => `
      <tr>
        <td>${trade.openTime}</td>
        <td>${trade.direction}</td>
        <td>${formatNumber(trade.lots, 2)}</td>
        <td>${formatNumber(trade.openPrice, 5)}</td>
        <td>${formatNumber(trade.stopLoss, 5)}</td>
        <td>${formatNumber(trade.stopLossPips, 1)}</td>
        <td>${formatNumber(trade.takeProfit, 5)}</td>
        <td>${formatNumber(trade.takeProfitPips, 1)}</td>
        <td>${trade.closeTime || '-'}</td>
        <td>${trade.closePrice ? formatNumber(trade.closePrice, 5) : '-'}</td>
        <td>${trade.closeReason || '-'}</td>
        <td class="${trade.profitLoss >= 0 ? 'profit' : 'loss'}">${formatNumber(trade.profitLoss, 2)}</td>
        <td>${formatNumber(trade.returnRate * 100, 2)}%</td>
        <td><button class="btn btn-secondary" onclick="showTradeChart(${start + index})">查看</button></td>
      </tr>
    `).join('');

    // 更新分页信息
    this.updatePagination();
  }

  /**
   * 更新分页控件
   */
  private updatePagination(): void {
    const totalPages = Math.ceil(this.trades.length / this.pageSize);
    const paginationDiv = document.getElementById('pagination');

    if (!paginationDiv) return;

    if (totalPages <= 1) {
      paginationDiv.style.display = 'none';
      return;
    }

    paginationDiv.style.display = 'flex';

    const pageInfo = document.getElementById('pageInfo');
    if (pageInfo) {
      pageInfo.textContent = `第 ${this.currentPage} / ${totalPages} 页，共 ${this.trades.length} 条`;
    }

    // 更新按钮状态
    const prevBtn = document.getElementById('prevPage') as HTMLButtonElement;
    const nextBtn = document.getElementById('nextPage') as HTMLButtonElement;

    if (prevBtn) {
      prevBtn.disabled = this.currentPage === 1;
    }
    if (nextBtn) {
      nextBtn.disabled = this.currentPage === totalPages;
    }
  }

  /**
   * 上一页
   */
  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.updateTradesDisplay();
    }
  }

  /**
   * 下一页
   */
  nextPage(): void {
    const totalPages = Math.ceil(this.trades.length / this.pageSize);
    if (this.currentPage < totalPages) {
      this.currentPage++;
      this.updateTradesDisplay();
    }
  }

  /**
   * 排序
   */
  sortTrades(column: string): void {
    // 如果点击同一列，切换排序方向
    if (this.sortColumn === column) {
      this.sortAscending = !this.sortAscending;
    } else {
      this.sortColumn = column;
      this.sortAscending = true;
    }

    // 执行排序
    this.trades.sort((a, b) => {
      const aVal = getPropertyValue(a, column);
      const bVal = getPropertyValue(b, column);

      // 处理null/undefined
      if (aVal === null || aVal === undefined) return 1;
      if (bVal === null || bVal === undefined) return -1;

      // 数字比较
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return this.sortAscending ? aVal - bVal : bVal - aVal;
      }

      // 字符串比较
      const aStr = String(aVal);
      const bStr = String(bVal);
      const result = aStr.localeCompare(bStr);
      return this.sortAscending ? result : -result;
    });

    // 更新排序图标
    this.updateSortIcons(column);

    // 重置到第一页
    this.currentPage = 1;
    this.updateTradesDisplay();
  }

  /**
   * 更新排序图标
   */
  private updateSortIcons(activeColumn: string): void {
    document.querySelectorAll('.sortable').forEach(th => {
      const column = th.getAttribute('data-column');
      const icon = th.querySelector('.sort-icon');

      if (!icon) return;

      if (column === activeColumn) {
        icon.textContent = this.sortAscending ? '▲' : '▼';
        icon.classList.add('active');
      } else {
        icon.textContent = '▲';
        icon.classList.remove('active');
      }
    });
  }

  /**
   * 改变每页显示数量
   */
  changePageSize(size: number): void {
    this.pageSize = size;
    this.currentPage = 1;
    this.updateTradesDisplay();
  }

  /**
   * 获取指定索引的交易
   */
  getTrade(index: number): Trade | undefined {
    return this.trades[index];
  }

  /**
   * 获取所有交易
   */
  getAllTrades(): Trade[] {
    return this.trades;
  }
}

// 全局实例
export const tradeTable = new TradeTable();
