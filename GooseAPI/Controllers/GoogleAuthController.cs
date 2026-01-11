using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        [HttpGet("auth/google")]
        public IActionResult GoogleLogin([FromQuery] string role)
        {
            string clientId = _config["GOOGLE_CLIENT_ID"];
            string redirectUri = _config["GOOGLE_REDIRECT_URI"];

            string origin =
                Request.Headers["Origin"].FirstOrDefault()
                ?? Request.Headers["Referer"].FirstOrDefault()
                ?? $"{Request.Scheme}://{Request.Host}";

            var statePayload = new Dictionary<string, string>
            {
                { "origin", origin.TrimEnd('/') },
                { "role", role ?? "" },
                { "ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }
            };

            string stateJson = JsonSerializer.Serialize(statePayload);
            string state = Base64UrlEncode(Encoding.UTF8.GetBytes(stateJson));

            string authUrl =
                "https://accounts.google.com/o/oauth2/v2/auth" +
                "?client_id=" + Uri.EscapeDataString(clientId) +
                "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
                "&response_type=code" +
                "&scope=" + Uri.EscapeDataString("openid email profile") +
                "&state=" + Uri.EscapeDataString(state);

            return Redirect(authUrl);
        }

        [HttpGet("auth/google/callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
                return BadRequest();

            var decodedState = JsonSerializer.Deserialize<Dictionary<string, string>>(
                Encoding.UTF8.GetString(Base64UrlDecode(state))
            );

            string origin = (decodedState.TryGetValue("origin", out var o) ? o : "").TrimEnd('/');
            string role = decodedState.TryGetValue("role", out var r) ? r : "";

            string clientId = _config["GOOGLE_CLIENT_ID"];
            string clientSecret = _config["GOOGLE_CLIENT_SECRET"];
            string redirectUri = _config["GOOGLE_REDIRECT_URI"];

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
                return Unauthorized();

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<GoogleTokenResponse>(tokenJson);

            var payload = await GoogleJsonWebSignature.ValidateAsync(
                token.id_token,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                }
            );

            FirebaseService firebaseService = new FirebaseService();

            User existingUser = firebaseService
                .GetData<Dictionary<string, User>>("/Users")
                .FirstOrDefault(kvp => kvp.Value.GoogleSubject == payload.Subject)
                .Value;

            if (existingUser != null)
            {
                string jwtExisting = GooseAPIUtils.GenerateJwtToken(existingUser, _config);
                return Redirect(origin + "/login/google/success?jwt=" + Uri.EscapeDataString(jwtExisting));
            }

            string requestedUserName = payload.Name.Replace(" ", "");
            string fixedUserName = requestedUserName;
            int userNameAddition = 1;

            while (firebaseService.GetData<User>($"/Users/{fixedUserName}") != null)
            {
                fixedUserName = requestedUserName + (++userNameAddition).ToString();
            }

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

            if (role == "coach")
            {
                firebaseService.InsertData($"/CoachCodes/{userData.UserName}", new CoachData
                {
                    CoachId = GooseAPIUtils.GenerateShortHexId(),
                    CoachUserName = userData.UserName
                });
            }

            firebaseService.InsertData($"Users/{userData.UserName}", userData);

            string jwt = GooseAPIUtils.GenerateJwtToken(userData, _config);
            return Redirect(origin + "/auth/google/success?jwt=" + Uri.EscapeDataString(jwt));
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
