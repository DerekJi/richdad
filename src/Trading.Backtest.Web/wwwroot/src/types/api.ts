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
  noTradeHours: number[];
  startTradingHour: number;
  endTradingHour: number;
  requirePinBarDirectionMatch: boolean;
  minAdx: number;
  adxPeriod: number;
  adxTimeframe: string;
  lowAdxRiskRewardRatio: number;
  maxConsecutiveLosses: number;
  pauseDaysAfterLosses: number;
  enableDynamicRiskManagement: boolean;
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
  totalReturnRate: number;
  avgWin: number;
  avgLoss: number;
  sharpeRatio: number;
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
  startDate: string;
  endDate: string;
  tradeCount: number;
  winningTrades: number;
  winRate: number;
  profitLoss: number;
  returnRate: number;
}

export interface EquityPoint {
  time: string;
  cumulativeProfit: number;
  cumulativeReturnRate: number;
}

export interface Trade {
  id: string;
  direction: string;
  openTime: string;
  openPrice: number;
  stopLoss: number;
  takeProfit: number;
  stopLossPips: number;
  takeProfitPips: number;
  closeTime?: string;
  closePrice?: number;
  closeReason?: string;
  profitLoss: number;
  returnRate: number;
  lots: number;
}

export interface Candle {
  dateTime: string;
  open: number;
  high: number;
  low: number;
  close: number;
}

export interface TradeKlineResponse {
  candles: Candle[];
  emaData: { [key: number]: (number | null)[] };
  openIndex: number;
  closeIndex: number;
  openPrice: number;
  closePrice?: number;
  stopLoss: number;
  takeProfit: number;
  direction: string;
}

export interface TradeKlineRequest {
  symbol: string;
  csvFilter?: string;
  openTime: string;
  closeTime?: string;
  openPrice: number;
  closePrice?: number;
  stopLoss: number;
  takeProfit: number;
  direction: string;
  baseEma: number;
  atrPeriod: number;
  emaList?: number[];
}

export interface TimeSlot {
  timeSlot: string;
  tradeCount: number;
  totalProfitLoss: number;
  avgProfitLoss: number;
  winRate: number;
}

export interface BacktestResult {
  id: string;
  symbol: string;
  startTime: string;
  endTime: string;
  overallMetrics: PerformanceMetrics;
  yearlyMetrics: PeriodMetrics[];
  monthlyMetrics: PeriodMetrics[];
  weeklyMetrics: PeriodMetrics[];
  topProfitSlots: TimeSlot[];
  topLossSlots: TimeSlot[];
  equityCurve: EquityPoint[];
  allTrades: Trade[];
}

export interface BacktestResponse {
  strategyName: string;
  config: StrategyConfig;
  account: AccountSettings;
  csvFileName?: string;
  result: BacktestResult;
}

export interface BacktestRequest {
  strategyName: string;
  symbol: string;
  csvFilter?: string;
  contractSize: number;
  baseEma: number;
  atrPeriod: number;
  threshold: number;
  maxBodyPercentage: number;
  minLongerWickPercentage: number;
  maxShorterWickPercentage: number;
  emaList?: number[];
  nearEmaThreshold: number;
  riskRewardRatio: number;
  stopLossAtrRatio: number;
  startTradingHour: number;
  endTradingHour: number;
  noTradingHoursLimit: boolean;
  noTradeHours?: number[];
  requirePinBarDirectionMatch: boolean;
  minLowerWickAtrRatio: number;
  minAdx: number;
  adxPeriod: number;
  adxTimeframe: string;
  lowAdxRiskRewardRatio: number;
  maxConsecutiveLosses: number;
  pauseDaysAfterLosses: number;
  // Account settings
  initialCapital: number;
  leverage: number;
  maxLossPerTradePercent: number;
  maxDailyLossPercent: number;
  enableDynamicRiskManagement: boolean;
  // Date range filter
  startDate?: string;
  endDate?: string;
}
