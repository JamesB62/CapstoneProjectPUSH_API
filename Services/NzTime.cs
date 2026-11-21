namespace CapstoneProjectPUSH_API.Services
{
    // Converts datetime to NZ datetime, then converts it to UTC -- since the server isn't in New Zealand
    public static class NzTime
    {
        // Gets the time zone for pacific/auckland -- falls back to UTC if not found
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

        // Start of today in NZ, converted to UTC datetime
        public static DateTime TodayStartUtc()
        {
            TimeZoneInfo nzTimeZone = GetTimeZone();
            DateTime nzToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, nzTimeZone).Date;
            DateTime nzMidnight = DateTime.SpecifyKind(nzToday, DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(nzMidnight, nzTimeZone);
        }
    }
}