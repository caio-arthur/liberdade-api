using System;
using System.Runtime.InteropServices;

namespace Application.Common.Helpers
{
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo _brazilTimeZone = GetBrazilTimeZone();

        private static TimeZoneInfo GetBrazilTimeZone()
        {
            var windowsId = "E. South America Standard Time";
            var ianaId = "America/Sao_Paulo";

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                }
                else
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(ianaId);
                }
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback para UTC se o fuso horário não for encontrado
                return TimeZoneInfo.Utc;
            }
            catch (InvalidTimeZoneException)
            {
                // Fallback para UTC se o fuso horário for inválido
                return TimeZoneInfo.Utc;
            }
        }

        public static DateTime UtcToBrazilLocalTime(this DateTime utcDateTime)
        {
            // Garantir que estamos tratando tempo em UTC.
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }

            // Se o fuso retornado for UTC (fallback), o método retornará o mesmo instante.
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _brazilTimeZone);
        }

    }
}
