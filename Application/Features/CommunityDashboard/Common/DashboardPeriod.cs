namespace Application.Features.CommunityDashboard.Common;

public static class DashboardPeriod
{
    public static DateTime WeekStart(DateTime utc) => utc.Date.AddDays(-(((int)utc.DayOfWeek + 6) % 7));

    public static (DateTime From, DateTime ToExclusive) Range(DateTime? from, DateTime? to)
    {
        var now = DateTime.UtcNow;
        var start = from?.Date ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = to?.Date ?? now.Date;
        if (start > end || end >= DateTime.MaxValue.Date || (end - start).TotalDays > 366) throw DashboardAuthorization.Invalid();
        return (DateTime.SpecifyKind(start, DateTimeKind.Utc), DateTime.SpecifyKind(end.AddDays(1), DateTimeKind.Utc));
    }
}
