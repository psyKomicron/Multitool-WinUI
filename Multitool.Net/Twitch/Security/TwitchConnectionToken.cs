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

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="token">The <see cref="string"/> representing the connection's token (OAuth2, .</param>
        public TwitchConnectionToken(string token)
        {
            this.token = token;
        }

        /// <summary>
        /// Twitch's client id associated with this token
        /// </summary>
        public string ClientId { get; private set; }
        /// <summary>
        /// Name of the user associated with this token.
        /// </summary>
        public string Login { get; private set; }
        /// <summary>
        /// Raw token.
        /// </summary>
        public string Token => token;
        /// <summary>
        /// <see langword="true"/> if the token has been validated.
        /// </summary>
        public bool Validated { get; private set; }

        /// <inheritdoc/>
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
        public async virtual Task<bool> ValidateToken()
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
                Login = value.ToString();
            }
            if (json.RootElement.TryGetProperty("client_id", out value))
            {
                ClientId = value.ToString();
                Validated = true;
                return true;
            }
#endif
            return false;
        }
    }
}
