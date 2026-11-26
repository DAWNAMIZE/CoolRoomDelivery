using System;
using System.Globalization;

namespace ColdRoom.Core
{
    /// <summary>Formatting helpers for the UTC ISO timestamps used by the API.</summary>
    public static class TimeText
    {
        public static string Age(string iso)
        {
            if (!TryParse(iso, out DateTime utc))
            {
                return "just now";
            }

            TimeSpan span = DateTime.UtcNow - utc;
            if (span.TotalMinutes < 1)
            {
                return "just now";
            }

            if (span.TotalMinutes < 60)
            {
                return $"{(int)span.TotalMinutes} min";
            }

            if (span.TotalHours < 24)
            {
                return $"{(int)span.TotalHours} hr";
            }

            return $"{(int)span.TotalDays} d";
        }

        public static string Clock(string iso)
        {
            if (!TryParse(iso, out DateTime utc))
            {
                return string.Empty;
            }

            return LagosTime.ToLagos(utc).ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        public static string Date(string iso)
        {
            if (!TryParse(iso, out DateTime utc))
            {
                return string.Empty;
            }

            return LagosTime.ToLagos(utc).ToString("dd MMM", CultureInfo.InvariantCulture);
        }

        private static bool TryParse(string iso, out DateTime utc)
        {
            if (DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                utc = parsed.Kind == DateTimeKind.Local
                    ? parsed.ToUniversalTime()
                    : DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                return true;
            }

            utc = default;
            return false;
        }
    }
}
