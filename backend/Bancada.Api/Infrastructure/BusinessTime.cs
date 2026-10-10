namespace Bancada.Api.Infrastructure;

public static class BusinessTime
{
    // PostgreSQL timestamps persist microseconds, not .NET's 100 ns ticks.
    public static DateTimeOffset UtcNow => Normalize(DateTimeOffset.UtcNow);
    public static DateTimeOffset Normalize(DateTimeOffset value) =>
        new(value.UtcTicks - value.UtcTicks % 10, TimeSpan.Zero);
}
