using System.Collections.Generic;

using Windows.UI;

namespace Multitool.Net.Twitch
{
    internal record GlobalUserState(
        string BadgeInfo,
        List<string> Badges,
        Color Color,
        string DisplayName,
        List<int> EmoteSets,
        bool HasTurbo,
        string UserId,
        string UserType
    );
}
