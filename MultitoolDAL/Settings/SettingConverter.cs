using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multitool.DAL.Settings
{
    public abstract class SettingConverter
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public SettingConverter() { }

        public abstract object Convert(object toConvert);
        public abstract object Restore(object toRestore);
    }
}
