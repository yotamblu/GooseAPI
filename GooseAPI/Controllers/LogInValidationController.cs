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

        public UserAuthController(IConfiguration config)
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

            string token = GenerateJwtToken(requestedUser);

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

        // 🔑 JWT CREATION (WITH KeyId)
        private string GenerateJwtToken(User user)
        {
            var claims = new List<Claim>
            {
                // 🔴 This becomes User.Identity.Name
                new Claim(ClaimTypes.NameIdentifier, user.UserName),

                // Optional JWT standard
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),

                new Claim("apiKey", user.ApiKey),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(
                    JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64
                )
            };

            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!)
            )
            {
                KeyId = "goosenet-default"
            };

            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
