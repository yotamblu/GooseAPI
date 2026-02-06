using Microsoft.AspNetCore.Mvc;

namespace GooseAPI.Controllers
{


    [ApiController]
    [Route("/api/sleep")]
    public class SleepDataController : Controller
    {
        //date must be yyyy-MM-dd
        [HttpGet("byDate")]
        public IActionResult Get([FromQuery] string apiKey, [FromQuery] string athleteName, [FromQuery] string date)
        {
            string userName = GooseAPI.GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            if (userName == "" || (GooseAPIUtils.GetUser(userName).Role != "coach" && userName != athleteName) ||  (userName != athleteName && !GooseAPIUtils.IsCoachingUser(userName, athleteName) && GooseAPIUtils.GetUser(userName).Role == "coach"))
            {
                return BadRequest(new { message = "there is no user with this apiKey" });

            }

            FirebaseService service = new FirebaseService();
            GarminData userGarminData = service.GetData<GarminData>($"/GarminData/{athleteName}");
            if(userGarminData == null)
            {
                return Unauthorized(new { message = "This user has not paired GooseNet with Garmin and is unable to view sleep data" });
            }
            SleepDataWEB sleepData = service.GetData<SleepDataWEB>($"SleepData/{userGarminData.userAccessToken}/{date}");
            if(sleepData == null)
            {
                return BadRequest(new { message = "There is no Sleep Data for this date" });
            }

            return Ok(sleepData);
            
        }


        [HttpGet("feed")]
        public IActionResult GetFeed(
      [FromQuery] string apiKey, 
      [FromQuery] string athleteName,
      [FromQuery] string cursor = null)
        {
            string userName = GooseAPI.GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            if (string.IsNullOrEmpty(userName) || (GooseAPIUtils.GetUser(userName).Role != "coach" && userName != athleteName ) || (userName != athleteName && !GooseAPIUtils.IsCoachingUser(userName, athleteName) && GooseAPIUtils.GetUser(userName).Role == "coach"))
            {
                return BadRequest(new { message = "there is no user with this apiKey" });
            }

            FirebaseService service = new FirebaseService();

            GarminData userGarminData =
                service.GetData<GarminData>($"/GarminData/{athleteName}");

            if (userGarminData == null)
            {
                return Unauthorized(new
                {
                    message = "This user has not paired GooseNet with Garmin and is unable to view sleep data"
                });
            }

            const int pageSize = 20;

            DateTime startDate;
            if (string.IsNullOrEmpty(cursor))
            {
                startDate = DateTime.UtcNow.Date;
            }
            else
            {
                if (!DateTime.TryParse(cursor, out startDate))
                {
                    return BadRequest(new { message = "cursor must be yyyy-MM-dd" });
                }
            }

            List<SleepDataWEB> items = new List<SleepDataWEB>();
            DateTime currentDate = startDate;

            while (items.Count < pageSize)
            {
                string dateKey = currentDate.ToString("yyyy-MM-dd");

                SleepDataWEB sleepData =
                    service.GetData<SleepDataWEB>(
                        $"SleepData/{userGarminData.userAccessToken}/{dateKey}");

                if (sleepData != null)
                {
                    items.Add(sleepData);
                }

                currentDate = currentDate.AddDays(-1);

                // Safety break: don’t scan forever if data is sparse
                if ((startDate - currentDate).TotalDays > 60)
                    break;
            }

            string nextCursor = items.Count > 0
                ? currentDate.AddDays(1).ToString("yyyy-MM-dd")
                : null;

            return Ok(new
            {
                items = items,
                nextCursor = nextCursor
            });
        }




    }



}
