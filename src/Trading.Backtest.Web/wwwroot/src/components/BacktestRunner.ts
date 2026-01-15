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
    return {
      strategyName,
      config: {
        strategyName,  // 添加这个字段
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
        requirePinBarDirectionMatch: this.getCheckboxValue('requirePinBarDirectionMatch')
      },
      account: {
        initialCapital: this.getInputNumber('initialCapital'),
        leverage: this.getInputNumber('leverage'),
        maxLossPerTradePercent: this.getInputNumber('maxLossPerTradePercent'),
        maxDailyLossPercent: this.getInputNumber('maxDailyLossPercent')
      },
      indicators: {
        baseEma: this.getInputNumber('globalBaseEma'),
        atrPeriod: this.getInputNumber('globalAtrPeriod'),
        emaFastPeriod: this.getInputNumber('emaFastPeriod'),
        emaSlowPeriod: this.getInputNumber('emaSlowPeriod')
      }
    };
  }

  /**
   * 显示回测结果
   */
  private displayResults(response: BacktestResponse): void {
    const result = response.result;
    const metrics = result.performanceMetrics;

    // 基础信息
    this.setTextContent('resultStrategy', response.strategyName);
    this.setTextContent('resultSymbol', result.symbol);
    this.setTextContent('resultPeriod', `${result.startTime} ~ ${result.endTime}`);

    // 交易统计
    this.setTextContent('totalTrades', metrics.totalTrades);
    this.setTextContent('winningTrades', metrics.winningTrades);
    this.setTextContent('losingTrades', metrics.losingTrades);
    this.setTextContent('winRate', formatNumber(metrics.winRate * 100, 2) + '%');

    // 收益统计
    this.setTextContent('totalProfit', formatNumber(metrics.totalProfit, 2));
    this.setTextContent('totalReturn', formatNumber(metrics.totalReturn * 100, 2) + '%');
    this.setTextContent('maxDrawdown', formatNumber(metrics.maxDrawdown * 100, 2) + '%');
    this.setTextContent('sharpeRatio', formatNumber(metrics.sharpeRatio, 2));

    // 最大连续盈利/亏损
    this.setInnerHTML('maxConsecutiveWins', 
      `${metrics.maxConsecutiveWins}<br/><small class="date-range">${formatDateRange(metrics.maxConsecutiveWinsStartTime, metrics.maxConsecutiveWinsEndTime)}</small>`
    );
    this.setInnerHTML('maxConsecutiveLosses', 
      `${metrics.maxConsecutiveLosses}<br/><small class="date-range">${formatDateRange(metrics.maxConsecutiveLossesStartTime, metrics.maxConsecutiveLossesEndTime)}</small>`
    );
    
    // 更新最大回撤显示（添加日期）
    this.setInnerHTML('maxDrawdown', 
      `${formatNumber(metrics.maxDrawdown * 100, 2)}%<br/><small class="date-range">${formatDateRange(metrics.maxDrawdownStartTime, metrics.maxDrawdownEndTime)}</small>`
    );

    // 平均值
    this.setTextContent('avgWin', formatNumber(metrics.avgWin, 2));
    this.setTextContent('avgLoss', formatNumber(metrics.avgLoss, 2));
    this.setTextContent('profitFactor', formatNumber(metrics.profitFactor, 2));

    // 绘制图表
    chartManager.drawEquityChart(result.equity);
    chartManager.drawYearlyChart(result.yearlyMetrics);
    chartManager.drawMonthlyChart(result.monthlyMetrics);

    // 显示交易明细
    tradeTable.displayTrades(result.trades);

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
    const button = document.querySelector('.run-button') as HTMLButtonElement;
    if (button) {
      button.disabled = show;
      button.textContent = show ? '回测中...' : '运行回测';
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
   * 设置文本内容
   */
  private setTextContent(id: string, value: any): void {
    const element = document.getElementById(id);
    if (element) {
      element.textContent = String(value);
    }
  }

  /**
   * 设置HTML内容
   */
  private setInnerHTML(id: string, html: string): void {
    const element = document.getElementById(id);
    if (element) {
      element.innerHTML = html;
    }
  }
}

// 全局实例
export const backtestRunner = new BacktestRunner();
