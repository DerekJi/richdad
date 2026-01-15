import type { StrategyData, BacktestRequest, BacktestResponse } from '../types/api';

/**
 * 回测API服务
 */
export class BacktestService {
  private readonly baseUrl = '/api/backtest';

  /**
   * 获取所有策略列表
   */
  async getStrategies(): Promise<StrategyData[]> {
    const response = await fetch(`${this.baseUrl}/strategies`);
    if (!response.ok) {
      throw new Error(`Failed to fetch strategies: ${response.statusText}`);
    }
    return response.json();
  }

  /**
   * 获取指定策略配置
   */
  async getStrategy(name: string): Promise<StrategyData> {
    const response = await fetch(`${this.baseUrl}/strategies/${name}`);
    if (!response.ok) {
      throw new Error(`Failed to fetch strategy ${name}: ${response.statusText}`);
    }
    return response.json();
  }

  /**
   * 运行回测
   */
  async runBacktest(request: BacktestRequest): Promise<BacktestResponse> {
    const response = await fetch(`${this.baseUrl}/run`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }

    return response.json();
  }
}

// 单例实例
export const backtestService = new BacktestService();
