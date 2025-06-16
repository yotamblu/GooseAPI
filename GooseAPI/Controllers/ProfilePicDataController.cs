using Microsoft.AspNetCore.Mvc;

namespace GooseAPI.Controllers
{
    [ApiController]
    [Route("/api/profilePic")]
    public class ProfilePicDataController : Controller
    {
        [HttpGet]
        public IActionResult Get(string userName) => Ok(new FirebaseService().GetData<User>($"Users/{userName}").ProfilePicString);




    }
}
