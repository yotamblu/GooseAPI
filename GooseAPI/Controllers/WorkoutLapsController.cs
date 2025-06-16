using Microsoft.AspNetCore.Mvc;

namespace GooseAPI.Controllers
{
    [ApiController]
    [Route("/api/workoutLaps")]
    public class WorkoutLapsController : Controller
    {
        [HttpGet]
        public IActionResult Get(string userName,long id)
            => Ok(new FirebaseService().GetData<List<FinalLap>>($"Activities/{GooseAPIUtils.GetUserAccessToken(userName)}/{id}/WorkoutLaps"));
        
    }
}
