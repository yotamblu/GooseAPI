using Microsoft.AspNetCore.Mvc;
using FireSharp.Serialization.JsonNet;
namespace GooseAPI.Controllers
{
    [ApiController]
    [Route("api/LoginValidation")]
    public class LogInValidationController : Controller
    {
        [HttpPost]
        public IActionResult Post([FromBody]LogInCredentials credentials )
        {
            FirebaseService fbService = new FirebaseService();
            User requestedUser = fbService.GetData<User>($"Users/{credentials.userName}");
           if(requestedUser == null || requestedUser.Password !=  GooseAPIUtils.ComputeSha256Hash(credentials.password))
            {
                return Unauthorized(new Message("The User Credentials Supplied Were Wrong!"));
            }
           
            return Ok(requestedUser);

            
        }
    }
}
