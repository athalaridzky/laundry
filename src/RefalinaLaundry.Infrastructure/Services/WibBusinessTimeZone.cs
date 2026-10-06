using RefalinaLaundry.Application.Abstractions;

namespace RefalinaLaundry.Infrastructure.Services;

public sealed class WibBusinessTimeZone : IBusinessTimeZone
{
    public WibBusinessTimeZone()
    {
        TimeZone = ResolveTimeZone();
    }

    public TimeZoneInfo TimeZone { get; }

    public DateTime ToLocal(DateTime utcDateTime)
    {
        DateTime utc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);
    }

    public DateTime ToUtc(DateTime localDateTime)
    {
        DateTime unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, TimeZone);
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
        }
    }
}
