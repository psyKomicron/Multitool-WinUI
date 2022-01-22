using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multitool.DAL.Settings
{
    public class StringListSettingConverter : SettingConverter
    {
        public StringListSettingConverter() : base()
        {
        }

        public override object Convert(object toConvert)
        {
            if (toConvert is List<object> list)
            {
                string[] array = new string[list.Count];
                int i = 0;
                foreach (var item in list)
                {
                    array[i] = item.ToString();
                    i++;
                }

                return array;
            }
            else
            {
                return null;
            }
        }

        public override object Restore(object toRestore)
        {
            if (toRestore is object[] array)
            {
                List<string> strings = new(array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    strings.Add(array[i].ToString());
                }
                return strings;
            }
            else
            {
                List<string> strings = new(1);
                strings.Add(toRestore.ToString());
                return strings;
            }
        }
    }
}