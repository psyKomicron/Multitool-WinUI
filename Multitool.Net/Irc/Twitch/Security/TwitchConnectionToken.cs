using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Windows.Web.Http;

namespace Multitool.Net.Irc.Security
{
    public class TwitchConnectionToken : ConnectionToken
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="token">The <see cref="string"/> representing the connection's token (OAuth2, .</param>
        public TwitchConnectionToken(string token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token), "Provided string token is null");
        }

        /// <summary>
        /// Twitch's client id associated with this token
        /// </summary>
        public string ClientId { get; private set; }
        /// <summary>
        /// Name of the user associated with this token.
        /// </summary>
        public string Login { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"oauth:{Token}";
        }

        /// <summary>
        /// Called when creating the token to verify that the token is valid, per the inherited classes
        /// requirements.
        /// </summary>
        /// <param name="token">The token to validate</param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public override async Task<bool> ValidateToken()
        {
            Regex validationRegex = new(@"^([0-9A-Z-._~+/]+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!validationRegex.IsMatch(Token))
            {
                throw new FormatException($"Token does not follow the expected format (actual token: {Token}, expected format: {validationRegex})");
            }

            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new("Bearer", Token);

            using HttpResponseMessage res = await client.GetAsync(new(Properties.Resources.TwitchOAuthValidationUrl));
            JsonDocument json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());

            if (res.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (json.RootElement.TryGetProperty("message", out JsonElement jsonElement))
                {
                    return false;
                }
                else
                {
                    InvalidOperationException ex = new("Failed to validate token. Server responded with 401/Unauthorized.");
                    ex.Data.Add("Token", Token);
                    ex.Data.Add("Full response", json);
                    throw ex;
                }
            }

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

            return false;
        }
    }
}
