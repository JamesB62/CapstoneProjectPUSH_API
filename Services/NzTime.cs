namespace CapstoneProjectPUSH_API.Services
{
    // The server isn't in New Zealand, so "today" is worked out in NZ time and converted to UTC
    public static class NzTime
    {
        public static TimeZoneInfo GetTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland");
            }
            catch (Exception)
            {
                return TimeZoneInfo.Utc;
            }
        }

        // Start of today in NZ, as a UTC time
        public static DateTime TodayStartUtc()
        {
            TimeZoneInfo nzTimeZone = GetTimeZone();
            DateTime nzToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, nzTimeZone).Date;
            DateTime nzMidnight = DateTime.SpecifyKind(nzToday, DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(nzMidnight, nzTimeZone);
        }
    }
}
