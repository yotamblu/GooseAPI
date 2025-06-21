using Microsoft.AspNetCore.Mvc;

namespace GooseAPI.Controllers
{
    public class RegistrationController : Controller
    {
        [HttpPost("/api/registration")]
        public IActionResult RegisterUser([FromBody] User userData)
        {
            FirebaseService service = new FirebaseService();
            //Check if user already exists
            User data = service.GetData<User>($"/Users/{userData.UserName}");
            if(data !=  null )
            {
                return BadRequest(new { message = "userName already exists" });
            }



            //generate profile pic
            String picString = GooseAPIUtils.GenerateProfilePictureBase64(userData.UserName[0]);
            service.InsertData($"Users/{userData.UserName}", new User
            {
                UserName = userData.UserName,
                Email = userData.Email,
                Password = userData.Password,
                ProfilePicString = picString,
                DefualtPicString = picString,
                ApiKey = GooseAPIUtils.GenerateApiKey(),
                FullName = userData.FullName,
                Role = userData.Role

            });

            if(userData.Role == "coach")
            {
                service.InsertData($"/CoachCodes/{userData.UserName}",new CoachData { 
                    
                    
                    CoachId = GooseAPIUtils.GenerateShortHexId(),
                    CoachUserName = userData.UserName
                
                });
            }

            return Ok(new { message = "User Registered Successfully" });
        }
    }
}
