using FireSharp;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace GooseAPI.Controllers
{
    [ApiController]

    public class PlannedWorkoutController : Controller
    {
        public class WorkoutStep
{
    public string targetType { get; set; }
    public int stepOrder { get; set; }
    public int repeatValue { get; set; }
    public string type { get; set; }
    public List<WorkoutStep> steps { get; set; }
    public string description { get; set; }
    public string durationType { get; set; }
    public double durationValue { get; set; }
    public string intensity { get; set; }
    public double targetValueLow { get; set; }
    public double targetValueHigh { get; set; }
    public string repeatType { get; set; }
}

public class Workout
{
    public string sport { get; set; }
    public List<WorkoutStep> steps { get; set; }
    public string workoutName { get; set; }
    public string description { get; set; }
}


        public static string FormatWorkoutJson(string json)
        {

            if (json.StartsWith("\"{") && json.EndsWith("}\""))
            {
                // First: Convert the outer string to actual JSON
                json = JsonConvert.DeserializeObject<string>(json);
            }
            Workout workout = JsonConvert.DeserializeObject<Workout>(json); 
            var sb = new StringBuilder();

            foreach (var step in workout.steps)
            {
                if (step.type == "WorkoutRepeatStep")
                {
                    sb.Append($"{step.repeatValue} * (");

                    var parts = new List<string>();
                    foreach (var subStep in step.steps)
                    {
                        if (subStep.durationType == "DISTANCE")
                        {
                            // Convert pace from m/s to min/km mm:ss
                            string paceStr = FormatPace(subStep.targetValueHigh);
                            string distanceStr = FormatDistance(subStep.durationValue);
                            if (subStep.intensity == "REST")
                            {
                                parts.Add($"{distanceStr} rest");
                            }
                            else
                            {
                                parts.Add($"{distanceStr} @ {paceStr}");
                            }
                        }
                        else if (subStep.durationType == "TIME")
                        {
                            string timeStr = FormatTime(subStep.durationValue);
                            if (subStep.intensity == "REST")
                            {
                                parts.Add($"{timeStr} rest");
                            }
                            else
                            {
                                parts.Add($"{timeStr}");
                            }
                        }
                    }

                    sb.Append(string.Join(", ", parts));
                    sb.AppendLine(")");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatPace(double metersPerSecond)
        {
            if (metersPerSecond <= 0) return "--:--";

            // pace in min/km = 16.6667 / m/s
            double paceMinPerKm = 16.6667 / metersPerSecond;
            int minutes = (int)paceMinPerKm;
            int seconds = (int)Math.Round((paceMinPerKm - minutes) * 60);

            if (seconds == 60)
            {
                minutes += 1;
                seconds = 0;
            }

            return $"{minutes:D2}:{seconds:D2}";
        }

        private static string FormatDistance(double meters)
        {
            if (meters >= 1000)
                return $"{meters / 1000:0.#}KM";
            else
                return $"{meters:0}M";
        }

        private static string FormatTime(double seconds)
        {
            int mins = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            return $"{mins:D2}:{secs:D2}";
        }


        private const string workoutPushUrl = "https://apis.garmin.com/training-api/workout";
        private const string workoutscheduleUrl = "https://apis.garmin.com/training-api/schedule";

        [HttpGet($"/api/plannedWorkout/byId")]
        public IActionResult GetPlannedWorkoutById([FromQuery] string id)
        {
            PlannedWorkout plannedWorkout = new FirebaseService().GetData<PlannedWorkout>($"/PlannedWorkouts/{id}");
            var response = new FirebaseClient(FireBaseConfig.config).Get($"/PlannedWorkoutsJSON/{id}");
            string plannedWorkoutJSON = response.Body;
            if (plannedWorkoutJSON == null )
            {
                return BadRequest(new {message = "no planned workout with this id was found"});
            }

            return Ok(new {worokutObject = plannedWorkout, plannedWorkoutJson = FormatWorkoutJson(plannedWorkoutJSON)});
        }


        [HttpGet($"/api/plannedWorkout/byDate")]
        public IActionResult GetPlannedWorkoutsByDate([FromQuery] string apiKey, [FromQuery] string athleteName, [FromQuery] string date)
        {
            User requestingUser = GooseAPIUtils.GetUser(GooseAPIUtils.FindUserNameByAPIKey(apiKey));
            User athleteUser = GooseAPIUtils.GetUser(athleteName);
            if (requestingUser == null || athleteUser == null || athleteUser.Role != "athlete" ||!(GooseAPIUtils.IsCoachingUser(requestingUser.UserName, athleteName) || requestingUser.UserName == athleteName))
            {
                return Unauthorized(new { message = "you are not authorized to view workouts for this user" });    
            }

            FirebaseService firebaseService = new FirebaseService();
            List<PlannedWorkout> plannedWorkouts = new List<PlannedWorkout>();
            Dictionary<string, PlannedWorkout> allPlannedWorkouts = firebaseService.GetData<Dictionary<string, PlannedWorkout>>("/PlannedWorkouts");
            foreach(KeyValuePair<string, PlannedWorkout> kvp in allPlannedWorkouts)
            {
                PlannedWorkout currentWorkout = kvp.Value;
                if(currentWorkout.AthleteNames.Contains(athleteName) && currentWorkout.Date == date)
                {
                    plannedWorkouts.Add(currentWorkout);
                    currentWorkout.workoutId = kvp.Key;
                }
            }



            return Ok(plannedWorkouts);
        }


        [HttpPost($"/api/addWorkout")]
        public IActionResult AddWorkout([FromBody] PlannedWorkoutData workoutData, [FromQuery] string apikey)
        {
            // if for singular athlete
            if (!workoutData.isFlock)
            {
                if (!GooseAPIUtils.IsCoachingUser(GooseAPIUtils.FindUserNameByAPIKey(apikey), workoutData.targetName))
                {
                    return BadRequest(new { message = "Only coaches can send workouts to athletes" });
                }

                GarminData garminData = GooseAPIUtils.GetUserAccessTokenAndSecret(workoutData.targetName);


                PushWorkoutToGarminConnect(
                    workoutData.jsonBody,
                    workoutData.date,
                    garminData.userAccessToken,
                    garminData.userAccessTokenSecret
                    );

                StorePlannedWorkout(workoutData.jsonBody, workoutData.date, workoutData.targetName, GooseAPIUtils.FindUserNameByAPIKey(apikey), false);


            }
            //if flock
            else
            {
                //push to all relevant Garmin Users

                FirebaseService service =  new FirebaseService();
                List<string> athleteNames =
                    service.
                     GetData<List<string>>($"/Flocks/{GooseAPIUtils.FindUserNameByAPIKey(apikey)}/{workoutData.targetName}/athletesUserNames");


                foreach (string athleteName in athleteNames)
                {
                    GarminData garminData = service.GetData<GarminData>($"/GarminData/{athleteName}");
                    if(garminData != null)
                    {
                        PushWorkoutToGarminConnect(workoutData.jsonBody, workoutData.date, garminData.userAccessToken, garminData.userAccessTokenSecret);
                    }
                    
                }
                StorePlannedWorkout(workoutData.jsonBody, workoutData.date, workoutData.targetName,GooseAPIUtils.FindUserNameByAPIKey(apikey) ,true);

            }




            return Ok(new {message = "workout pushed successfully"});
        }

        private void StorePlannedWorkout(string jsonBody, string workoutDate,string targetName,string coachName,bool isFlock)
        {
            string plannedWorkoutId = GooseAPIUtils.GenerateShortHexId();
            List<string> athleteNames = new List<string>();

            if (isFlock)
            {
                athleteNames = new FirebaseService().GetData<List<string>>($"Flocks/{coachName}/{targetName}/athletesUserNames");
            }
            else
            {
                athleteNames.Add(targetName);
            }
            Console.WriteLine(workoutDate);

            WorkoutData workout = JsonConvert.DeserializeObject<WorkoutData>(jsonBody);
            PlannedWorkout plannedWorkout = new PlannedWorkout
            {
                Intervals = workout.steps,
                AthleteNames = athleteNames,
                CoachName = coachName,
                Date = GooseAPIUtils.RemoveLeadingZeros(GooseAPIUtils.ConvertDateFormat(workoutDate)),
                Description = workout.description,
                WorkoutName = workout.workoutName,
            };

            FirebaseService service = new FirebaseService();
            service.InsertData($"PlannedWorkoutsJSON/{plannedWorkoutId}", jsonBody);
            service.InsertData($"PlannedWorkouts/{plannedWorkoutId}", plannedWorkout);






        }


        private void PushWorkoutToGarminConnect(string jsonBody, string workoutDate,string userAccessToken,string userAccessTokenSecret) {
            Dictionary<string, string> garminApiCreds = GooseAPIUtils.GetGarminAPICredentials();
            string consumerKey = garminApiCreds["ConsumerKey"];
            string consumerSecret = garminApiCreds["ConsumerSecret"];
            
            var oauthParams = new Dictionary<string, string>
            {
            { "oauth_consumer_key", consumerKey },
            { "oauth_token", userAccessToken },
            { "oauth_nonce", Guid.NewGuid().ToString("N") }, // Unique nonce
            { "oauth_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }, // Timestamp
            { "oauth_signature_method", "HMAC-SHA1" },
            { "oauth_version", "1.0" }
            };

            // Generate the OAuth signature using HMAC-SHA1
            string signature = GenerateOAuthSignature(workoutPushUrl, oauthParams, consumerSecret, userAccessTokenSecret);

            // Add the signature to the OAuth parameters
            oauthParams.Add("oauth_signature", signature);

            // Create the OAuth header
            string oauthHeader = "OAuth " + string.Join(", ", oauthParams.Select(p => $"{p.Key}=\"{Uri.EscapeDataString(p.Value)}\""));

            // Create an HttpClient to send the request
            var httpClient = new HttpClient();

            // Set up the POST request content
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Add OAuth and other headers to the request
            httpClient.DefaultRequestHeaders.Add("Authorization", oauthHeader);


            // Make the POST request
            var response = httpClient.PostAsync(workoutPushUrl, content).GetAwaiter().GetResult();

            //Part 2 - put the workout on the calendar

            // Read the response content
            var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            JObject jsonObject = JObject.Parse(responseContent);
            Console.WriteLine(responseContent);
            // Extract workoutId as an integer
            int workoutId = (int)jsonObject["workoutId"];

            // Output the response status and content
           
            var jsonBodySchedule = new Dictionary<string, object>
            {
                { "workoutId", workoutId},
                {"date",workoutDate }
            };

            oauthParams = new Dictionary<string, string>
        {
            { "oauth_consumer_key", consumerKey },
            { "oauth_token", userAccessToken },
            { "oauth_nonce", Guid.NewGuid().ToString("N") }, // Unique nonce
            { "oauth_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }, // Timestamp
            { "oauth_signature_method", "HMAC-SHA1" },
            { "oauth_version", "1.0" }
        };

            signature = GenerateOAuthSignature(workoutscheduleUrl, oauthParams, consumerSecret, userAccessTokenSecret);

            // Add the signature to the OAuth parameters
            oauthParams.Add("oauth_signature", signature);

            oauthHeader = "OAuth " + string.Join(", ", oauthParams.Select(p => $"{p.Key}=\"{Uri.EscapeDataString(p.Value)}\""));


            HttpClient httpClient2 = new HttpClient();

            var content2 = new StringContent(JsonConvert.SerializeObject(jsonBodySchedule), Encoding.UTF8, "application/json");


            httpClient2.DefaultRequestHeaders.Add("Authorization", oauthHeader);


            var response2 = httpClient2.PostAsync(workoutscheduleUrl, content2).GetAwaiter().GetResult();

        }


        static string GenerateOAuthSignature(string url, Dictionary<string, string> oauthParams, string consumerSecret, string tokenSecret)
        {
            // Build the base string
            var baseString = BuildBaseString(url, oauthParams);

            // Create the signing key (consumer secret + "&" + token secret)
            string signingKey = Uri.EscapeDataString(consumerSecret) + "&" + Uri.EscapeDataString(tokenSecret);

            // Create HMAC-SHA1 signature
            using (var hmacsha1 = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey)))
            {
                byte[] hashBytes = hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(baseString));
                return Convert.ToBase64String(hashBytes);
            }
        }

        static string BuildBaseString(string url, Dictionary<string, string> oauthParams)
        {
            // Sort parameters by name
            var sortedParams = oauthParams.OrderBy(p => p.Key)
                                          .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}")
                                          .ToList();

            // Create the query string (oauth parameters)
            string baseString = string.Join("&", sortedParams);
            baseString = $"POST&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(baseString)}";

            return baseString;
        }

    }



}
