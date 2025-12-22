using FireSharp;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GooseAPI.Controllers
{
    [ApiController]
    public class PlannedWorkoutController : Controller
    {
        // ===================== GARMIN JSON MODELS =====================

        public class WorkoutStep
        {
            public int stepOrder { get; set; }
            public int repeatValue { get; set; }
            public string type { get; set; }
            public List<WorkoutStep> steps { get; set; }
            public string description { get; set; }

            public string durationType { get; set; }
            public double durationValue { get; set; }
            public string intensity { get; set; }

            public string targetType { get; set; }
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

        // ===================== FORMATTER =====================

        public static string FormatWorkoutJson(string json)
        {
            try
            {
                if (json.StartsWith("\"{") && json.EndsWith("}\""))
                    json = JsonConvert.DeserializeObject<string>(json);

                Workout workout = JsonConvert.DeserializeObject<Workout>(json);
                if (workout?.steps == null)
                    return "Workout format error";

                var sb = new StringBuilder();

                foreach (var step in workout.steps)
                {
                    if (step.type != "WorkoutRepeatStep")
                        continue;

                    int repeatCount = step.repeatValue > 0 ? step.repeatValue : 1;
                    sb.Append($"{repeatCount} * (");

                    if (step.steps == null || step.steps.Count == 0)
                    {
                        sb.AppendLine(")");
                        continue;
                    }

                    var parts = new List<string>();

                    foreach (var sub in step.steps)
                    {
                        if (sub.durationType == "DISTANCE")
                        {
                            string dist = FormatDistance(sub.durationValue);
                            if (sub.intensity == "REST")
                                parts.Add($"{dist} rest");
                            else
                                parts.Add($"{dist} @ {FormatPace(sub.targetValueHigh)}");
                        }
                        else if (sub.durationType == "TIME")
                        {
                            string time = FormatTime(sub.durationValue);
                            if (sub.intensity == "REST")
                                parts.Add($"{time} rest");
                            else
                                parts.Add($"{time} @ {FormatPace(sub.targetValueHigh)}");
                        }
                    }

                    sb.Append(string.Join(", ", parts));
                    sb.AppendLine(")");
                }

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return "Workout format error";
            }
        }

        private static string FormatPace(double minPerKm)
        {
            if (minPerKm <= 0) return "--:--";
            int min = (int)minPerKm;
            int sec = (int)Math.Round((minPerKm - min) * 60);
            if (sec == 60) { min++; sec = 0; }
            return $"{min:D2}:{sec:D2}";
        }

        private static string FormatDistance(double meters)
        {
            return meters >= 1000 ? $"{meters / 1000:0.#}KM" : $"{meters:0}M";
        }

        private static string FormatTime(double seconds)
        {
            int min = (int)(seconds / 60);
            int sec = (int)(seconds % 60);
            return $"{min:D2}:{sec:D2}";
        }

        // ===================== STEP → INTERVAL =====================

        private static List<Interval> ConvertStepsToIntervals(List<WorkoutStep> steps)
        {
            if (steps == null)
                return new List<Interval>();

            return steps.Select(step => new Interval
            {
                stepOrder = step.stepOrder,
                repeatValue = step.repeatValue,
                type = step.type,
                description = step.description,
                durationType = step.durationType,
                durationValue = step.durationValue,
                intensity = step.intensity,
                targetValueLow = step.targetValueLow,
                targetValueHigh = step.targetValueHigh,
                repeatType = step.repeatType,
                steps = ConvertStepsToIntervals(step.steps)
            }).ToList();
        }

        // ===================== ENDPOINTS =====================

        private const string workoutPushUrl = "https://apis.garmin.com/training-api/workout";
        private const string workoutscheduleUrl = "https://apis.garmin.com/training-api/schedule";

        [HttpPost("/api/addWorkout")]
        public IActionResult AddWorkout([FromBody] PlannedWorkoutData workoutData, [FromQuery] string apikey)
        {
            if (!workoutData.isFlock)
            {
                if (!GooseAPIUtils.IsCoachingUser(
                    GooseAPIUtils.FindUserNameByAPIKey(apikey),
                    workoutData.targetName))
                {
                    return BadRequest(new { message = "Only coaches can send workouts to athletes" });
                }

                GarminData garminData = GooseAPIUtils.GetUserAccessTokenAndSecret(workoutData.targetName);

                PushWorkoutToGarminConnectStrict(
                    workoutData.jsonBody,
                    workoutData.date,
                    garminData);

                StorePlannedWorkout(
                    workoutData.jsonBody,
                    workoutData.date,
                    workoutData.targetName,
                    GooseAPIUtils.FindUserNameByAPIKey(apikey),
                    false);
            }
            else
            {
                FirebaseService service = new FirebaseService();

                var athleteNames =
                    service.GetData<List<string>>(
                        $"/Flocks/{GooseAPIUtils.FindUserNameByAPIKey(apikey)}/{workoutData.targetName}/athletesUserNames")
                    ?? new List<string>();

                foreach (string athlete in athleteNames)
                {
                    GarminData garminData = service.GetData<GarminData>($"/GarminData/{athlete}");
                    if (garminData == null)
                        continue;

                    try
                    {
                        PushWorkoutToGarminConnectLenient(
                            workoutData.jsonBody,
                            workoutData.date,
                            garminData,
                            athlete);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Garmin push failed for {athlete}: {ex.Message}");
                    }
                }

                StorePlannedWorkout(
                    workoutData.jsonBody,
                    workoutData.date,
                    workoutData.targetName,
                    GooseAPIUtils.FindUserNameByAPIKey(apikey),
                    true);
            }

            return Ok(new { message = "workout pushed successfully" });
        }

        // ===================== STORAGE =====================

        private void StorePlannedWorkout(
            string jsonBody,
            string workoutDate,
            string targetName,
            string coachName,
            bool isFlock)
        {
            string plannedWorkoutId = GooseAPIUtils.GenerateShortHexId();
            List<string> athleteNames = new List<string>();

            if (isFlock)
            {
                athleteNames = new FirebaseService()
                    .GetData<List<string>>($"Flocks/{coachName}/{targetName}/athletesUserNames")
                    ?? new List<string>();
            }
            else
            {
                athleteNames.Add(targetName);
            }

            Workout workout = JsonConvert.DeserializeObject<Workout>(jsonBody);

            PlannedWorkout plannedWorkout = new PlannedWorkout
            {
                Intervals = ConvertStepsToIntervals(workout.steps),
                AthleteNames = athleteNames,
                CoachName = coachName,
                Date = GooseAPIUtils.RemoveLeadingZeros(
                    GooseAPIUtils.ConvertDateFormat(workoutDate)),
                Description = workout.description,
                WorkoutName = workout.workoutName
            };

            FirebaseService service = new FirebaseService();
            service.InsertData($"PlannedWorkoutsJSON/{plannedWorkoutId}", jsonBody);
            service.InsertData($"PlannedWorkouts/{plannedWorkoutId}", plannedWorkout);
        }

        // ===================== GARMIN PUSH =====================

        private void PushWorkoutToGarminConnectStrict(
            string jsonBody,
            string workoutDate,
            GarminData garminData)
        {
            if (string.IsNullOrWhiteSpace(garminData?.userAccessToken) ||
                string.IsNullOrWhiteSpace(garminData?.userAccessTokenSecret))
            {
                throw new Exception("Garmin OAuth credentials missing");
            }

            PushWorkoutInternal(jsonBody, workoutDate,
                garminData.userAccessToken,
                garminData.userAccessTokenSecret);
        }

        private void PushWorkoutToGarminConnectLenient(
            string jsonBody,
            string workoutDate,
            GarminData garminData,
            string athlete)
        {
            if (string.IsNullOrWhiteSpace(garminData?.userAccessToken) ||
                string.IsNullOrWhiteSpace(garminData?.userAccessTokenSecret))
            {
                Console.WriteLine($"Skipping {athlete}: missing Garmin OAuth secret");
                return;
            }

            PushWorkoutInternal(jsonBody, workoutDate,
                garminData.userAccessToken,
                garminData.userAccessTokenSecret);
        }

        private void PushWorkoutInternal(
            string jsonBody,
            string workoutDate,
            string userAccessToken,
            string userAccessTokenSecret)
        {
            var creds = GooseAPIUtils.GetGarminAPICredentials();

            var oauthParams = BuildOAuthParams(creds["ConsumerKey"], userAccessToken);
            oauthParams.Add("oauth_signature",
                GenerateOAuthSignature(workoutPushUrl, oauthParams,
                    creds["ConsumerSecret"], userAccessTokenSecret));

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", BuildOAuthHeader(oauthParams));

            var response = client.PostAsync(
                workoutPushUrl,
                new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                .GetAwaiter().GetResult();

            JObject json = JObject.Parse(
                response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

            int workoutId = (int)json["workoutId"];

            var scheduleBody = new { workoutId, date = workoutDate };

            oauthParams = BuildOAuthParams(creds["ConsumerKey"], userAccessToken);
            oauthParams.Add("oauth_signature",
                GenerateOAuthSignature(workoutscheduleUrl, oauthParams,
                    creds["ConsumerSecret"], userAccessTokenSecret));

            using var client2 = new HttpClient();
            client2.DefaultRequestHeaders.Add("Authorization", BuildOAuthHeader(oauthParams));
            client2.PostAsync(
                workoutscheduleUrl,
                new StringContent(JsonConvert.SerializeObject(scheduleBody),
                Encoding.UTF8, "application/json"))
                .GetAwaiter().GetResult();
        }

        // ===================== OAUTH HELPERS =====================

        private static Dictionary<string, string> BuildOAuthParams(string consumerKey, string token)
        {
            if (string.IsNullOrWhiteSpace(consumerKey))
                throw new Exception("Garmin consumer key missing");

            if (string.IsNullOrWhiteSpace(token))
                throw new Exception("Garmin oauth token missing");

            return new Dictionary<string, string>
            {
                { "oauth_consumer_key", consumerKey },
                { "oauth_token", token },
                { "oauth_nonce", Guid.NewGuid().ToString("N") },
                { "oauth_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_version", "1.0" }
            };
        }

        private static string BuildOAuthHeader(Dictionary<string, string> oauthParams)
        {
            return "OAuth " + string.Join(", ",
                oauthParams.Select(p => $"{p.Key}=\"{Uri.EscapeDataString(p.Value)}\""));
        }

        static string GenerateOAuthSignature(
            string url,
            Dictionary<string, string> oauthParams,
            string consumerSecret,
            string tokenSecret)
        {
            if (string.IsNullOrWhiteSpace(tokenSecret))
                throw new Exception("OAuth token secret missing");

            var baseString = BuildBaseString(url, oauthParams);
            string key = Uri.EscapeDataString(consumerSecret) + "&" + Uri.EscapeDataString(tokenSecret);

            using var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(key));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(baseString)));
        }

        static string BuildBaseString(string url, Dictionary<string, string> oauthParams)
        {
            string paramString = string.Join("&",
                oauthParams.OrderBy(p => p.Key)
                    .Select(p =>
                    {
                        if (p.Value == null)
                            throw new Exception($"OAuth parameter '{p.Key}' is null");

                        return $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}";
                    }));

            return $"POST&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(paramString)}";
        }
    }
}
