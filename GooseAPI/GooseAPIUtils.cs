using System;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Globalization;

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
            int width = 256;
            int height = 256;

            using (Bitmap bitmap = new Bitmap(width, height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Random background color
                Random random = new Random();
                Color bgColor = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
                graphics.Clear(bgColor);

                // Dynamic font size based on image height
                float fontSize = height * 0.9f; // 60% of the height
                using (System.Drawing.Font font = new System.Drawing.Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    // Center the text
                    StringFormat format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    graphics.DrawString(c.ToString(), font, Brushes.White, new RectangleF(0, 0, width, height), format);
                }

                // Save and return Base64
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
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
