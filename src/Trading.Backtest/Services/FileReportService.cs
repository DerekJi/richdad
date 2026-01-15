using System.Text;
using Trading.Data.Models;
using ScottPlot;

namespace Trading.Backtest.Services;

/// <summary>
/// 文件报告服务 - 将回测报告保存到文件
/// </summary>
public class FileReportService
{
    private readonly string _reportDirectory;

    public FileReportService(string? baseDirectory = null)
    {
        _reportDirectory = baseDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "reports");
        Directory.CreateDirectory(_reportDirectory);
    }

    /// <summary>
    /// 保存回测报告到文件
    /// </summary>
    /// <returns>保存的文件路径</returns>
    public string SaveReport(BacktestResult result, decimal initialCapital, decimal leverage, string? diagnosticInfo = null)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var baseFileName = $"{timestamp}_{result.Config.StrategyName}";
        var txtFilePath = Path.Combine(_reportDirectory, $"{baseFileName}.txt");
        var chartFilePath = Path.Combine(_reportDirectory, $"{baseFileName}.png");

        var sb = new StringBuilder();
        
        // 生成报告内容
        AppendOverallMetrics(sb, result, initialCapital, leverage);
        AppendYearlyMetrics(sb, result);
        AppendMonthlyMetrics(sb, result);
        AppendWeeklyMetrics(sb, result);
        AppendEquityCurve(sb, result);
        AppendTradeDetails(sb, result);
        
        // 添加诊断信息（如果有）
        if (!string.IsNullOrEmpty(diagnosticInfo))
        {
            sb.AppendLine("\n" + new string('=', 80));
            sb.AppendLine("策略诊断分析");
            sb.AppendLine(new string('=', 80));
            sb.AppendLine(diagnosticInfo);
        }

        File.WriteAllText(txtFilePath, sb.ToString(), Encoding.UTF8);
        
        // 生成收益曲线图
        GenerateEquityChart(result, initialCapital, chartFilePath);
        
        return txtFilePath;
    }

    private void AppendOverallMetrics(StringBuilder sb, BacktestResult result, decimal initialCapital, decimal leverage)
    {
        var metrics = result.OverallMetrics;

        sb.AppendLine(new string('=', 80));
        sb.AppendLine("总体统计");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine($"策略名称: {result.Config.StrategyName}");
        sb.AppendLine($"交易品种: {result.Config.Symbol}");
        sb.AppendLine($"回测周期: {result.StartTime:yyyy-MM-dd} 至 {result.EndTime:yyyy-MM-dd}");
        sb.AppendLine($"初始资金: {initialCapital:N2} USD (杠杆: {leverage}x)");
        sb.AppendLine(new string('-', 80));
        sb.AppendLine($"总交易数: {metrics.TotalTrades}");
        sb.AppendLine($"盈利交易: {metrics.WinningTrades} ({metrics.WinRate:F2}%)");
        sb.AppendLine($"亏损交易: {metrics.LosingTrades}");
        sb.AppendLine($"总收益: {metrics.TotalProfit:N2} USD");
        sb.AppendLine($"总收益率: {metrics.TotalReturnRate:F2}%");
        sb.AppendLine($"最终资金: {(initialCapital + metrics.TotalProfit):N2} USD");
        sb.AppendLine($"盈亏比: {metrics.ProfitFactor:F2}");
        sb.AppendLine($"平均持仓时间: {FormatTimeSpan(metrics.AverageHoldingTime)}");
        sb.AppendLine($"最大连续盈利: {metrics.MaxConsecutiveWins} 单");
        sb.AppendLine($"最大连续亏损: {metrics.MaxConsecutiveLosses} 单");
        sb.AppendLine($"最大回撤: {metrics.MaxDrawdown:N2} USD ({(metrics.MaxDrawdown/initialCapital*100):F2}%) ({(metrics.MaxDrawdownTime.HasValue ? metrics.MaxDrawdownTime.Value.ToString("yyyy-MM-dd") : "N/A")})");
        sb.AppendLine($"平均每月开仓: {metrics.AverageTradesPerMonth:F1} 单");
    }

    private void AppendYearlyMetrics(StringBuilder sb, BacktestResult result)
    {
        if (!result.YearlyMetrics.Any()) return;

        sb.AppendLine("\n" + new string('=', 80));
        sb.AppendLine("年度统计");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine($"{"年份",-10} {"交易数",10} {"盈利数",10} {"胜率",10} {"盈亏额",15} {"收益率",12}");
        sb.AppendLine(new string('-', 80));

        foreach (var metric in result.YearlyMetrics)
        {
            sb.AppendLine($"{metric.Period,-10} {metric.TradeCount,10} {metric.WinningTrades,10} " +
                         $"{(metric.TradeCount > 0 ? (decimal)metric.WinningTrades / metric.TradeCount * 100 : 0),9:F2}% " +
                         $"{metric.ProfitLoss,14:N2} {metric.ReturnRate,11:F2}%");
        }
    }

    private void AppendMonthlyMetrics(StringBuilder sb, BacktestResult result)
    {
        if (!result.MonthlyMetrics.Any()) return;

        sb.AppendLine("\n" + new string('=', 80));
        sb.AppendLine("月度统计（最近12个月）");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine($"{"月份",-10} {"交易数",10} {"盈利数",10} {"胜率",10} {"盈亏额",15} {"收益率",12}");
        sb.AppendLine(new string('-', 80));

        foreach (var metric in result.MonthlyMetrics.TakeLast(12))
        {
            sb.AppendLine($"{metric.Period,-10} {metric.TradeCount,10} {metric.WinningTrades,10} " +
                         $"{(metric.TradeCount > 0 ? (decimal)metric.WinningTrades / metric.TradeCount * 100 : 0),9:F2}% " +
                         $"{metric.ProfitLoss,14:N2} {metric.ReturnRate,11:F2}%");
        }
    }

    private void AppendWeeklyMetrics(StringBuilder sb, BacktestResult result)
    {
        if (!result.WeeklyMetrics.Any()) return;

        sb.AppendLine("\n" + new string('=', 80));
        sb.AppendLine("周度统计（最近12周）");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine($"{"周",-12} {"交易数",10} {"盈利数",10} {"胜率",10} {"盈亏额",15} {"收益率",12}");
        sb.AppendLine(new string('-', 80));

        foreach (var metric in result.WeeklyMetrics.TakeLast(12))
        {
            sb.AppendLine($"{metric.Period,-12} {metric.TradeCount,10} {metric.WinningTrades,10} " +
                         $"{(metric.TradeCount > 0 ? (decimal)metric.WinningTrades / metric.TradeCount * 100 : 0),9:F2}% " +
                         $"{metric.ProfitLoss,14:N2} {metric.ReturnRate,11:F2}%");
        }
    }

    private void AppendEquityCurve(StringBuilder sb, BacktestResult result)
    {
        if (!result.EquityCurve.Any()) return;

        sb.AppendLine("\n" + new string('=', 80));
        sb.AppendLine("收益曲线（最近20个点）");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine($"{"时间",-20} {"累计收益",15} {"累计收益率",15}");
        sb.AppendLine(new string('-', 80));

        foreach (var point in result.EquityCurve.TakeLast(20))
        {
            sb.AppendLine($"{point.Time:yyyy-MM-dd HH:mm,-20} {point.CumulativeProfit,14:N2} {point.CumulativeReturnRate,14:F2}%");
        }
    }

    private void AppendTradeDetails(StringBuilder sb, BacktestResult result)
    {
        sb.AppendLine("\n" + new string('=', 80));
        sb.AppendLine($"交易明细（共 {result.Trades.Count} 笔）");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine($"{"序号",5} {"方向",6} {"开仓时间",-20} {"开仓价",12} {"平仓时间",-20} {"平仓价",12} {"盈亏",12} {"收益率",10}");
        sb.AppendLine(new string('-', 80));

        int index = 1;
        foreach (var trade in result.Trades)
        {
            var direction = trade.Direction == TradeDirection.Long ? "做多" : "做空";
            sb.AppendLine($"{index,5} {direction,6} {trade.OpenTime:yyyy-MM-dd HH:mm,-20} {trade.OpenPrice,11:F2} " +
                         $"{(trade.CloseTime.HasValue ? trade.CloseTime.Value.ToString("yyyy-MM-dd HH:mm") : "未平仓"),-20} " +
                         $"{(trade.ClosePrice.HasValue ? trade.ClosePrice.Value.ToString("F2") : ""),-12} " +
                         $"{(trade.ProfitLoss.HasValue ? trade.ProfitLoss.Value.ToString("N2") : ""),11} " +
                         $"{(trade.ReturnRate.HasValue ? trade.ReturnRate.Value.ToString("F2") + "%" : ""),9}");
            index++;
        }
    }

    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalDays >= 1)
            return $"{ts.TotalDays:F1} 天";
        if (ts.TotalHours >= 1)
            return $"{ts.TotalHours:F1} 小时";
        return $"{ts.TotalMinutes:F0} 分钟";
    }

    /// <summary>
    /// 生成收益曲线图
    /// </summary>
    private void GenerateEquityChart(BacktestResult result, decimal initialCapital, string filePath)
    {
        if (!result.Trades.Any()) return;

        var plt = new Plot();
        
        // 准备数据
        var times = new List<DateTime>();
        var equity = new List<double>();
        var drawdown = new List<double>();
        
        // 添加起始点
        times.Add(result.StartTime);
        equity.Add((double)initialCapital);
        drawdown.Add(0);
        
        decimal cumulativeProfit = 0;
        decimal peak = initialCapital;
        
        foreach (var trade in result.Trades.OrderBy(t => t.CloseTime))
        {
            if (!trade.CloseTime.HasValue || !trade.ProfitLoss.HasValue) continue;
            
            cumulativeProfit += trade.ProfitLoss.Value;
            var currentEquity = initialCapital + cumulativeProfit;
            
            times.Add(trade.CloseTime.Value);
            equity.Add((double)currentEquity);
            
            // 计算回撤
            if (currentEquity > peak)
                peak = currentEquity;
            var currentDrawdown = (double)((peak - currentEquity) / initialCapital * 100);
            drawdown.Add(-currentDrawdown); // 负数表示回撤
        }
        
        // 转换为ScottPlot需要的格式
        var xValues = times.Select(t => t.ToOADate()).ToArray();
        var yEquity = equity.ToArray();
        var yDrawdown = drawdown.ToArray();
        
        // 主图：权益曲线
        var equityLine = plt.Add.Scatter(xValues, yEquity);
        equityLine.LegendText = "账户权益";
        equityLine.Color = Color.FromHex("#2E7D32"); // 深绿色
        equityLine.LineWidth = 2;
        equityLine.MarkerSize = 0;
        
        // 添加初始资金参考线
        var initialLine = plt.Add.HorizontalLine((double)initialCapital);
        initialLine.Text = $"初始资金 ({initialCapital:N0} USD)";
        initialLine.Color = Color.FromHex("#757575"); // 灰色
        initialLine.LineWidth = 1;
        initialLine.LinePattern = LinePattern.Dashed;
        
        // 标注最终权益
        var finalEquity = equity.Last();
        var finalTime = xValues.Last();
        var marker = plt.Add.Marker(finalTime, finalEquity);
        marker.Color = finalEquity >= (double)initialCapital 
            ? Color.FromHex("#2E7D32") // 绿色
            : Color.FromHex("#C62828"); // 红色
        marker.Size = 10;
        marker.Shape = MarkerShape.FilledCircle;
        
        // 设置图表样式
        plt.Title($"{result.Config.StrategyName} - 账户权益曲线");
        plt.XLabel("时间");
        plt.YLabel("权益 (USD)");
        
        // 设置X轴为日期格式
        plt.Axes.DateTimeTicksBottom();
        
        // 添加网格
        plt.Grid.MajorLineColor = Color.FromHex("#E0E0E0");
        plt.Grid.MinorLineColor = Color.FromHex("#F5F5F5");
        
        // 显示图例
        plt.ShowLegend(Alignment.UpperLeft);
        
        // 设置图表尺寸
        plt.FigureBackground.Color = Color.FromHex("#FFFFFF");
        plt.DataBackground.Color = Color.FromHex("#FAFAFA");
        
        // 保存图表
        plt.SavePng(filePath, 1600, 900);
    }
}
