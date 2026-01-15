import type { StrategyData } from '../types/api';
import { backtestService } from '../services/backtestService';

/**
 * 策略管理器
 */
export class StrategyManager {
  private currentStrategy: StrategyData | null = null;

  /**
   * 加载策略列表
   */
  async loadStrategies(): Promise<void> {
    try {
      const strategies = await backtestService.getStrategies();
      const list = document.getElementById('strategyList');
      
      if (!list) return;
      
      if (!strategies || strategies.length === 0) {
        list.innerHTML = '<li class="strategy-item">暂无策略</li>';
        return;
      }
      
      list.innerHTML = strategies.map((s, i) => 
        `<li class="strategy-item ${i === 0 ? 'active' : ''}" onclick="strategyManager.loadStrategy('${s.name}')">${s.name}</li>`
      ).join('');
      
      // 默认加载第一个策略
      if (strategies.length > 0) {
        await this.loadStrategy(strategies[0].name);
      }
    } catch (error) {
      console.error('加载策略失败:', error);
      const list = document.getElementById('strategyList');
      if (list) {
        list.innerHTML = '<li class="strategy-item">加载失败</li>';
      }
    }
  }

  /**
   * 加载指定策略配置
   */
  async loadStrategy(name: string): Promise<void> {
    try {
      const data = await backtestService.getStrategy(name);
      // 将name存储到data对象中，确保有值
      this.currentStrategy = { ...data, name };
      
      const config = data.config;
      const account = data.account;
      const indicators = data.indicators;
      
      // 更新活动状态
      document.querySelectorAll('.strategy-item').forEach(item => {
        item.classList.toggle('active', item.textContent === name);
      });
      
      // 填充表单 - 基础配置
      this.setInputValue('symbol', config.symbol);
      this.setInputValue('csvFilter', config.csvFilter);
      this.setInputValue('contractSize', config.contractSize);
      
      // Pin Bar 参数
      this.setInputValue('maxBodyPercentage', config.maxBodyPercentage);
      this.setInputValue('minLongerWickPercentage', config.minLongerWickPercentage);
      this.setInputValue('maxShorterWickPercentage', config.maxShorterWickPercentage);
      this.setInputValue('minLowerWickAtrRatio', config.minLowerWickAtrRatio);
      this.setInputValue('threshold', config.threshold);
      
      // EMA 参数
      this.setInputValue('baseEma', config.baseEma);
      this.setInputValue('emaList', (config.emaList || []).join(', '));
      this.setInputValue('nearEmaThreshold', config.nearEmaThreshold);
      
      // 风险管理
      this.setInputValue('atrPeriod', config.atrPeriod);
      this.setInputValue('stopLossAtrRatio', config.stopLossAtrRatio);
      this.setInputValue('riskRewardRatio', config.riskRewardRatio);
      
      // 交易时段
      this.setCheckboxValue('noTradingHoursLimit', config.noTradingHoursLimit);
      this.setInputValue('startTradingHour', config.startTradingHour);
      this.setInputValue('endTradingHour', config.endTradingHour);
      
      // 高级参数
      this.setCheckboxValue('requirePinBarDirectionMatch', config.requirePinBarDirectionMatch);
      
      // 指标设置
      this.setInputValue('globalBaseEma', indicators?.baseEma);
      this.setInputValue('globalAtrPeriod', indicators?.atrPeriod);
      this.setInputValue('emaFastPeriod', indicators?.emaFastPeriod);
      this.setInputValue('emaSlowPeriod', indicators?.emaSlowPeriod);
      
      // 账户设置
      this.setInputValue('initialCapital', account?.initialCapital);
      this.setInputValue('leverage', account?.leverage);
      this.setInputValue('maxLossPerTradePercent', account?.maxLossPerTradePercent);
      this.setInputValue('maxDailyLossPercent', account?.maxDailyLossPercent);
      
    } catch (error) {
      console.error('加载策略配置失败:', error);
      alert('加载策略配置失败: ' + (error as Error).message);
    }
  }

  /**
   * 获取当前策略
   */
  getCurrentStrategy(): StrategyData | null {
    return this.currentStrategy;
  }

  /**
   * 设置input值
   */
  private setInputValue(id: string, value: any): void {
    const element = document.getElementById(id) as HTMLInputElement;
    if (element && value !== undefined && value !== null) {
      element.value = String(value);
    }
  }

  /**
   * 设置checkbox值
   */
  private setCheckboxValue(id: string, value: boolean): void {
    const element = document.getElementById(id) as HTMLInputElement;
    if (element) {
      element.checked = value || false;
    }
  }
}

// 全局实例
export const strategyManager = new StrategyManager();

// 切换高级参数显示
export function toggleAdvanced(): void {
  const advancedDiv = document.getElementById('advancedParams');
  const toggleText = document.getElementById('advancedToggleText');
  
  if (!advancedDiv || !toggleText) return;
  
  if (advancedDiv.style.display === 'none') {
    advancedDiv.style.display = 'block';
    toggleText.textContent = '隐藏';
  } else {
    advancedDiv.style.display = 'none';
    toggleText.textContent = '显示';
  }
}

// 切换指标设置显示
export function toggleIndicators(): void {
  const indicatorsDiv = document.getElementById('indicatorsParams');
  const toggleText = document.getElementById('indicatorsToggleText');
  
  if (!indicatorsDiv || !toggleText) return;
  
  if (indicatorsDiv.style.display === 'none') {
    indicatorsDiv.style.display = 'block';
    toggleText.textContent = '隐藏';
  } else {
    indicatorsDiv.style.display = 'none';
    toggleText.textContent = '显示';
  }
}

// 切换账户设置显示
export function toggleAccount(): void {
  const accountDiv = document.getElementById('accountParams');
  const toggleText = document.getElementById('accountToggleText');
  
  if (!accountDiv || !toggleText) return;
  
  if (accountDiv.style.display === 'none') {
    accountDiv.style.display = 'block';
    toggleText.textContent = '隐藏';
  } else {
    accountDiv.style.display = 'none';
    toggleText.textContent = '显示';
  }
}
