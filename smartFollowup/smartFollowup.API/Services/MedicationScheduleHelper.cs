namespace SmartFollowUp.API.Services
{
    public static class MedicationScheduleHelper
    {
        private static readonly TimeSpan DayStart = new TimeSpan(8, 0, 0);  // 8:00 AM
        private static readonly TimeSpan DayEnd = new TimeSpan(22, 0, 0);   // 10:00 PM

        // Evenly spaces doses between 8:00 AM and 10:00 PM
        public static List<TimeSpan> ComputeDoseTimes(int timesPerDay)
        {
            if (timesPerDay <= 1)
                return new List<TimeSpan> { new TimeSpan(9, 0, 0) };

            var span = DayEnd - DayStart;
            var times = new List<TimeSpan>();
            for (int i = 0; i < timesPerDay; i++)
            {
                var t = DayStart + TimeSpan.FromTicks(span.Ticks * i / (timesPerDay - 1));
                // Round to nearest 5 minutes for a cleaner display
                var minutes = Math.Round(t.TotalMinutes / 5.0) * 5;
                times.Add(TimeSpan.FromMinutes(minutes));
            }
            return times;
        }

        public static List<string> ComputeDoseTimesFormatted(int timesPerDay)
        {
            return ComputeDoseTimes(timesPerDay)
                .Select(t => DateTime.Today.Add(t).ToString("h:mm tt"))
                .ToList();
        }

        public static string BuildFrequencyLabel(int timesPerDay)
        {
            var times = ComputeDoseTimesFormatted(timesPerDay);
            var timesText = timesPerDay == 1 ? "Once a day" : $"{timesPerDay} times a day";
            return $"{timesText} ({string.Join(", ", times)})";
        }
    }
}
