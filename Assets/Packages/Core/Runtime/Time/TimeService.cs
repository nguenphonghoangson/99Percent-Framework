using System;

namespace NinetyNine.Core
{
    /// <summary>
    ///     Wall-clock time in UTC. Features compare whole UTC days rather than local dates so time zones and
    ///     DST cannot move a daily boundary. A server-time implementation plugs in behind the same contract.
    /// </summary>
    public interface ITimeService
    {
        long UtcNowSeconds { get; }

        /// <summary>Days since the Unix epoch, UTC.</summary>
        long UtcDay { get; }
    }

    public static class TimeUnits
    {
        public const long SecondsPerDay = 86400;

        public static long ToUtcDay(long utcSeconds) => (long)Math.Floor(utcSeconds / (double)SecondsPerDay);
    }

    public sealed class SystemTimeService : ITimeService
    {
        public long UtcNowSeconds => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public long UtcDay => TimeUnits.ToUtcDay(UtcNowSeconds);
    }

    /// <summary>Settable clock for tests and dev cheats.</summary>
    public sealed class ManualTimeService : ITimeService
    {
        public ManualTimeService(long utcNowSeconds = 0) => UtcNowSeconds = utcNowSeconds;

        public long UtcNowSeconds { get; set; }

        public long UtcDay => TimeUnits.ToUtcDay(UtcNowSeconds);

        public void AdvanceDays(int days) => UtcNowSeconds += days * TimeUnits.SecondsPerDay;
    }
}
