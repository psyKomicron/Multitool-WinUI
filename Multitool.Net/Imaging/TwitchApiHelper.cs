using Multitool.Net.Irc.Security;
using Multitool.Net.Properties;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Web.Http;

namespace Multitool.Net.Imaging
{
    internal class TwitchApiHelper : IDisposable
    {
        private readonly HttpClient client;
        private readonly TwitchConnectionToken token;
        private bool disposed;
        private bool needsValidation;

        public TwitchApiHelper(TwitchConnectionToken connectionToken) : this(new HttpClient())
        {
            token = connectionToken;
            if (connectionToken.Validated)
            {
                client.DefaultRequestHeaders.Authorization = new("Bearer", connectionToken.Token);
                client.DefaultRequestHeaders.Add(new("Client-Id", connectionToken.ClientId));
            }
            else
            {
                needsValidation = true;
            }
        }

        public TwitchApiHelper(HttpClient client)
        {
            this.client = client;
        }

        public void Dispose()
        {
            client.Dispose();
            disposed = true;
        }

        /// <summary>
        /// Gets the twitch id of <paramref name="login"/>.
        /// </summary>
        /// <param name="login">Login to get the id for.</param>
        /// <returns>The id for the <see cref="login"/></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="FormatException">Thrown if the API does not reply with the correct data type.</exception>
        public async Task<string> GetUserId(string login)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (needsValidation)
            {
                if (!token.Validated && !await token.ValidateToken())
                {
                    throw new ArgumentException("Twitch connection is not valid. The calls to the twitch API will fail.");
                }
                client.DefaultRequestHeaders.Authorization = new("Bearer", token.Token);
                client.DefaultRequestHeaders.Add(new("Client-Id", token.ClientId));
            }

            using HttpResponseMessage getUsersEndpointResponse = await client.GetAsync(new(string.Format(Resources.TwitchApiGetUsersByLoginEndpoint, login)), HttpCompletionOption.ResponseHeadersRead);
            getUsersEndpointResponse.EnsureSuccessStatusCode();

            string usersEndpointResponse = await getUsersEndpointResponse.Content.ReadAsStringAsync();
            JsonDocument idData = JsonDocument.Parse(usersEndpointResponse);

            if (idData.RootElement.TryGetProperty("data", out JsonElement jsonData))
            {
                if (jsonData.ValueKind != JsonValueKind.Array)
                {
                    throw new FormatException($"Twitch api 'channel emotes' did not reply with a correct data type. Expected array, got {jsonData.ValueKind}");
                }

                if (jsonData[0].TryGetProperty("id", out JsonElement value))
                {
                    return value.ToString();
                }
                else
                {
                    Exception ex = new FormatException("Unable to parse user id from Twitch API GetUsers endpoint");
                    ex.Data.Add("Full response", usersEndpointResponse);
                    throw ex;
                }
            }
            else
            {
                Exception ex = new FormatException("Unable to parse Twitch API GetUsers endpoint response. (does not have { data: {...} })");
                ex.Data.Add("Full response", usersEndpointResponse);
                throw ex;
            }
        }
    }
}
