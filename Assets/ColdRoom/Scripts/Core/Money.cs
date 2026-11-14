using System.Globalization;

namespace ColdRoom.Core
{
    /// <summary>Money is always stored as an integer number of kobo. 100 kobo = NGN 1.</summary>
    public static class Money
    {
        public const long KoboPerNaira = 100;

        public static string Format(long kobo)
        {
            decimal naira = kobo / (decimal)KoboPerNaira;
            return "\u20A6" + naira.ToString(naira == decimal.Truncate(naira) ? "N0" : "N2", CultureInfo.InvariantCulture);
        }

        public static string FormatCompact(long kobo)
        {
            decimal naira = kobo / (decimal)KoboPerNaira;
            if (naira >= 1_000_000m)
            {
                return "\u20A6" + (naira / 1_000_000m).ToString("0.#", CultureInfo.InvariantCulture) + "m";
            }

            if (naira >= 1_000m)
            {
                return "\u20A6" + (naira / 1_000m).ToString("0.#", CultureInfo.InvariantCulture) + "k";
            }

            return Format(kobo);
        }

        public static long FromNaira(decimal naira) => (long)(naira * KoboPerNaira);
    }
}
