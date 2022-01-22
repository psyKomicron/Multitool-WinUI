using System.Collections;

namespace Multitool.DAL.FileSystem
{
    /// <summary>
    /// Converts a <see cref="IList"/> to a string array.
    /// </summary>
    public class ArrayConverter : ISettingConverter<IList, string[]>
    {
        /// <inheritdoc/>
        public string[] Convert(IList toConvert)
        {
            string[] values = new string[toConvert.Count];
            for (int i = 0; i < toConvert.Count; i++)
            {
                values[i] = toConvert[i].ToString();
            }
            return values;
        }
    }
}
