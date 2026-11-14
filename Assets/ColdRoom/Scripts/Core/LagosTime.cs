using System;
using System.Globalization;

namespace ColdRoom.Core
{
    /// <summary>
    /// All persisted timestamps are UTC. The app displays them in Africa/Lagos (UTC+1, no DST).
    /// Lagos has no daylight-saving transitions, so a fixed offset is both correct and reliable
    /// on WebGL where the runtime timezone database may be incomplete.
    /// </summary>
    public static class LagosTime
    {
        public static readonly TimeSpan Offset = TimeSpan.FromHours(1);

        public static DateTime Now => DateTime.UtcNow + Offset;

        public static DateTime ToLagos(DateTime utc)
        {
            if (utc.Kind == DateTimeKind.Local)
            {
                utc = utc.ToUniversalTime();
            }
            else if (utc.Kind == DateTimeKind.Unspecified)
            {
                utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            }

            return utc + Offset;
        }

        public static DateTime ToUtc(DateTime lagos)
        {
            return DateTime.SpecifyKind(lagos - Offset, DateTimeKind.Utc);
        }

        public static string Format(DateTime utc, string format = "ddd d MMM \u00B7 HH:mm")
        {
            return ToLagos(utc).ToString(format, CultureInfo.InvariantCulture);
        }

        public static string Clock => Now.ToString("ddd d MMM \u00B7 HH:mm", CultureInfo.InvariantCulture);
    }
}
