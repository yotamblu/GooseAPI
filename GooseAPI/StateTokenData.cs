using System.Security.Policy;

namespace GooseAPI
{
    public class StateTokenData
    {
        public string Jwt {  get; set; }
        public string Token { get; set; }
        public string ExpiresAt {  get; set; }
    }
}
