namespace GooseAPI
{
    public class PlannedWorkout
    {
        public string Date { get; set; }
        public string WorkoutName { get; set; }
        public string Description { get; set; }
        public List<Interval> Intervals { get; set; }
        public string CoachName { get; set; }
        public List<string> AthleteNames { get; set; }



    }


    public class Interval
    {
        public int stepOrder { get; set; }
        public int repeatValue { get; set; }
        public string type { get; set; }
        public List<Interval> steps { get; set; }
        public string description { get; set; }

        public string durationType { get; set; }
        public double durationValue { get; set; }
        public string intensity { get; set; }

        public string targetType = "PACE";

        public double targetValueLow { get; set; }
        public double targetValueHigh { get; set; }


        public string repeatType { get; set; }

    }

}
