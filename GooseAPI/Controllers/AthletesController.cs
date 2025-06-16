using Microsoft.AspNetCore.Mvc;

namespace GooseAPI.Controllers
{
    [ApiController]
    [Route($"/api/athletes")]
    public class AthletesController : Controller
    {
        [HttpGet]
        public IActionResult Get(string apiKey)
        {

            List<string> list = new List<string>();


            FirebaseService fbService = new FirebaseService();
            foreach(KeyValuePair<string, User> kvp in  fbService.GetData<Dictionary<string,User>>("Users")) {
            
                if(kvp.Value.ApiKey == apiKey)
                {
                    foreach (KeyValuePair<string,AthleteCoachConnection> conn in fbService.GetData<Dictionary<string, AthleteCoachConnection>>("AthleteCoachConnections"))
                    {
                        if(conn.Value.CoachUserName == kvp.Value.UserName)
                        {
                            list.Add(conn.Value.AthleteUserName);
                        }
                    }
                }

            
            }


            return Ok(list);
        }
    }
}
