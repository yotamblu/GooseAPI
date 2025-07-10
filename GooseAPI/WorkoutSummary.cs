namespace GooseAPI
{
    public class WorkoutSummary
    {
        public string WorkoutName {  get; set; }
        public long WorkoutId { get; set; }

        public int WorkoutDurationInSeconds { get; set; }
        public float WorkoutDistanceInMeters { get; set; }
        public int WorkoutAvgHR { get; set; }

        public float WorkoutAvgPaceInMinKm { get; set; }
        public string WorkoutCoordsJsonStr { get; set; }

        public string WorkoutDate { get; set; }
        
        public string ProfilePicData { get; set; }
        public string AthleteName {  get; set; }


    }
}
