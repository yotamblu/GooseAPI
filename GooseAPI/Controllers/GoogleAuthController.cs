using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace GooseAPI.Controllers
{
    [ApiController]
    public class GoogleAuthController : Controller
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public GoogleAuthController(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }

        // STEP 1: Redirect user to Google
        [HttpGet("auth/google")]
        public IActionResult GoogleLogin([FromQuery] string role)
        {
            string clientId = _config["GOOGLE_CLIENT_ID"];
            string redirectUri = _config["GOOGLE_REDIRECT_URI"] + $"/{role}";
            Console.WriteLine(redirectUri);
            string authUrl =
                "https://accounts.google.com/o/oauth2/v2/auth" +
                "?client_id=" + clientId +
                "&redirect_uri=" + redirectUri +
                "&response_type=code" +
                "&scope=openid%20email%20profile";

            return Redirect(authUrl);
        }

        // STEP 2: Google redirects back here
        [HttpGet("auth/google/callback/{role}")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromRoute] string role)
        {
            FirebaseService firebaseService = new FirebaseService();
            if (string.IsNullOrEmpty(code))
                return BadRequest("Missing authorization code");

            string clientId = _config["GOOGLE_CLIENT_ID"];
            string clientSecret = _config["GOOGLE_CLIENT_SECRET"];
            string redirectUri = _config["GOOGLE_REDIRECT_URI"] + $"/{role}";
            string redirectUrl = _config["GOOGLE_SUCCESS_URL"] + "?jwt=";

            // Exchange authorization code for tokens
            var tokenResponse = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "code", code },
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "redirect_uri", redirectUri },
                    { "grant_type", "authorization_code" }
                })
            );

            if (!tokenResponse.IsSuccessStatusCode)
                return Unauthorized(tokenResponse);

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<GoogleTokenResponse>(tokenJson);

            // Validate Google ID token
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                token.id_token,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                }
            );

            //check if already signed up
            User existingUser =  firebaseService
            .GetData<Dictionary<string, User>>("/Users")
            .FirstOrDefault(kvp => kvp.Value.GoogleSubject == payload.Subject)
            .Value;
            if(existingUser != null)
            {
                //log user in
                string jwtToken = GooseAPIUtils.GenerateJwtToken(existingUser,_config);
                return Redirect(redirectUrl + jwtToken);
            }


            //prevent doubles in the DB by adding a number for duplicate UserNames
            string requestedUserName = payload.Name.Replace(" ","");
            string fixedUserName = requestedUserName;
            bool userNameValidated = false;
            int userNameAddition = 2;
            while (!userNameValidated)
            {
                if (firebaseService.GetData<User>($"/Users/{requestedUserName}") != null)
                {
                    fixedUserName = requestedUserName + (++userNameAddition).ToString();
                }
                else
                {
                    userNameValidated = true;
                }
            }



            //create user
            User userData = new User
            {
                UserName = fixedUserName,
                ApiKey = GooseAPIUtils.GenerateApiKey(),
                DefualtPicString = payload.Picture,
                FullName = payload.Name,
                Email = payload.Email,
                Role = role,
                GoogleSubject = payload.Subject,
                ProfilePicString = payload.Picture
            };
            if (userData.Role == "coach")
            {
                firebaseService.InsertData($"/CoachCodes/{userData.UserName}", new CoachData
                {


                    CoachId = GooseAPIUtils.GenerateShortHexId(),
                    CoachUserName = userData.UserName

                });
            }

            firebaseService.InsertData($"Users/{userData.UserName}", userData);

            //log user in
            string jwt = GooseAPIUtils.GenerateJwtToken(userData, _config);
            return Redirect(redirectUrl + jwt);
        }

      
    }

    
}
