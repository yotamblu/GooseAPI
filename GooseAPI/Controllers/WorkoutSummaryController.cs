using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Reflection.Metadata;

namespace GooseAPI.Controllers
{
    [ApiController]
    [Route("api/workoutSummary")]
    public class WorkoutSummaryController : Controller
    {
        [HttpGet]
        public IActionResult Get(string athleteName, string apiKey, string date)
        {
            if(!IsAuthorized(athleteName,apiKey))return Unauthorized(new Message("You dont have the access to this data"));
            else
            {
                List<WorkoutSummary> workouts = new List<WorkoutSummary>(); 
                foreach(KeyValuePair<string,Workout> kvp in 
                    new FirebaseService().GetData<Dictionary<string, Workout>>($"Activities/{GooseAPIUtils.GetUserAccessToken(athleteName)}"))
                {
                    if(kvp.Value.WorkoutDate.Replace(" ","") ==  date)
                    {
                        workouts.Add(new WorkoutSummary
                        {
                            WorkoutDate = kvp.Value.WorkoutDate,
                            WorkoutAvgHR = kvp.Value.WorkoutAvgHR,
                            WorkoutCoordsJsonStr = kvp.Value.WorkoutCoordsJsonStr,
                            WorkoutDistanceInMeters = kvp.Value.WorkoutDistanceInMeters,
                            WorkoutDurationInSeconds = kvp.Value.WorkoutDurationInSeconds,
                            WorkoutId = kvp.Value.WorkoutId,
                            WorkoutName = kvp.Value.WokroutName,
                            WorkoutAvgPaceInMinKm = kvp.Value.WorkoutAvgPaceInMinKm,
                            AthleteName = athleteName,
                            ProfilePicData = GooseAPIUtils.GetUser(athleteName).ProfilePicString

                        }) ;
                    }
                }
                return Ok(workouts);
            }

        }
        [HttpGet("getWorkout")]
        public IActionResult GetWorkout([FromQuery] string userName,[FromQuery] string id)
        {
            FirebaseService firebaseService = new FirebaseService();
            GarminData data = firebaseService.GetData<GarminData>($"/GarminData/{userName}");
            if(data == null) {
                return BadRequest(new { message = "this user is not conneted with Garmin" });
            }
            Workout workout = firebaseService.GetData<Workout>($"/Activities/{data.userAccessToken}/{id}");
            if(workout == null)
            {
                return BadRequest(new { message = "no workout with this id was found" });
            }

            return Ok(workout);
        }

        [HttpGet("data")]
        public IActionResult GetWorkoutData([FromQuery] string workoutId, [FromQuery] string userName)
        {
            if(GooseAPIUtils.GetUser(userName) == null)
            {
                return BadRequest(new { message = "there is no such userName" });
            }
            

            FirebaseService service = new FirebaseService();
            GarminData garminData = service.GetData<GarminData>($"/GarminData/{userName}");
            if(garminData == null) {
                return BadRequest(new { message = "this athlete is not connected to garmin with GooseNet" });
            
            }
            Workout workout = service.GetData<Workout>($"/Activities/{garminData.userAccessToken}/{workoutId}");
            if(workout == null)
            {
                return BadRequest(new { message = "workout not found" });
            }
            return Ok(new WorkoutExtensiveData
            {
                dataSamples = workout.DataSamples,
                workoutLaps = workout.WorkoutLaps

            });
        }


