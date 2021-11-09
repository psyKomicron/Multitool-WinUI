using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multitool.Net.Irc
{
    public abstract class ConnectionToken
    {
        public ConnectionToken(string token)
        {
            StringToken = token;
        }

        protected string StringToken { get; set; }

        public abstract string GetToken();
    }
}
