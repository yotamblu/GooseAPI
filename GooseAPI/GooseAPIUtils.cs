using System;
using System.Security.Cryptography;
using System.Text;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Globalization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;


namespace GooseAPI
{
    public class GooseAPIUtils
    {

      
        public static string FindUserNameByAPIKey(string apiKey)
        {
            foreach (KeyValuePair<string, User> kvp in new FirebaseService().GetData<Dictionary<string,User>>("Users"))
            {
                if(kvp.Value.ApiKey == apiKey)
                {
                    return kvp.Key;
                }
            }
            return string.Empty;
        }


        public static bool IsCoachingUser(string coachName,string athleteName)
        {
            Dictionary<string,AthleteCoachConnection> allCons = new FirebaseService().GetData<Dictionary<string, AthleteCoachConnection>>("/AthleteCoachConnections");
            foreach(KeyValuePair<string,AthleteCoachConnection> con in allCons)
            {
                if(con.Value.AthleteUserName == athleteName && con.Value.CoachUserName == coachName)
                {
                    return true;
                    
                }
            }
            return false;
        }



        public static string RemoveLeadingZeros(string dateStr)
        {
            // Try parsing the date using common formats
            string[] formats = {
            "MM/dd/yyyy", "dd/MM/yyyy", "yyyy-MM-dd",
            "M/d/yyyy", "d/M/yyyy", "yyyy-M-d"
        };

            if (DateTime.TryParseExact(dateStr, formats, CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out DateTime date))
            {
                // Return in format without leading zeros
                return $"{date.Month}/{date.Day}/{date.Year}";
            }

            // If parsing fails, return original string
            return dateStr;
        }

        public static Dictionary<string,string> GetGarminAPICredentials()
        {
            return new FirebaseService().GetData<Dictionary<string, string>>("/GarminAPICredentials");
        }

        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));

                return builder.ToString();
            }
        }



        public static string GenerateJwtToken(User user, IConfiguration _config)
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

        public static GarminData GetUserAccessTokenAndSecret(string userName)
        {
            if (GetUser(userName).Role != "athlete") {
                return null;
            }

            GarminData apiCreds = new FirebaseService().GetData<GarminData>($"/GarminData/{userName}");

            return apiCreds == null ? null : apiCreds;


        }

        public static string ConvertDateFormat(string inputDate)
        {
            // Parse the input string assuming it's in "yyyy-MM-dd" format
            if (DateTime.TryParseExact(inputDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out DateTime parsedDate))
            {
                // Return the date formatted as "MM/dd/yyyy"
                return parsedDate.ToString("MM/dd/yyyy");
            }
            else
            {
                throw new FormatException("Input date is not in the expected yyyy-MM-dd format.");
            }
        }


        public static string NormalizeDateToMMDDYYYY(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Date is required");

            if (!DateTime.TryParse(input, out var date))
                throw new FormatException($"Invalid date format: {input}");

            return date.ToString("MM/dd/yyyy");
        }


        private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            public static string GenerateApiKey(int length = 32)
            {
                var data = new byte[length];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(data);
                }

                var result = new StringBuilder(length);
                foreach (var b in data)
                {
                    // Use b mod chars length to pick a character
                    result.Append(chars[b % chars.Length]);
                }

                return result.ToString();
            }



        public static string GenerateProfilePictureBase64(char c)
        {
            const int size = 256;
            string text = c.ToString().ToUpper();

            // 1. Generate a consistent color based on the character 
            // This ensures "G" for GooseNet always looks the same
            int seed = (int)c;
            Random random = new Random(seed);
            Color bgColor = Color.FromRgb((byte)random.Next(40, 180), (byte)random.Next(40, 180), (byte)random.Next(40, 180));

            using (Image<Rgba32> image = new Image<Rgba32>(size, size))
            {
                // 2. Setup Font - Safer fallback for Linux/Docker
                if (!SystemFonts.Collection.TryGet("Arial", out var family))
                {
                    family = SystemFonts.Collection.Families.FirstOrDefault();
                    if (family == null) throw new Exception("No fonts installed on this system.");
                }

                // Use 0.5f to 0.6f of height to ensure the character stays inside the "safe zone"
                Font font = family.CreateFont(size * 0.55f, FontStyle.Bold);

                // 3. Perform Mutations
                image.Mutate(ctx =>
                {
                    // Fill background
                    ctx.Fill(bgColor);

                    // Configure text options for perfect centering
                    var options = new RichTextOptions(font)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Origin = new System.Numerics.Vector2(size / 2f, size / 2f),
                        // This ensures the drawing engine treats the center of the glyph as the origin
                        WrappingLength = size
                    };

                    // Draw the character in White
                    ctx.DrawText(options, text, Color.White);
                });

                // 4. Convert to Base64
                using (var ms = new MemoryStream())
                {
                    image.SaveAsPng(ms);
                    return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
                }
            }
        }


        public static User GetUser(string username) => new FirebaseService().GetData<User>($"/Users/{username}");



        public static bool IsAlreadyConnected(string athleteUserName, string coachUserName)
        {
            Dictionary<string, AthleteCoachConnection> allCons = new FirebaseService()
                .GetData<Dictionary<string, AthleteCoachConnection>>("/AthleteCoachConnections");
            foreach(KeyValuePair<string, AthleteCoachConnection> kvp in allCons)
            {
                AthleteCoachConnection connection = kvp.Value;
                if(connection.CoachUserName == coachUserName && connection.AthleteUserName == athleteUserName)
                {
                    return true;
                }
            }

            return false;

        }



        public static string GenerateShortHexId()
        {
            byte[] buffer = Guid.NewGuid().ToByteArray();
            // Take the first 6 bytes (12 hex characters)
            return BitConverter.ToString(buffer, 0, 6).Replace("-", "").ToLower();
        }
        public static string GetCoachById(string coachId)
        {
            FirebaseService firebaseService = new FirebaseService();
            Dictionary<string, CoachData> allCoachData = firebaseService.GetData<Dictionary<string, CoachData>>("/CoachCodes");
            CoachData coachData = null;
            foreach(KeyValuePair<string, CoachData> kvp in allCoachData)
            {
                if(kvp.Value.CoachId == coachId)
                {
                    coachData = kvp.Value;
                }
            }
            if(coachData == null)
            {
                return string.Empty;
            }
            
            return coachData.CoachUserName;


        }


        public static string GetUserAccessToken(string userName)
        {

            FirebaseService firebaseService = new FirebaseService();
            GarminData userData = firebaseService.GetData<GarminData>($"GarminData/{userName}");
            return userData.userAccessToken;

        }

    }
}