        [HttpGet("feed")]
        public IActionResult GetWorkoutFeed(
     [FromQuery] string apiKey,
     [FromQuery] string athleteName,
     [FromQuery] string runningCursor = null,
     [FromQuery] string strengthCursor = null)
        {
            string caller = GooseAPI.GooseAPIUtils.FindUserNameByAPIKey(apiKey);

            if (string.IsNullOrEmpty(caller))
                return BadRequest(new { message = "there is no user with this apiKey" });

            if (string.IsNullOrEmpty(athleteName))
                return BadRequest(new { message = "athleteName is required" });

            bool allowed =
                caller == athleteName ||
                GooseAPI.GooseAPIUtils.IsCoachingUser(caller, athleteName);

            if (!allowed)
                return Unauthorized(new { message = "You are not allowed to access this athlete's workouts" });

            FirebaseService firebase = new FirebaseService();

            GarminData garmin =
                firebase.GetData<GarminData>($"/GarminData/{athleteName}");

            if (garmin == null)
                return BadRequest(new { message = "this athlete is not connected with Garmin" });

            DateTime? runningCursorDate = ParseCursorOrNull(runningCursor);
            DateTime? strengthCursorDate = ParseCursorOrNull(strengthCursor);

            const int pageSize = 10;

            // ---------------- RUNNING ----------------
            Dictionary<string, Workout> allRunning =
                firebase.GetData<Dictionary<string, Workout>>(
                    $"/Activities/{garmin.userAccessToken}"
                ) ?? new Dictionary<string, Workout>();

            List<(Workout w, DateTime d)> runningValid = new();

            foreach (Workout w in allRunning.Values)
            {
                if (!TryParseWorkoutDate(w.WorkoutDate, out DateTime d))
                    continue;

                d = d.Date;

                if (runningCursorDate != null && d >= runningCursorDate.Value)
                    continue;

                runningValid.Add((w, d));
            }

            List<Workout> runningWorkouts = runningValid
                .OrderByDescending(x => x.d)
                .Take(pageSize)
                .Select(x => x.w)
                .ToList();

            foreach (Workout w in runningWorkouts)
            {
                w.DataSamples = null;
                w.WorkoutLaps = null;
            }

            DateTime? nextRunningCursor =
                runningWorkouts.Count > 0
                    ? runningValid
                        .OrderByDescending(x => x.d)
                        .Take(pageSize)
                        .Last().d
                    : null;

            // ---------------- STRENGTH ----------------
            Dictionary<string, StrengthWorkout> allStrength =
                firebase.GetData<Dictionary<string, StrengthWorkout>>(
                    "/PlannedStrengthWorkouts"
                ) ?? new Dictionary<string, StrengthWorkout>();

            List<(StrengthWorkout sw, DateTime d)> strengthValid = new();

            foreach (StrengthWorkout sw in allStrength.Values)
            {
                if (sw?.WorkoutReviews == null)
                    continue;

                if (!sw.WorkoutReviews.Keys.Any(k =>
                    string.Equals(k, athleteName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (!TryParseWorkoutDate(sw.WorkoutDate, out DateTime d))
                    continue;

                d = d.Date;

                if (strengthCursorDate != null && d >= strengthCursorDate.Value)
                    continue;

                strengthValid.Add((sw, d));
            }

            List<StrengthWorkout> strengthWorkouts = strengthValid
                .OrderByDescending(x => x.d)
                .Take(pageSize)
                .Select(x => x.sw)
                .ToList();

            DateTime? nextStrengthCursor =
                strengthWorkouts.Count > 0
                    ? strengthValid
                        .OrderByDescending(x => x.d)
                        .Take(pageSize)
                        .Last().d
                    : null;

            return Ok(new
            {
                runningWorkouts = runningWorkouts,
                strengthWorkouts = strengthWorkouts,
                runningNextCursor = nextRunningCursor?.ToString("MM/dd/yyyy"),
                strengthNextCursor = nextStrengthCursor?.ToString("MM/dd/yyyy")
            });
        }


        private static DateTime? ParseCursorOrNull(string cursor)
        {
            if (string.IsNullOrWhiteSpace(cursor))
                return null;

            if (!TryParseWorkoutDate(cursor, out DateTime d))
                throw new ArgumentException("Invalid cursor date");

            return d.Date;
        }

        private static bool TryParseCompositeCursor(
       string cursor,
       out DateTime date,
       out string type,
       out string id)
        {
            date = default;
            type = null;
            id = null;

            if (string.IsNullOrWhiteSpace(cursor))
                return false;

            string[] parts = cursor.Split('|');
            if (parts.Length != 3)
                return false;

            if (!TryParseWorkoutDate(parts[0], out date))
                return false;

            type = parts[1];
            id = parts[2];

            return true;
        }




        private static bool TryParseWorkoutDate(string date, out DateTime result)
        {
            result = default;

            if (string.IsNullOrWhiteSpace(date))
                return false;

            string[] formats =
            {
        "M/d/yyyy",
        "M/dd/yyyy",
        "MM/d/yyyy",
        "MM/dd/yyyy"
    };

            return DateTime.TryParseExact(
                date.Trim(),
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result
            );
        }




        private bool IsAuthorized(string userName,string apiKey)
        {
            FirebaseService firebaseService = new FirebaseService();
            string requesterUserName = GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            if (requesterUserName == string.Empty)
            {
                return false;
            }
            if(requesterUserName == userName)
            {
                return true;
            }
            foreach (KeyValuePair<string, AthleteCoachConnection> conn in firebaseService.GetData<Dictionary<string, AthleteCoachConnection>>("AthleteCoachConnections"))
            {
                if (conn.Value.AthleteUserName == userName && conn.Value.CoachUserName == requesterUserName)
                {
                    return true;
                }


            }
            return false;

        }
    }
}
