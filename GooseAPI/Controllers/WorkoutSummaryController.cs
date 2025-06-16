using Microsoft.AspNetCore.Mvc;

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
                            WorkoutAvgPaceInMinKm = kvp.Value.WorkoutAvgPaceInMinKm

                        });
                    }
                }
                return Ok(workouts);
            }

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
