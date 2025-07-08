namespace GooseAPI
{
    public class Workout
    {
        public long WorkoutId { get; set; }
        public string WokroutName { get; set; }
        public int WorkoutDurationInSeconds { get; set; }
        public float WorkoutDistanceInMeters { get; set; }
        public int WorkoutAvgHR { get; set; }

        public float WorkoutAvgPaceInMinKm { get; set; }

        public List<FinalLap> WorkoutLaps { get; set; }
        public string WorkoutCoordsJsonStr { get; set; }
        public string WorkoutMapCenterJsonStr { get; set; }
        public double WorkoutMapZoom { get; set; }
        public string WorkoutDeviceName { get; set; }


        public string UserAccessToken { get; set; }


        public string WorkoutDate { get; set; }
    }


    public class FinalLap
    {

        public float LapDistanceInKilometers { get; set; }
        public int LapDurationInSeconds { get; set; }
        public float LapPaceInMinKm { get; set; }
    }


    public class WorkoutData
    {
        public string workoutName { get; set; }
        public string description { get; set; }

        public string sport = "RUNNING";

        public List<Interval> steps = new List<Interval>();



    }

    

}
