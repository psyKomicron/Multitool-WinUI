using System;

namespace Multitool.Net.Irc.Twitch
{
    [Flags]
    public enum RoomStates
    {
        EmoteOnlyOn,
        EmoteOnlyOff,
        FollowersOnlyOn,
        FollowersOnlyOff,
        R9KOn,
        R9KOff,
        SlowModeOn,
        SlowModeOff,
        SubsOnlyOn,
        SubsOnlyOff,
        None
    }
}
