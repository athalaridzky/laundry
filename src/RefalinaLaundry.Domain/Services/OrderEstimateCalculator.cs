namespace RefalinaLaundry.Domain.Services;

public static class OrderEstimateCalculator
{
    public static DateTime? CalculateUtc(
        DateTime receivedAtUtc,
        IEnumerable<int?> durationDays,
        TimeZoneInfo businessTimeZone)
    {
        int[] configuredDurations = durationDays.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        if (configuredDurations.Length == 0)
        {
            return null;
        }

        int longestDuration = configuredDurations.Max();
        DateTime normalizedReceivedAt = receivedAtUtc.Kind == DateTimeKind.Utc
            ? receivedAtUtc
            : DateTime.SpecifyKind(receivedAtUtc, DateTimeKind.Utc);

        if (longestDuration > 0)
        {
            return normalizedReceivedAt.AddDays(longestDuration);
        }

        DateTime localReceivedAt = TimeZoneInfo.ConvertTimeFromUtc(normalizedReceivedAt, businessTimeZone);
        DateTime endOfLocalDay = localReceivedAt.Date.AddDays(1).AddMilliseconds(-1);
        return TimeZoneInfo.ConvertTimeToUtc(endOfLocalDay, businessTimeZone);
    }
}
