using Microsoft.AspNetCore.Mvc;

namespace GooseAPI.Controllers
{
    public class GarminConnectionValidationController : Controller
    {
        [HttpGet("/api/ValidateGarminConnection")]
        public IActionResult CheckForGarminData([FromQuery] string apiKey)
        {
            bool isConnectedCheck =
                 GooseAPIUtils.FindUserNameByAPIKey(apiKey) != string.Empty &&
                new FirebaseService().GetData<GarminData>($"/GarminData/{GooseAPIUtils.FindUserNameByAPIKey(apiKey)}") != null;


             return Ok(new { isConnected = isConnectedCheck });
        }
    }
}
