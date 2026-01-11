using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GooseAPI.Controllers
{
    [ApiController]
    [Route("api/userAuth")]
    public class UserAuthController : Controller
    {
        private readonly IConfiguration _config;

        public  UserAuthController(IConfiguration config)
        {
            _config = config;
        }

        // 🔐 LOGIN
        // POST /api/userAuth
        [HttpPost]
        public IActionResult Login([FromBody] LogInCredentials credentials)
        {
            FirebaseService fbService = new FirebaseService();
            User requestedUser = fbService.GetData<User>($"Users/{credentials.userName}");

            if (requestedUser == null || requestedUser.Password != credentials.hashedPassword)
            {
                return Unauthorized(new Message("The User Credentials Supplied Were Wrong!"));
            }

            string token = GooseAPIUtils.GenerateJwtToken(requestedUser,_config);

            // 🔁 BACKWARD-COMPATIBLE RESPONSE
            return Ok(new
            {
                message = "Valid Credentials, Authorized",
                authorized = true,
                apiKey = requestedUser.ApiKey,
                token = token
            });
        }

        // 👤 ME
        // GET /api/userAuth/me
        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            // 🔴 THIS NOW WORKS RELIABLY
            var userName = User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userName))
            {
                return Unauthorized("No username found in JWT");
            }

            FirebaseService fbService = new FirebaseService();
            User user = fbService.GetData<User>($"Users/{userName}");

            if (user == null)
            {
                return NotFound("User not found");
            }

            user.Password = null;
            return Ok(user);
        }

      
    }
}
