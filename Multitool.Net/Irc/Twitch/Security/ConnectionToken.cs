using System.Threading.Tasks;

namespace Multitool.Net.Irc.Security
{
    public abstract class ConnectionToken
    {
        /// <summary>
        /// Raw token.
        /// </summary>
        public string Token { get; protected set; }
        /// <summary>
        /// <see langword="true"/> if the token has been validated.
        /// </summary>
        public bool Validated { get; protected set; }

        public abstract override string ToString();
        public abstract Task<bool> ValidateToken();
    }
}