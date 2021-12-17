using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI;

namespace Multitool.UI
{
    public class ColorConverter
    {
        public ColorConverter() { }

        public ColorConverter(byte defaultAlpha)
        {
            DefaultAlpha = defaultAlpha;
        }

        public byte DefaultAlpha { get; set; } = 0xFF;

        public Color ConvertFromString(string color)
        {
            ReadOnlySpan<char> text = new(color.ToCharArray());
            int index = 0;

            if (text[0] == '#')
            {
                index = 1;
            }

            ReadOnlySpan<char> r, g, b;
            r = text.Slice(index, 2);
            index += 2;
            g = text.Slice(index, 2);
            index += 2;
            b = text.Slice(index, 2);

            // parse
            if (byte.TryParse(r, out byte red))
            {
                if (byte.TryParse(g, out byte green))
                {
                    if (byte.TryParse(b, out byte blue))
                    {
                        Color c = new();
                        c.A = DefaultAlpha;
                        c.R = red;
                        c.G = green;
                        c.B = blue;
                        return c;
                    }
                    else
                    {
                        throw new FormatException("Blue value could not be parsed: " + b.ToString());
                    }
                }
                else
                {
                    throw new FormatException("Green value could not be parsed: " + g.ToString());
                }
            }
            else
            {
                throw new FormatException("Red value could not be parsed: " + r.ToString());
            }
        }
    }
}
