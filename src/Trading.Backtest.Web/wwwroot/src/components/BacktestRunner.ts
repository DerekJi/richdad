import type { BacktestRequest, BacktestResponse } from '../types/api';
import { backtestService } from '../services/backtestService';
import { formatNumber, formatDateRange } from '../utils/helpers';
import { strategyManager } from './StrategyManager';
import { chartManager } from './ChartManager';
import { tradeTable } from './TradeTable';

/**
 * 回测运行器
 */
export class BacktestRunner {
  private isRunning = false;
  public lastConfig: any = null;
  public lastResponse: BacktestResponse | null = null;

  /**
   * 运行回测
   */
  async runBacktest(): Promise<void> {
    if (this.isRunning) {
      alert('回测正在进行中...');
      return;
    }

    const strategy = strategyManager.getCurrentStrategy();
    if (!strategy) {
      alert('请先选择一个策略');
      return;
    }

    try {
      this.isRunning = true;
      this.showLoading(true);

      const request = this.buildRequest(strategy.name);
      const response = await backtestService.runBacktest(request);

      this.displayResults(response);
      this.showLoading(false);

      // 滚动到统计结果部分
      setTimeout(() => {
        const resultsElement = document.getElementById('results');
        if (resultsElement) {
          resultsElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
      }, 300);
    } catch (error) {
      console.error('回测失败:', error);
      alert('回测失败: ' + (error as Error).message);
      this.showLoading(false);
    } finally {
      this.isRunning = false;
    }
  }

  /**
   * 构建回测请求
   */
  private buildRequest(strategyName: string): BacktestRequest {
    const request: BacktestRequest = {
      strategyName,
      symbol: this.getInputValue('symbol'),
      csvFilter: this.getInputValue('csvFilter'),
      contractSize: this.getInputNumber('contractSize'),
      maxBodyPercentage: this.getInputNumber('maxBodyPercentage'),
      minLongerWickPercentage: this.getInputNumber('minLongerWickPercentage'),
      maxShorterWickPercentage: this.getInputNumber('maxShorterWickPercentage'),
      minLowerWickAtrRatio: this.getInputNumber('minLowerWickAtrRatio'),
      threshold: this.getInputNumber('threshold'),
      baseEma: this.getInputNumber('baseEma'),
      emaList: this.parseEmaList(this.getInputValue('emaList')),
      nearEmaThreshold: this.getInputNumber('nearEmaThreshold'),
      atrPeriod: this.getInputNumber('atrPeriod'),
      stopLossAtrRatio: this.getInputNumber('stopLossAtrRatio'),
      riskRewardRatio: this.getInputNumber('riskRewardRatio'),
      noTradingHoursLimit: this.getCheckboxValue('noTradingHoursLimit'),
      startTradingHour: this.getInputNumber('startTradingHour'),
      endTradingHour: this.getInputNumber('endTradingHour'),
      noTradeHours: this.parseNoTradeHours(this.getInputValue('noTradeHours')),
      requirePinBarDirectionMatch: this.getCheckboxValue('requirePinBarDirectionMatch'),
      initialCapital: this.getInputNumber('initialCapital'),
      leverage: this.getInputNumber('leverage'),
      maxLossPerTradePercent: this.getInputNumber('maxLossPerTradePercent'),
      maxDailyLossPercent: this.getInputNumber('maxDailyLossPercent')
    };

    // 保存配置供K线图使用
    this.lastConfig = {
      symbol: request.symbol,
      csvFilter: request.csvFilter,
      baseEma: request.baseEma,
      atrPeriod: request.atrPeriod,
      emaList: request.emaList
    };

    return request;
  }

  /**
   * 显示回测结果
   */
  private displayResults(response: BacktestResponse): void {
    this.lastResponse = response;
    const result = response.result;
    const metrics = result.overallMetrics;

    // 动态生成关键指标网格
    const metricsGrid = document.getElementById('metricsGrid');
    if (metricsGrid) {
      metricsGrid.innerHTML = `
        <div class="metric-card">
          <div class="metric-label">策略名称</div>
          <div class="metric-value">${response.strategyName || response.config.strategyName}</div>
        </div>
        <div class="metric-card">
          <div class="metric-label">交易品种</div>
          <div class="metric-value">${result.symbol || response.config.symbol}</div>
        </div>
        <div class="metric-card">
          <div class="metric-label">回测周期</div>
          <div class="metric-value">${result.startTime.split('T')[0]} ~ ${result.endTime.split('T')[0]}</div>
        </div>
        <div class="metric-card">
          <div class="metric-label">总交易数</div>
          <div class="metric-value">${metrics.totalTrades}</div>
        </div>
        <div class="metric-card">
          <div class="metric-label">盈利/亏损</div>
          <div class="metric-value">${metrics.winningTrades} / ${metrics.losingTrades}</div>
        </div>
        <div class="metric-card">
          <div class="metric-label">胜率</div>
          <div class="metric-value">${formatNumber(metrics.winRate, 2)}%</div>
        </div>
        <div class="metric-card">
          <div class="metric-label">总收益</div>
          <div class="metric-value ${metrics.totalProfit >= 0 ? 'positive' : 'negative'}">
            $${formatNumber(metrics.totalProfit, 2)}
          </div>
        </div>
        <div class="metric-card">
          <div class="metric-label">收益率</div>
          <div class="metric-value ${metrics.totalReturnRate >= 0 ? 'positive' : 'negative'}">
            ${formatNumber(metrics.totalReturnRate, 2)}%
          </div>
        </div>
        <div class="metric-card">
          <div class="metric-label">夏普比率</div>
          <div class="metric-value">${formatNumber(metrics.sharpeRatio, 2)}</div>
        </div>
        <div class="metric-card">
          <div class="metric-label">盈亏比</div>
          <div class="metric-value">${formatNumber(metrics.profitFactor, 2)}</div>
        </div>
        <div class="metric-card">
          <div class="metric-label">平均盈利</div>
          <div class="metric-value positive">$${formatNumber(metrics.avgWin, 2)}</div>
        </div>
        <div class="metric-card">
          <div class="metric-label">平均亏损</div>
          <div class="metric-value negative">$${formatNumber(Math.abs(metrics.avgLoss), 2)}</div>
        </div>
        <div class="metric-card">
          <div class="metric-label">最大回撤</div>
          <div class="metric-value negative">$${formatNumber(metrics.maxDrawdown, 2)}</div>
          ${formatDateRange(metrics.maxDrawdownStartTime, metrics.maxDrawdownEndTime)}
        </div>
        <div class="metric-card">
          <div class="metric-label">最大连续盈利</div>
          <div class="metric-value">${metrics.maxConsecutiveWins}</div>
          ${formatDateRange(metrics.maxConsecutiveWinsStartTime, metrics.maxConsecutiveWinsEndTime)}
        </div>
        <div class="metric-card">
          <div class="metric-label">最大连续亏损</div>
          <div class="metric-value">${metrics.maxConsecutiveLosses}</div>
          ${formatDateRange(metrics.maxConsecutiveLossesStartTime, metrics.maxConsecutiveLossesEndTime)}
        </div>
      `;
    }

    // 绘制图表
    chartManager.drawEquityChart(result.equityCurve);
    chartManager.drawYearlyChart(result.yearlyMetrics);
    chartManager.drawMonthlyChart(result.monthlyMetrics);
    chartManager.drawTimeSlotCharts(result.topProfitSlots, result.topLossSlots);

    // 显示交易明细
    tradeTable.displayTrades(result.allTrades);

    // 显示结果区域
    const resultsDiv = document.getElementById('results');
    if (resultsDiv) {
      resultsDiv.style.display = 'block';
      resultsDiv.scrollIntoView({ behavior: 'smooth' });
    }
  }

  /**
   * 显示/隐藏加载状态
   */
  private showLoading(show: boolean): void {
    const results = document.getElementById('results');
    const btn = document.getElementById('runBacktestBtn') as HTMLButtonElement;

    if (results) {
      results.style.display = show ? 'none' : 'block';
    }

    if (btn) {
      btn.disabled = show;
      btn.textContent = show ? '⏳ 运行中...' : '🚀 运行回测';
    }
  }

  /**
   * 获取input值
   */
  private getInputValue(id: string): string {
    const element = document.getElementById(id) as HTMLInputElement;
    return element ? element.value : '';
  }

  /**
   * 获取input数字值
   */
  private getInputNumber(id: string): number {
    const value = this.getInputValue(id);
    return value ? parseFloat(value) : 0;
  }

  /**
   * 获取checkbox值
   */
  private getCheckboxValue(id: string): boolean {
    const element = document.getElementById(id) as HTMLInputElement;
    return element ? element.checked : false;
  }

  /**
   * 解析EMA列表
   */
  private parseEmaList(value: string): number[] {
    if (!value) return [];
    return value.split(',').map(s => parseInt(s.trim())).filter(n => !isNaN(n));
  }

  /**
   * 解析禁止交易时段
   */
  private parseNoTradeHours(value: string): number[] {
    if (!value || value.trim() === '') return [];
    return value.split(',').map(v => parseInt(v.trim())).filter(n => !isNaN(n) && n >= 0 && n <= 23);
  }

  // Unused utility functions - keeping for potential future use
  // /**
  //  * 设置文本内容
  //  */
  // private setTextContent(id: string, value: any): void {
  //   const element = document.getElementById(id);
  //   if (element) {
  //     element.textContent = String(value);
  //   }
  // }

  // /**
  //  * 设置HTML内容
  //  */
  // private setInnerHTML(id: string, html: string): void {
  //   const element = document.getElementById(id);
  //   if (element) {
  //     element.innerHTML = html;
  //   }
  // }
}

// 全局实例
export const backtestRunner = new BacktestRunner();
