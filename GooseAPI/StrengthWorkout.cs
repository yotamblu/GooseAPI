namespace GooseAPI
{
    public class StrengthWorkout
    {
        public string CoachName { get; set; }
        public string WorkoutName { get; set; }
        public string WorkoutDescription { get; set; }
        public string WorkoutDate { get; set; }
        public List<StrengthWorkoutDrill> WorkoutDrills { get; set; }

        public List<string> AthleteNames { get; set; }

        public Dictionary<string, StrengthWorkoutReview> WorkoutReviews { get; set; }
        public string WorkoutId { get; set; }

    }
}
