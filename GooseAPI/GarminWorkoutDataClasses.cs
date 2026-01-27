namespace GooseAPI
{
    public class ActivityDetailsList
    {
        public ActivitySummary[] activityDetails { get; set; }
    }




    public class ActivitySummary
    {

        public string UserAccessToken { get; set; }

        public string SummaryId { get; set; }
        public long ActivityId { get; set; }
        public SummaryDetails Summary { get; set; }
        public List<Sample> Samples { get; set; }
        public List<Lap> Laps { get; set; }

    }

    public class SummaryDetails
    {
        public float AveragePaceInMinutesPerKilometer { get; set; }
        public long ActivityId { get; set; }
        public string ActivityName { get; set; }
        public int DurationInSeconds { get; set; }
        public long StartTimeInSeconds { get; set; }
        public int StartTimeOffsetInSeconds { get; set; }
        public string ActivityType { get; set; }
        public int AverageHeartRateInBeatsPerMinute { get; set; }
        public string DeviceName { get; set; }
        public int MaxHeartRateInBeatsPerMinute { get; set; }
        public bool IsWebUpload { get; set; }
        public float DistanceInMeters { get; set; }
    }

    public class Sample
    {

        public long StartTimeInSeconds { get; set; }
        public double ElevationInMeters { get; set; }
        public double AirTemperatureCelcius { get; set; }
        public int HeartRate { get; set; }
        public double SpeedMetersPerSecond { get; set; }
        public double StepsPerMinute { get; set; }
        public double TotalDistanceInMeters { get; set; }
        public int TimerDurationInSeconds { get; set; }
        public int ClockDurationInSeconds { get; set; }
        public int MovingDurationInSeconds { get; set; }
        public float latitudeInDegree { get; set; }
        public float longitudeInDegree { get; set; }
    }

    public class Lap
    {
        public long StartTimeInSeconds { get; set; }
    }
}
