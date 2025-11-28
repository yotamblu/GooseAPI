using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;


namespace GooseAPI.Controllers
{
    [Route("/api/editProfile")]
    public class ProfileEditingController : Controller
    {
        [HttpPost("changePassword")]
        public IActionResult ChangePassword([FromQuery] string apiKey,[FromBody] PasswordChangingData requestData)
        {
            string userName = GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            if ( userName == string.Empty)
            {
                return Unauthorized(new {message = "invalid apiKey"});
            }
            
            //perform password change
            new FirebaseService().InsertData($"Users/{userName}/Password", requestData.NewPassword);
            return Ok(new { message = "password changed successfully" });

            
        }

        [HttpPost("changePic")]
        public IActionResult ChangeProfilePic([FromQuery] string apiKey, [FromQuery] string isRevert, [FromBody] PicChangeRequestData requestData) {
            string userName = GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            if ( userName == string.Empty)
            {
                return Unauthorized(new
                {
                    message = "Invalid API key"
                });
            }
            FirebaseService firebaseService = new FirebaseService();
            if (bool.Parse(isRevert))
            {
                string defaultPicString = GooseAPIUtils.GetUser(userName).DefualtPicString;
                firebaseService.InsertData($"Users/{userName}/ProfilePicString", defaultPicString);
            }
            else
            {
                firebaseService.InsertData($"Users/{userName}/ProfilePicString", requestData.PicString);
            }

            return Ok(new {message = "Profile Picture Updated Successfully"});


        }
    }
}
