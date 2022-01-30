using System;

using Windows.UI;

namespace Multitool.Drawing
{
    public class ColorConverter
    {
        public ColorConverter() { }

        public byte DefaultAlpha { get; set; } = 0xFF;

        public Color ConvertFromHexaString(string color)
        {
            if (color.Length < 6 || color.Length > 7)
            {
                throw new FormatException("Color does not have the right amount of characters. Actual string: " + color);
            }

            ReadOnlySpan<char> text = new(color.ToCharArray());
            int index = 0;

            if (text[0] == '#')
            {
                index = 1;
            }

            byte red, green, blue;
            red = ConvertHexa(text.Slice(index, 2));
            index += 2;
            green = ConvertHexa(text.Slice(index, 2));
            index += 2;
            blue = ConvertHexa(text.Slice(index, 2));

            Color c = new();
            c.A = DefaultAlpha;
            c.R = red;
            c.G = green;
            c.B = blue;
            return c;
        }

        private static byte ConvertHexa(ReadOnlySpan<char> span)
        {
            char highByte = span[0];
            char lowByte = span[1];

            byte value1 = highByte switch
            {
                '0' => 0x0,
                '1' => 0x10,
                '2' => 0x20,
                '3' => 0x30,
                '4' => 0x40,
                '5' => 0x80,
                '6' => 0x60,
                '7' => 0x70,
                '8' => 0x80,
                '9' => 0x90,
                'A' => 0xA0,
                'B' => 0xB0,
                'C' => 0xC0,
                'D' => 0xD0,
                'E' => 0xE0,
                'F' => 0xF0,
                _ => throw new FormatException("Provided char is not an hexadecimal value, [0-9,A-F]"),
            };
            byte value2 = lowByte switch
            {
                '0' => 0x0,
                '1' => 0x1,
                '2' => 0x2,
                '3' => 0x3,
                '4' => 0x4,
                '5' => 0x5,
                '6' => 0x6,
                '7' => 0x7,
                '8' => 0x8,
                '9' => 0x9,
                'A' => 0xA,
                'B' => 0xB,
                'C' => 0xC,
                'D' => 0xD,
                'E' => 0xE,
                'F' => 0xF,
                _ => throw new FormatException("Provided char is not an hexadecimal value, [0-9,A-F]"),
            };
            return (byte)(value1 + value2);
        }
    }
}
