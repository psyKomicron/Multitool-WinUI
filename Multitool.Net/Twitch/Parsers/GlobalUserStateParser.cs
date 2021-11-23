using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI;

namespace Multitool.Net.Twitch.Parsers
{
    /// <summary>
    /// <see cref="GlobalUserState"/> factory.
    /// </summary>
    internal static class GlobalUserStateParser
    {
        public static GlobalUserState Parse(Memory<char> toParse)
        {
#if DEBUG
            throw new NotImplementedException();
#endif
            List<Memory<char>> tokens = new();
            int lastSlice = 0;
            int tokenLength = 0;
            int equalsPosition = -1;
            for (int i = 0; i < toParse.Length; i++)
            {
                if (toParse.Span[i] == ';')
                {
                    tokens.Add(toParse.Slice(lastSlice, tokenLength));
                    lastSlice = i + 1;
                    tokenLength = 0;
                }
                else if (toParse.Span[i] == '=')
                {
                    equalsPosition = i;
                }
                else
                {
                    tokenLength++;
                }
            }

            // i assume the tag reponse is constant
            string badgeInfo = tokens[0][10..].ToString();
            List<string> badges = new();
            badges.Add(tokens[1][6..].ToString());
            //Color color = (Color)new ColorConverter().ConvertFromInvariantString(null, tokens[2][5..].ToString());
        }
    }
}
