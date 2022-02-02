using System;
using System.Collections.Generic;

namespace Multitool.Net.Twitch.Irc
{
    public class RoomStateEventArgs : EventArgs
    {
        private Dictionary<RoomStates, int> _data;

        public RoomStates States { get; internal set; }
        public Dictionary<RoomStates, int> Data
        {
            get
            {
                if (_data == null)
                {
                    _data = new();
                }
                return _data;
            }
            internal set => _data = value;
        }
    }
}
