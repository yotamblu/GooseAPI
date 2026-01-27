namespace GooseAPI
{
   
   
    public class Sleep
    {
        public string UserId { get; set; }
        public string UserAccessToken { get; set; }
        public string SummaryId { get; set; }
        public string CalendarDate { get; set; }
        public int DurationInSeconds { get; set; }
        public int TotalNapDurationInSeconds { get; set; }
        public long StartTimeInSeconds { get; set; }
        public int StartTimeOffsetInSeconds { get; set; }
        public int unmeasurableSleepInSeconds { get; set; }
        public int DeepSleepDurationInSeconds { get; set; }
        public int LightSleepDurationInSeconds { get; set; }
        public int RemSleepInSeconds { get; set; }
        public int AwakeDurationInSeconds { get; set; }
        public object SleepLevelsMap { get; set; }
        public string Validation { get; set; }
        public Dictionary<string, object> TimeOffsetSleepSpo2 { get; set; }
        public Dictionary<string, double> TimeOffsetSleepRespiration { get; set; }
        public SleepScore OverallSleepScore { get; set; }
        public Dictionary<string, Dictionary<string, string>> SleepScores { get; set; }
    }

   
    public class SleepScoreKVP
    {

        public string Key { get; set; }
        public SleepScore Value { get; set; }
    }


    public class SleepData
    {
        public string SummaryID { get; set; }
        public int SleepDurationInSeconds { get; set; }
        public long SleepStartTimeInSeconds { get; set; }
        public int SleepTimeOffsetInSeconds { get; set; }
        public string SleepDate { get; set; }
        public int DeepSleepDurationInSeconds { get; set; }
        public int LightSleepDurationInSeconds { get; set; }
        public int RemSleepInSeconds { get; set; }
        public int AwakeDurationInSeconds { get; set; }
        public Dictionary<string, Dictionary<string, string>> SleepScores { get; set; }
        public SleepScore OverallSleepScore { get; set; }
        public string SleepScoreQualifier;

    }

    public class Root
    {
        public List<Sleep> Sleeps { get; set; }
    }


   
}
