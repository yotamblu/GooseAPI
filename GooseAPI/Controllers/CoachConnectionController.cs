using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace GooseAPI.Controllers
{



    [ApiController]
    [Route("/api/coachConnection")]
    public class CoachConnectionController : Controller
    {
        [HttpPost]
        [Route("connect")]
        public IActionResult ConnectToCoach([FromBody] CoachConnectionRequestData requestData)
        {
            string athleteName = GooseAPIUtils.FindUserNameByAPIKey(requestData.apiKey);
            if (athleteName == string.Empty) {
                return Unauthorized(new { message = "invalid ApiKey" });
            }
            if(GooseAPIUtils.GetUser(athleteName).Role == "coach")
            {
                return Unauthorized(new { message = "only athletes can be connected with coaches" });

            }
            string coachUserName = GooseAPIUtils.GetCoachById(requestData.coachId);
            if (coachUserName == string.Empty)
            {
                return BadRequest(new { message = "no coach was found with the supplied ID" });
            }

            if(!GooseAPIUtils.IsAlreadyConnected(athleteName, coachUserName))
            {
                AthleteCoachConnection connection = new AthleteCoachConnection
                {
                    AthleteUserName = athleteName
            ,
                    CoachUserName = coachUserName
                };
                new FirebaseService().InsertData(
                  $"/AthleteCoachConnections/{GooseAPIUtils.GenerateShortHexId()}"
                  , connection);

                return Ok(connection);
            }
            return Unauthorized(
                new { message = "cannot connect to the same coach twice"}
                  );
        }



        [HttpGet]
        [Route("getCoachName")]
        public IActionResult GetCoachName([FromQuery] string coachId)
        {
            string coachUserName = GooseAPIUtils.GetCoachById(coachId);
            if(coachUserName == string.Empty)
            {
                return BadRequest(new
                {
                    message = "coach not found"
                });
                
            }

            return Ok(
                new
                {
                    coachUsername = coachUserName
                });
        }

        [HttpGet]
        [Route("getCoachId")]
        public IActionResult GetCoachId([FromQuery] string coachName)
        {
            CoachData coachData = new FirebaseService().GetData<CoachData>($"/CoachCodes/{coachName}");
            if(coachData == null)
            {
                return BadRequest(new
                {
                    message = "a coach with this userName was not found"

                });
            }
            return Ok(
                new { coachId = coachData.CoachId }
                );
        }



    }





}
