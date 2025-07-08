using Microsoft.AspNetCore.Mvc;
using RestSharp.Extensions;

namespace GooseAPI.Controllers
{
    [ApiController]
    [Route("/api/flocks")]
    public class FlocksController : Controller
    {
        [HttpGet("getFlocks")]
        public IActionResult GetFlocks([FromQuery] string apiKey)
        {
            if (GooseAPIUtils.GetUser(GooseAPIUtils.FindUserNameByAPIKey(apiKey)).Role != "coach")
            {
                return Unauthorized(new { message = "only coaches can view their flocks" });
            }

            List<string> flockNames = new List<string>();

            FirebaseService firebaseService = new FirebaseService();
            try
            {
                List<Flock> flocks = firebaseService.GetData<Dictionary<string, Flock>>($"/Flocks/{GooseAPIUtils.FindUserNameByAPIKey(apiKey)}").Values.ToList();
                foreach (Flock flock in flocks)
                {
                    flockNames.Add(flock.FlockName);
                }
            }
            catch (Exception ex) { }


            return Ok(new { flocks = flockNames });


        }

        [HttpPost("createFlock")]
        public IActionResult CreateFlock([FromQuery] string apiKey, [FromQuery] string flockName) {
            User coachUser = GooseAPIUtils.GetUser(GooseAPIUtils.FindUserNameByAPIKey(apiKey));
            if (coachUser == null || coachUser.Role != "coach")
            {
                return Unauthorized(new { message = "only coaches can view their flocks" });
            }
            FirebaseService service = new FirebaseService();
            if(service.GetData<Flock>($"/Flocks/{coachUser.UserName}/{flockName}") != null)
            {
                return Unauthorized(new { message = "This coach already has a flock with this name" });
            }

            service.InsertData($"/Flocks/{coachUser.UserName}/{flockName}", new Flock { FlockName = flockName, athletesUserNames = null});
            return Ok(new { message = "Flock created successfully" });
        }



        [HttpPost("addToFlock")]
        public IActionResult AddToFlock([FromBody] AddToFlockData data, [FromQuery] string apiKey) {
            User athleteUser = GooseAPIUtils.GetUser(data.athleteUserName);
            if (GooseAPIUtils.GetUser(GooseAPIUtils.FindUserNameByAPIKey(apiKey)).Role != "coach" )
            {
                return Unauthorized(new { message = "only coaches can view their flocks" });
            }
           
            string coachUserName = GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            if (athleteUser == null || athleteUser.Role != "athlete" || !GooseAPIUtils.IsCoachingUser(coachUserName, athleteUser.UserName))
            {
                return Unauthorized(new { message = "you can't add this user to a Flock" });

            }
            

            string athleteUserName = athleteUser.UserName;
            FirebaseService firebaseService = new FirebaseService();
            Dictionary<string, Flock> coachFlocks = firebaseService.GetData<Dictionary<string, Flock>>($"Flocks/{coachUserName}");
            if(coachFlocks == null)
            {
                return BadRequest(new { message = "this coach has no flocks" });
            }
            if (!coachFlocks.ContainsKey(data.flockName))
            {
                return BadRequest(new { message = "this coach has no flock with this name" });

            }
            if (coachFlocks[data.flockName].athletesUserNames != null && coachFlocks[data.flockName].athletesUserNames.Contains(athleteUserName))
            {
                return BadRequest(new { message = "this athlete is already in this flock" });

            }

            //perform adding to flock

            List<string> athletes = coachFlocks[data.flockName].athletesUserNames;
            if (athletes == null)
            {
                athletes = new List<string>();
            }
             athletes.Add(athleteUserName);
            firebaseService.InsertData($"/Flocks/{coachUserName}/{data.flockName}/athletesUserNames", athletes);


            return Ok(new { message = "athlete added to flock successfully"});
        }
    }
}
