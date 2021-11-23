using System;
using System.Text.RegularExpressions;

namespace Multitool.Net.Twitch
{
    public abstract class ConnectionToken
    {
        private readonly string token;
        private readonly Regex validationRegex;

        protected ConnectionToken(string token, Regex regex)
        {
            this.token = token;
            validationRegex = regex;
            ValidateToken(token);
        }

        public override string ToString()
        {
            return token;
        }

        /// <summary>
        /// Called when creating the token to verify that the token is valid, per the inherited classes
        /// requirements.
        /// </summary>
        /// <param name="token">The token to validate</param>
        protected virtual void ValidateToken(string token)
        {
            if (!validationRegex.IsMatch(token))
            {
                throw new FormatException($"Token is not at the right format (actual token: {token})");
            }
        }
    }
}
