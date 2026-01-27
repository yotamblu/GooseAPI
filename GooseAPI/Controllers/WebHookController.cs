using FireSharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json;

namespace GooseAPI.Controllers
{
    [ApiController]
    [Route("/api/webhook")]
    public class WebHookController : Controller
    {
        [AllowAnonymous]
        [HttpPost("workoutData")]
        public async Task<IActionResult> SubmitWorkoutData()
        {
            using var reader = new StreamReader(Request.Body);
            var rawJsonString = await reader.ReadToEndAsync();
            LogActivityData(rawJsonString);
            List<Workout> data = new ActivityJsonParser(rawJsonString).ParseActivityData();
            await Console.Out.WriteLineAsync(data.Count == 0 ? "0" :"!0");
            foreach (Workout workout in data)
            {
                LogActivityData(workout);
            }

            return Ok(new { message = "Workout Stored Successfully" });
        }


        [HttpPost("sleepData")] 
        public IActionResult SubmitSleepData()
        {
            Request.EnableBuffering();

            string rawJsonString = "";
            // Use a StreamReader to read the body stream
            using (var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8))
            {
                rawJsonString = reader.ReadToEndAsync().GetAwaiter().GetResult();

                Request.Body.Position = 0;
            }




            Sleep sleep = JsonConvert.DeserializeObject<Root>(rawJsonString).Sleeps[0];
            FirebaseClient client = new FirebaseClient(FireBaseConfig.config);

            SleepData data = new SleepData
            {
                AwakeDurationInSeconds = sleep.AwakeDurationInSeconds,
                DeepSleepDurationInSeconds = sleep.DeepSleepDurationInSeconds,
                LightSleepDurationInSeconds = sleep.LightSleepDurationInSeconds,
                OverallSleepScore = sleep.OverallSleepScore,
                RemSleepInSeconds = sleep.RemSleepInSeconds,
                SleepDate = sleep.CalendarDate,
                SleepDurationInSeconds = sleep.DurationInSeconds,
                SleepScores = sleep.SleepScores,
                SleepStartTimeInSeconds = sleep.StartTimeInSeconds,
                SleepTimeOffsetInSeconds = sleep.StartTimeOffsetInSeconds,
                SummaryID = sleep.SummaryId


            };

            new FirebaseService().InsertData($"SleepData/{sleep.UserAccessToken}/{sleep.CalendarDate}", data);





            return Ok();
        }


        


        private void LogActivityData(Workout data)
        {
            FirebaseService service = new FirebaseService();
            string path = $"Activities/{data.UserAccessToken}/{data.WorkoutId}";
            Console.WriteLine(path);
            service.InsertData(path, data);

        }

        private void LogActivityData(string data)
        {
            FirebaseClient client = new FirebaseClient(FireBaseConfig.pushConfig);
            string path = $"{DateTime.Now.ToString().Replace("/","-")}";
            Console.WriteLine(path);
            client.Set(path, data);

        }


    }
}
