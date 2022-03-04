using System;

namespace Multitool.Net.Irc.Twitch
{
    public interface IIrcSubscriber
    {
        void OnDisconnected(IIrcClient client, EventArgs args);
        void OnMessageReceived(IIrcClient client, Message args);
        void OnRoomChanged(IIrcClient client, RoomStateEventArgs args);
        void OnUserTimedOut(IIrcClient client, UserTimeoutEventArgs args);
        void OnUserNotice(IIrcClient client, UserNoticeEventArgs args);
    }
}
