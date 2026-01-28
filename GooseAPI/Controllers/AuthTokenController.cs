using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Data;

namespace GooseAPI.Controllers
{
  
    public class AuthTokenController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AuthTokenController(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }


        [HttpGet("/api/request-token")]
        public IActionResult GetTokenAndSecret([FromQuery] string? apiKey = null)
        {
            var creds = GooseAPIUtils.GetGarminAPICredentials(); // expects ConsumerKey, ConsumerSecret

            var url = "https://connectapi.garmin.com/oauth-service/oauth/request_token";
            var service = new HttpsService("https://connectapi.garmin.com");
            var oAuth = new OAuthHelper(creds["ConsumerKey"], creds["ConsumerSecret"]);

            var header = oAuth.GenerateOAuthHeader(url, "POST");
            service.SetOAuthHeader(header);

            // Send POST (empty body)
            string response = service.PostFormUrlEncoded("/oauth-service/oauth/request_token", "");

            // Parse query string response into dictionary
            var values = ParseQueryString(response);

            if (!values.ContainsKey("oauth_token") || !values.ContainsKey("oauth_token_secret"))
                return BadRequest("Invalid response from Garmin: " + response);

            if (!string.IsNullOrEmpty(apiKey))
            {
                string oauthStateToken = CreateOauthStateToken(apiKey);
                return Ok(new
                {
                    stateToken = oauthStateToken,
                    oauth_token = values["oauth_token"],
                    oauth_token_secret = values["oauth_token_secret"]
                });
            }

            return Ok(new
            {
                oauth_token = values["oauth_token"],
                oauth_token_secret = values["oauth_token_secret"]
            });
        }

        [HttpGet("/api/access-token")]
        public IActionResult GetAccessToken([FromQuery] string oauth_token, [FromQuery] string oauth_verifier
            , [FromQuery] string token_secret, [FromQuery] string apiKey)
        {
            if(new FirebaseService().GetData<User>($"/Users/{GooseAPIUtils.FindUserNameByAPIKey(apiKey)}").Role != "athlete")
            {
                return Unauthorized("Must be athlete to do this action");
            }
            Console.WriteLine($"oauth_token: '{oauth_token}'");
            Console.WriteLine($"oauth_verifier: '{oauth_verifier}'");
            Console.WriteLine($"token_secret: '{token_secret}'");

            var creds = GooseAPIUtils.GetGarminAPICredentials();
            var baseUrl = "https://connectapi.garmin.com"; 
            var endpoint = "/oauth-service/oauth/access_token";
            var fullUrl = baseUrl + endpoint;

            var service = new HttpsService(baseUrl);
            Console.WriteLine(creds["ConsumerKey"]);
            Console.WriteLine(creds["ConsumerSecret"]);
            var oAuth = new OAuthHelper(creds["ConsumerKey"], creds["ConsumerSecret"]);

            // Set the token secret received from previous step (request token secret)
            oAuth.SetTokenSecret(token_secret);

            // Generate the OAuth Authorization header including oauth_token and oauth_verifier
            var authHeader = oAuth.GenerateOAuthHeader(fullUrl, "POST", oauth_token, oauth_verifier) ;
            Console.WriteLine(authHeader);
            service.SetOAuthHeader(authHeader );

            // Post with empty body (as required by Garmin OAuth)
            var response = service.PostFormUrlEncoded(endpoint, "");

            var values = ParseQueryString(response);

            if (!values.ContainsKey("oauth_token") || !values.ContainsKey("oauth_token_secret"))
            {
                return BadRequest("Invalid response from Garmin: " + response);
            }
            string userName = GooseAPIUtils.FindUserNameByAPIKey(apiKey);
            new FirebaseService().InsertData($"/GarminData/{userName}",new GarminData
            {userAccessToken = values["oauth_token"],
               userAccessTokenSecret = values["oauth_token_secret"],
               userName =userName
            });

            return Ok();
        }




        [HttpGet("/api/auth/stateToken")]
        public IActionResult GetJwtFromStateToken([FromQuery] string token)
        {
            string jwt = ExchangeStateTokenForJwt(token);
            if(jwt == null)
            {
                return Unauthorized(new { message = "the state token is either expired or doesnt exist" });
            }
            return Ok(new {token = jwt });
        }


        private Dictionary<string, string> ParseQueryString(string input)
        {
            var dict = new Dictionary<string, string>();
            var pairs = input.Split('&');

            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                    dict[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
            }

            return dict;
        }

        private string CreateOauthStateToken(string apiKey)
        {
            FirebaseService firebaseService = new FirebaseService();
            User userData = GooseAPIUtils.GetUser(GooseAPIUtils.FindUserNameByAPIKey(apiKey));
            string jwt = GooseAPIUtils.GenerateJwtToken(userData, _config);
            string tokenExpiration = DateTime.Now.AddMinutes(10).ToString();
            string stateToken = GooseAPIUtils.GenerateShortHexId();
            firebaseService.InsertData($"/OAuthStateTokens/{stateToken}", new StateTokenData
            {
                Token = stateToken,
                ExpiresAt = tokenExpiration,
                Jwt = jwt
            });


            return stateToken;
        }

        private string ExchangeStateTokenForJwt(string stateToken)
        {
            FirebaseService firebaseService = new FirebaseService();
            string path = $"/OAuthStateTokens/{stateToken}";
            StateTokenData data = firebaseService.GetData<StateTokenData>(path);

            if (data == null)
                return null;

            if (DateTime.Parse(data.ExpiresAt) <= DateTime.Now)
            {
                firebaseService.DeleteData(path);
                return null;
            }

            firebaseService.DeleteData(path);
            return data.Jwt;
        }

    }
}
