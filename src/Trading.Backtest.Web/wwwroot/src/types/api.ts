// API响应类型定义

export interface StrategyConfig {
  strategyName: string;
  symbol: string;
  csvFilter: string;
  contractSize: number;
  baseEma: number;
  atrPeriod: number;
  emaList: number[];
  maxBodyPercentage: number;
  minLongerWickPercentage: number;
  maxShorterWickPercentage: number;
  minLowerWickAtrRatio: number;
  threshold: number;
  nearEmaThreshold: number;
  stopLossAtrRatio: number;
  riskRewardRatio: number;
  noTradingHoursLimit: boolean;
  startTradingHour: number;
  endTradingHour: number;
  requirePinBarDirectionMatch: boolean;
}

export interface AccountSettings {
  initialCapital: number;
  leverage: number;
  maxLossPerTradePercent: number;
  maxDailyLossPercent: number;
}

export interface IndicatorSettings {
  baseEma: number;
  atrPeriod: number;
  emaFastPeriod: number;
  emaSlowPeriod: number;
}

export interface StrategyData {
  name: string;
  config: StrategyConfig;
  account: AccountSettings;
  indicators: IndicatorSettings;
}

export interface PerformanceMetrics {
  totalTrades: number;
  winningTrades: number;
  losingTrades: number;
  winRate: number;
  totalProfit: number;
  totalReturn: number;  // 改名
  avgWin: number;  // 新增
  avgLoss: number;  // 新增
  sharpeRatio: number;  // 新增
  maxConsecutiveWins: number;
  maxConsecutiveWinsStartTime?: string;
  maxConsecutiveWinsEndTime?: string;
  maxConsecutiveLosses: number;
  maxConsecutiveLossesStartTime?: string;
  maxConsecutiveLossesEndTime?: string;
  maxDrawdown: number;
  maxDrawdownStartTime?: string;
  maxDrawdownEndTime?: string;
  profitFactor: number;
}

export interface PeriodMetrics {
  period: string;
  trades: number;  // 改名
  winRate: number;
  profit: number;  // 改名
  return: number;  // 新增
}

export interface EquityPoint {
  time: string;
  equity: number;  // 改名
}

export interface Trade {
  entryTime: string;  // 改名
  type: string;  // 改名
  entryPrice: number;  // 改名
  exitPrice: number;  // 改名
  volume: number;  // 改名
  profit: number;  // 改名
  profitPercent: number;  // 改名
  exitReason: string;  // 改名
}

export interface BacktestResult {
  symbol: string;  // 新增
  startTime: string;
  endTime: string;
  performanceMetrics: PerformanceMetrics;  // 改名
  yearlyMetrics: PeriodMetrics[];
  monthlyMetrics: PeriodMetrics[];
  equity: EquityPoint[];  // 改名
  trades: Trade[];  // 改名
}

export interface BacktestResponse {
  strategyName: string;  // 新增
  result: BacktestResult;
}

export interface BacktestRequest {
  strategyName: string;
  config: StrategyConfig;
  account: AccountSettings;
  indicators: IndicatorSettings;
}
