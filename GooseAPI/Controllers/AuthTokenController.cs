using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace GooseAPI.Controllers
{
  
    public class AuthTokenController : ControllerBase
    {


        [HttpGet("/api/request-token")]
        public IActionResult GetTokenAndSecret()
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


    }
}
