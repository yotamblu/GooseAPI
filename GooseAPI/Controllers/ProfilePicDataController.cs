using Microsoft.AspNetCore.Mvc;

namespace GooseAPI.Controllers
{
    [ApiController]
    [Route("/api/profilePic")]
    public class ProfilePicDataController : Controller
    {
        [HttpGet]
        public IActionResult Get(string userName) {
            User userData = new FirebaseService().GetData<User>($"Users/{userName}");
            if (userData == null || string.IsNullOrEmpty(userData.ProfilePicString))
            {
                return NotFound("User or profile picture not found.");
            }
            
            return Ok(userData.ProfilePicString);
        }




    }
}
