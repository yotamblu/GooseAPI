using System;
using System.Security.Cryptography;
using System.Text;
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

        public static string GetUserAccessToken(string userName)
        {

            FirebaseService firebaseService = new FirebaseService();
            GarminData userData = firebaseService.GetData<GarminData>($"GarminData/{userName}");
            return userData.userAccessToken;

        }

    }
}
