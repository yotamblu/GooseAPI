using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using static GooseAPI.Controllers.PlannedWorkoutController;

namespace GooseAPI.Controllers
{
    [Route("/api/trainingSummary")]
    public class TrainingSummaryController : Controller
    {
        [HttpGet]
        public IActionResult Index([FromQuery]string apiKey, [FromQuery] string athleteName, [FromQuery] string startDate, [FromQuery] string endDate)
        {
            
            string caller = GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            User callerUser = GooseAPIUtils.GetUser(caller);
            if (callerUser == null 
                || (callerUser.Role == "athlete" && caller != athleteName)
                || (callerUser.Role == "coach" && !GooseAPIUtils.IsCoachingUser(caller, athleteName))) {

                return Unauthorized(
                   new {message = "You do not have the access to view this data"} 
                    );

            }


            FirebaseService firebaseService = new FirebaseService();

            GarminData athleteGarminData = firebaseService.GetData<GarminData>($"/GarminData/{athleteName}");
            if(athleteGarminData == null)
            {
                return BadRequest(new {message = "this user hasn't paired GooseNet with Garmin and so doesn't have a training summary"});
            }
            Dictionary<string,Workout> allRunningWorkouts = firebaseService.GetData<Dictionary<string, Workout>>($"/Activities/{athleteGarminData.userAccessToken}");

            return Ok(BuildTrainingSummary(startDate, endDate, allRunningWorkouts));




        }

        public TrainingSummary BuildTrainingSummary(string startDate, string endDate, Dictionary<string, Workout> workouts)
        {
            if (!DateTime.TryParseExact(startDate.Trim(), "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start))
                throw new ArgumentException("Invalid startDate format. Expected M/d/yyyy");
            if (!DateTime.TryParseExact(endDate.Trim(), "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end))
                throw new ArgumentException("Invalid endDate format. Expected M/d/yyyy");
            if (end < start)
                throw new ArgumentException("endDate must be after or equal to startDate.");

            // Cache parsed dates for each unique WorkoutDate string
            var dateCache = new Dictionary<string, DateTime>(workouts.Count);

            var filteredWorkouts = new List<Workout>(workouts.Count);
            double totalDistanceKm = 0;
            double totalTimeSec = 0;

            foreach (var workout in workouts.Values)
            {
                if (string.IsNullOrWhiteSpace(workout.WorkoutDate))
                    continue;

                if (!dateCache.TryGetValue(workout.WorkoutDate, out DateTime parsedDate))
                {
                    if (!DateTime.TryParseExact(workout.WorkoutDate.Trim(), "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                        continue; // skip invalid dates

                    dateCache[workout.WorkoutDate] = parsedDate;
                }

                if (parsedDate < start || parsedDate > end)
                    continue;

                filteredWorkouts.Add(workout);
                totalDistanceKm += workout.WorkoutDistanceInMeters / 1000.0;
                totalTimeSec += workout.WorkoutDurationInSeconds;
            }

            // Sort descending by date (newest first) using cached parsed dates
            filteredWorkouts.Sort((a, b) => dateCache[b.WorkoutDate].CompareTo(dateCache[a.WorkoutDate]));

            int totalDays = (end - start).Days + 1;

            return new TrainingSummary
            {
                startDate = start.ToShortDateString(),
                endDate = end.ToShortDateString(),
                distanceInKilometers = totalDistanceKm,
                averageDailyInKilometers = totalDays > 0 ? totalDistanceKm / totalDays : 0,
                timeInSeconds = totalTimeSec,
                averageDailyInSeconds = totalDays > 0 ? totalTimeSec / totalDays : 0,
                allWorkouts = filteredWorkouts
            };
        }

    }
}
