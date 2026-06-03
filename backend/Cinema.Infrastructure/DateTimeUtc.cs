namespace Cinema.Infrastructure;

internal static class DateTimeUtc
{
    public static DateTime Mark(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
