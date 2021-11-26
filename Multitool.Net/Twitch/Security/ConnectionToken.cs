using System;
using System.Text.RegularExpressions;

namespace Multitool.Net.Twitch.Security
{
    public abstract class ConnectionToken
    {
        private readonly string token;

        protected ConnectionToken(string token, Regex regex)
        {
            this.token = token;
            ValidateToken(token, regex);
        }

        public string Token => token;

        public override string ToString()
        {
            return token;
        }

        /// <summary>
        /// Called when creating the token to verify that the token is valid, per the inherited classes
        /// requirements.
        /// </summary>
        /// <param name="token">The token to validate</param>
        protected virtual void ValidateToken(string token, Regex validationRegex)
        {
            if (!validationRegex.IsMatch(token))
            {
                throw new FormatException($"Token does not follow the expected format (actual token: {token}, expected format: {validationRegex})");
            }
        }
    }
}
