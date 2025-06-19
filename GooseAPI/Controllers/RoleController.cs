using Microsoft.AspNetCore.Mvc;

namespace GooseAPI.Controllers
{
    public class RoleController : Controller
    {
        [HttpGet("/api/getRole")]
        public IActionResult GetRole(string apiKey)
        {
            //Check if user even exists
            string userName = GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            if (userName == String.Empty) {
                return NotFound(new { message = "userName not found"});
            }
            FirebaseService service = new FirebaseService();
            return Ok(new
            {
                role = service.GetData<User>($"/Users/{userName}").Role
            });
        }
    }
}
