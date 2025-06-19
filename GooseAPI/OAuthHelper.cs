using System.Security.Cryptography;
using System.Text;

public class OAuthHelper
{
    private readonly string _consumerKey;
    private readonly string _consumerSecret;
    private string _tokenSecret = "";

    public void SetTokenSecret(string tokenSecret)
    {
        _tokenSecret = tokenSecret;
    }

    public OAuthHelper(string consumerKey, string consumerSecret)
    {
        _consumerKey = consumerKey;
        _consumerSecret = consumerSecret;
    }

    public string GenerateOAuthHeader(string url, string method = "POST", string token = null, string verifier = null)
    {
        var oauthParams = new Dictionary<string, string>
        {
            { "oauth_consumer_key", _consumerKey },
            { "oauth_nonce", Guid.NewGuid().ToString("N") },
            { "oauth_signature_method", "HMAC-SHA1" },
            { "oauth_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
            { "oauth_version", "1.0" }
        };

        if (!string.IsNullOrEmpty(token))
            oauthParams["oauth_token"] = token;

        if (!string.IsNullOrEmpty(verifier))
            oauthParams["oauth_verifier"] = verifier;

        string signature = GenerateSignature(url, method, oauthParams);
        oauthParams["oauth_signature"] = signature;

        return "OAuth " + string.Join(", ", oauthParams
            .Select(kvp => $"{kvp.Key}=\"{Uri.EscapeDataString(kvp.Value)}\""));
    }

    private string GenerateSignature(string url, string method, Dictionary<string, string> parameters)
    {
        var sortedParams = parameters.OrderBy(p => p.Key)
                                     .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");
        var paramString = string.Join("&", sortedParams);

        var signatureBaseString = $"{method.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(paramString)}";

        var signingKey = $"{Uri.EscapeDataString(_consumerSecret)}&";

        if (parameters.ContainsKey("oauth_token"))
        {
            var tokenSecret = _tokenSecret ?? "";
            signingKey = $"{Uri.EscapeDataString(_consumerSecret)}&{Uri.EscapeDataString(tokenSecret)}";
        }

        using (var hasher = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey)))
        {
            var hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(signatureBaseString));
            return Convert.ToBase64String(hash);
        }
    }
}
