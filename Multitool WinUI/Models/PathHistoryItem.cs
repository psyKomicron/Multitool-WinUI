using Microsoft.UI.Dispatching;

using System;
using System.Text.RegularExpressions;

namespace MultitoolWinUI.Models
{
    public class PathHistoryItem : Model
    {
        private static readonly Regex regex = new(@"!(\\|\/)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private string _fullPath;
        private string _shortPath;

        public PathHistoryItem() : base(null)
        {
        }

        public PathHistoryItem(DispatcherQueue dispatcherQueue) : base(dispatcherQueue)
        {
        }

        public string ShortPath
        {
            get
            { 
                return _shortPath;
            }
            set
            {
                _shortPath = value;
                RaiseNotifyPropertyChanged();
            }
        }


        public string FullPath
        {
            get 
            { 
                return _fullPath; 
            }
            set
            { 
                _fullPath = value;
                RaiseNotifyPropertyChanged();
            }
        }

        public override string ToString()
        {
            return $"{ShortPath},{FullPath}";
        }

        public static PathHistoryItem FromString(string s, DispatcherQueue queue)
        {
            string[] pathes = s.Split(',');
            if (pathes.Length == 2 && !regex.IsMatch(pathes[0]))
            {
                return new PathHistoryItem(queue)
                {
                    ShortPath = pathes[0],
                    FullPath = pathes[1]
                };
            }
            else
            {
                throw new FormatException($"{nameof(s)} is not to the right format.");
            }
        }
    }
}
