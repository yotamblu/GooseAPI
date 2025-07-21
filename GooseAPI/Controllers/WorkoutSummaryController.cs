using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;

namespace GooseAPI.Controllers
{
    [ApiController]
    [Route("api/workoutSummary")]
    public class WorkoutSummaryController : Controller
    {
        [HttpGet]
        public IActionResult Get(string userName, string apiKey, string date)
        {
            if(!IsAuthorized(userName,apiKey))return Unauthorized(new Message("You dont have the access to this data"));
            else
            {
                List<WorkoutSummary> workouts = new List<WorkoutSummary>(); 
                foreach(KeyValuePair<string,Workout> kvp in 
                    new FirebaseService().GetData<Dictionary<string, Workout>>($"Activities/{GooseAPIUtils.GetUserAccessToken(userName)}"))
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
                            AthleteName = userName,
                            ProfilePicData = GooseAPIUtils.GetUser(userName).ProfilePicString

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
