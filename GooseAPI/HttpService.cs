using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace GooseAPI
{
    public class HttpsService : IDisposable
    {
        private readonly HttpClient _httpClient;

        public HttpsService(string baseAddress = null)
        {
            _httpClient = new HttpClient();

            if (!string.IsNullOrEmpty(baseAddress))
            {
                _httpClient.BaseAddress = new Uri(baseAddress);
            }

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public string Get(string url)
        {
            var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();

            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        // For JSON POST
        public string PostJson(string url, string jsonBody)
        {
            using var content = new StringContent(jsonBody ?? "", Encoding.UTF8, "application/json");
            var response = _httpClient.PostAsync(url, content).GetAwaiter().GetResult();

            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        // For OAuth token request, which requires "application/x-www-form-urlencoded" content type
        public string PostFormUrlEncoded(string url, string formBody)
        {
            using var content = new StringContent(formBody ?? "", Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = _httpClient.PostAsync(url, content).GetAwaiter().GetResult();

            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        public void SetAuthorizationHeader(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        public void SetOAuthHeader(string rawHeaderValue)
        {
            // Remove existing Authorization header first
            if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
                _httpClient.DefaultRequestHeaders.Remove("Authorization");

            // rawHeaderValue already includes "OAuth ", so set directly
            _httpClient.DefaultRequestHeaders.Add("Authorization", rawHeaderValue);
        }
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
