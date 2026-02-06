namespace Trading.Core.RiskManagement;

/// <summary>
/// Utility class for trading day calculations based on server timezone
/// </summary>
public class TradingDayCalculator
{
    /// <summary>
    /// Gets the current server time based on UTC and timezone offset
    /// </summary>
    /// <param name="timeZoneOffset">Timezone offset in hours (e.g., 2 for UTC+2)</param>
    /// <returns>Current server time</returns>
    public static DateTime GetServerTime(int timeZoneOffset)
    {
        return DateTime.UtcNow.AddHours(timeZoneOffset);
    }

    /// <summary>
    /// Gets the current trading day (date only) based on server timezone
    /// </summary>
    /// <param name="timeZoneOffset">Timezone offset in hours</param>
    /// <returns>Trading day date</returns>
    public static DateTime GetCurrentTradingDay(int timeZoneOffset)
    {
        var serverTime = GetServerTime(timeZoneOffset);
        return serverTime.Date;
    }

    /// <summary>
    /// Checks if the trading day has changed since the last reset date
    /// </summary>
    /// <param name="lastResetDate">Last reset date</param>
    /// <param name="timeZoneOffset">Timezone offset in hours</param>
    /// <returns>True if trading day has changed</returns>
    public static bool HasTradingDayChanged(DateTime? lastResetDate, int timeZoneOffset)
    {
        if (!lastResetDate.HasValue)
            return true;

        var currentTradingDay = GetCurrentTradingDay(timeZoneOffset);
        return currentTradingDay > lastResetDate.Value.Date;
    }

    /// <summary>
    /// Gets the start of the current trading day
    /// </summary>
    /// <param name="timeZoneOffset">Timezone offset in hours</param>
    /// <returns>Start of trading day in UTC</returns>
    public static DateTime GetTradingDayStart(int timeZoneOffset)
    {
        var tradingDay = GetCurrentTradingDay(timeZoneOffset);
        // Convert trading day start back to UTC
        return tradingDay.AddHours(-timeZoneOffset);
    }

    /// <summary>
    /// Gets the end of the current trading day
    /// </summary>
    /// <param name="timeZoneOffset">Timezone offset in hours</param>
    /// <returns>End of trading day in UTC</returns>
    public static DateTime GetTradingDayEnd(int timeZoneOffset)
    {
        return GetTradingDayStart(timeZoneOffset).AddDays(1);
    }
}
