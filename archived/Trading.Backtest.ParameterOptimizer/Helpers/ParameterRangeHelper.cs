using Trading.Backtest.Data.Models;
namespace Trading.Backtest.ParameterOptimizer.Helpers;

public static class ParameterRangeHelper
{
    /// <summary>
    /// 生成整数参数范围
    /// </summary>
    public static int[] SetRange(int start, int end, int step)
    {
        var result = new List<int>();
        for (int i = start; i <= end; i += step)
        {
            result.Add(i);
        }
        return result.ToArray();
    }

    /// <summary>
    /// 生成小数参数范围
    /// </summary>
    public static decimal[] SetRange(decimal start, decimal end, decimal step)
    {
        var result = new List<decimal>();
        for (decimal i = start; i <= end; i += step)
        {
            result.Add(i);
        }
        return result.ToArray();
    }
}
