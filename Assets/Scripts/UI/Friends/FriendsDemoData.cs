using System;
using System.Collections.Generic;

namespace LudoNX.UI
{
    /// <summary>One person shown on the Friends screen: a friend, an incoming request, or a search result.</summary>
    [Serializable]
    public class FriendPerson
    {
        public string name;
        public string playerId;
        public int avatar;
        public bool online;
        public string subtitle;
        public bool requestSent;

        public FriendPerson(string name, string playerId, int avatar, bool online, string subtitle)
        {
            this.name = name;
            this.playerId = playerId;
            this.avatar = avatar;
            this.online = online;
            this.subtitle = subtitle;
        }

        /// <summary>Case-insensitive name match, or a player ID that starts with the query.</summary>
        public bool Matches(string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            return name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   playerId.StartsWith(query, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Sample people for the Friends screen until a real backend exists. The game has no
    /// online service yet, so every list here is local and resets when the app restarts.
    ///
    /// Search tests:
    ///   "jo"      -> friend Josiah J., players Joy A. and Jonah K.
    ///   "ma"      -> friends Amara O., Omar H., Mateo R.; players Maya C., Fatima Y.
    ///   "100458"  -> player Daniel E. (search by ID)
    ///   "204781"  -> friend Josiah J. (search by ID)
    ///   "zzz"     -> "No players found"
    /// </summary>
    public static class FriendsDemoData
    {
        public static List<FriendPerson> CreateFriends() => new List<FriendPerson>();

        public static List<FriendPerson> CreateRequests() => new List<FriendPerson>();

        public static List<FriendPerson> CreatePlayers() => new List<FriendPerson>
        {
            new FriendPerson("Joy A.",    "100245", 21, true,  "Player Since: 2022"),
            new FriendPerson("Jonah K.",  "100312", 22, false, "Player Since: 2021"),
            new FriendPerson("Daniel E.", "100458", 23, true,  "Player Since: 2019"),
            new FriendPerson("Esther I.", "100561", 24, false, "Player Since: 2023"),
            new FriendPerson("Kelvin U.", "100677", 25, true,  "Player Since: 2020"),
            new FriendPerson("Maya C.",   "100789", 26, true,  "Player Since: 2024"),
            new FriendPerson("Liam F.",   "100804", 27, false, "Player Since: 2022"),
            new FriendPerson("Ngozi R.",  "100915", 28, true,  "Player Since: 2021"),
            new FriendPerson("Ahmed S.",  "101026", 29, false, "Player Since: 2023"),
            new FriendPerson("Chloe V.",  "101137", 30, true,  "Player Since: 2020"),
            new FriendPerson("Ethan G.",  "101248", 31, false, "Player Since: 2025"),
            new FriendPerson("Fatima Y.", "101359", 32, true,  "Player Since: 2022"),
        };
    }
}
