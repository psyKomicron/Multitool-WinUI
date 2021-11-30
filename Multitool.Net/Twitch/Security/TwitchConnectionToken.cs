using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Web.Http;

namespace Multitool.Net.Twitch.Security
{
    public class TwitchConnectionToken
    {
        private readonly string token;

        public TwitchConnectionToken(string token)
        {
            this.token = token;
        }

        public bool Validated { get; private set; }

        public string Token => token;

        public string ClientId { get; private set; }

        public override string ToString()
        {
            return "oauth:" + token;
        }

        /// <summary>
        /// Called when creating the token to verify that the token is valid, per the inherited classes
        /// requirements.
        /// </summary>
        /// <param name="token">The token to validate</param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async virtual Task ValidateToken()
        {
            Regex validationRegex = new(@"^([0-9A-Z-._~+/]+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!validationRegex.IsMatch(token))
            {
                throw new FormatException($"Token does not follow the expected format (actual token: {token}, expected format: {validationRegex})");
            }

            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
            HttpResponseMessage res = await client.GetAsync(new(Properties.Resources.TwitchOAuthValidationUrl));
            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                InvalidOperationException ex = new("Failed to validate token. Server responded with 401.");
                ex.Data.Add("Token", token);
                ex.Data.Add("Full response", await res.Content.ReadAsStringAsync());
                throw ex;
            }
#if true
            JsonDocument json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            if (json.RootElement.TryGetProperty("login", out JsonElement value))
            {
                Debug.WriteLine("login: " + value.ToString());
            }
            if (json.RootElement.TryGetProperty("client_id", out value))
            {
                ClientId = value.ToString();
                Validated = true;
            }
#endif
        }
    }
}
