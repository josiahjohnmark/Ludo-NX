using System;
using System.Collections.Generic;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// The three missions on offer today, their progress, and what has been claimed.
    ///
    /// Daily only — no weekly or monthly ladder. The three are drawn from the pool by a seed made
    /// of the date, so every device shows the same set on the same day without a server, and the
    /// set is stable if the player closes the game and comes back.
    ///
    /// Progress is reported by gameplay through <see cref="Report"/>. Until the match screen is
    /// rebuilt nothing calls it, so <see cref="SeedDemoProgress"/> fills in a plausible morning's
    /// play the first time the screen is opened; it is a development convenience and the flag is
    /// turned off for the shipping build.
    ///
    /// Reset is local midnight. The device clock can be moved forward to farm a second set — the
    /// same trade <see cref="EnergyService"/> makes, and the same fix: a server timestamp, once
    /// there is a server.
    /// </summary>
    public static class DailyMissionsData
    {
        public enum Kind { PlayMatches, WinMatches, CaptureTokens, RollSixes, TokensHome, WinStreak }

        [Serializable]
        public class Mission
        {
            public string id;
            public Kind kind;
            public string title;
            public int target;
            public int rewardCoins;
            public int rewardNx;
            public string icon;       // file name inside Art/UI/Missions

            public Mission(string id, Kind kind, string title, int target, int coins, int nx, string icon)
            {
                this.id = id; this.kind = kind; this.title = title;
                this.target = target; rewardCoins = coins; rewardNx = nx; this.icon = icon;
            }
        }

        /// <summary>Set false before shipping so progress only ever comes from real matches.</summary>
        public const bool SeedDemoProgress = true;

        public const int BonusCoins = 250;
        public const int BonusNx = 40;

        private static readonly Mission[] Pool =
        {
            new Mission("play3",  Kind.PlayMatches,   "Play 3 matches",            3,  90,  10, "icon_dice"),
            new Mission("win2",   Kind.WinMatches,    "Win 2 matches",             2, 150,  20, "icon_badge"),
            new Mission("cap5",   Kind.CaptureTokens, "Capture 5 tokens",          5, 120,  15, "icon_stat"),
            new Mission("six6",   Kind.RollSixes,     "Roll six 6s",               6, 100,  12, "icon_dice"),
            new Mission("home4",  Kind.TokensHome,    "Get 4 tokens home",         4, 110,  14, "icon_badge"),
            new Mission("streak2",Kind.WinStreak,     "Win 2 matches in a row",    2, 200,  30, "icon_stat"),
            new Mission("play5",  Kind.PlayMatches,   "Play 5 matches",            5, 140,  18, "icon_dice"),
            new Mission("cap3",   Kind.CaptureTokens, "Capture 3 tokens",          3,  80,  10, "icon_stat"),
        };

        private const string KeyDay = "ludonx_missions_day";
        private const string KeyProgress = "ludonx_mission_p_";
        private const string KeyClaimed = "ludonx_mission_c_";
        private const string KeyBonus = "ludonx_mission_bonus";

        private static List<Mission> _today;
        private static string _todayKey;

        /// <summary>Raised whenever progress or a claim changes, so an open screen can refresh.</summary>
        public static event Action Changed;

        private static string DayKey => DateTime.Now.ToString("yyyyMMdd");

        /// <summary>Today's three missions, in a fixed order.</summary>
        public static List<Mission> Today()
        {
            string day = DayKey;
            if (_today != null && _todayKey == day) return _today;

            if (PlayerPrefs.GetString(KeyDay, "") != day) ResetForNewDay(day);

            // A date-seeded shuffle: same three everywhere on the same day, different tomorrow.
            var rng = new System.Random(int.Parse(day));
            var bag = new List<Mission>(Pool);
            var picked = new List<Mission>(3);
            var families = new HashSet<string>();
            while (picked.Count < 3 && bag.Count > 0)
            {
                var m = bag[rng.Next(bag.Count)];
                bag.Remove(m);
                // One per family, so a day never offers "Win 2 matches" beside "Win 2 in a row" —
                // near-duplicates make the set look shorter than it is.
                if (families.Contains(Family(m.kind))) continue;
                families.Add(Family(m.kind));
                picked.Add(m);
            }

            _today = picked;
            _todayKey = day;
            return _today;
        }

        private static void ResetForNewDay(string day)
        {
            foreach (var m in Pool)
            {
                PlayerPrefs.DeleteKey(KeyProgress + m.id);
                PlayerPrefs.DeleteKey(KeyClaimed + m.id);
            }
            PlayerPrefs.DeleteKey(KeyBonus);
            PlayerPrefs.SetString(KeyDay, day);
            PlayerPrefs.Save();

            if (SeedDemoProgress)
            {
                // A believable half-finished morning: one nearly done, one started, one untouched.
                var rng = new System.Random(int.Parse(day) + 7);
                var set = Today_NoReset(day);
                for (int i = 0; i < set.Count; i++)
                {
                    int p = i == 0 ? Mathf.Max(1, set[i].target - 1)
                          : i == 1 ? rng.Next(0, Mathf.Max(1, set[i].target))
                          : 0;
                    PlayerPrefs.SetInt(KeyProgress + set[i].id, p);
                }
                PlayerPrefs.Save();
            }
        }

        /// <summary>The day's picks without touching storage — used while resetting.</summary>
        private static List<Mission> Today_NoReset(string day)
        {
            var rng = new System.Random(int.Parse(day));
            var bag = new List<Mission>(Pool);
            var picked = new List<Mission>(3);
            var kinds = new HashSet<Kind>();
            while (picked.Count < 3 && bag.Count > 0)
            {
                var m = bag[rng.Next(bag.Count)];
                bag.Remove(m);
                if (kinds.Contains(m.kind)) continue;
                kinds.Add(m.kind);
                picked.Add(m);
            }
            return picked;
        }

        /// <summary>Kinds that ask the player for the same thing, and so must not appear together.</summary>
        private static string Family(Kind kind) => kind switch
        {
            Kind.WinMatches or Kind.WinStreak => "win",
            Kind.PlayMatches => "play",
            Kind.CaptureTokens => "capture",
            Kind.RollSixes => "roll",
            _ => "home",
        };

        public static int Progress(Mission m) => Mathf.Clamp(PlayerPrefs.GetInt(KeyProgress + m.id, 0), 0, m.target);
        public static bool IsComplete(Mission m) => Progress(m) >= m.target;
        public static bool IsClaimed(Mission m) => PlayerPrefs.GetInt(KeyClaimed + m.id, 0) == 1;
        public static bool BonusClaimed => PlayerPrefs.GetInt(KeyBonus, 0) == 1;

        public static bool AllClaimed()
        {
            foreach (var m in Today())
            {
                if (!IsClaimed(m)) return false;
            }
            return true;
        }

        public static int ClaimedCount()
        {
            int n = 0;
            foreach (var m in Today())
            {
                if (IsClaimed(m)) n++;
            }
            return n;
        }

        /// <summary>True when there is a reward waiting — drives the red dot on the home badge.</summary>
        public static bool HasClaimable()
        {
            foreach (var m in Today())
            {
                if (IsComplete(m) && !IsClaimed(m)) return true;
            }
            return AllClaimed() && !BonusClaimed;
        }

        /// <summary>Pays out a finished mission. Returns false if it was not ready or already paid.</summary>
        public static bool Claim(Mission m)
        {
            if (!IsComplete(m) || IsClaimed(m)) return false;
            PlayerPrefs.SetInt(KeyClaimed + m.id, 1);
            PlayerPrefs.Save();
            Grant(m.rewardCoins, m.rewardNx);
            Changed?.Invoke();
            return true;
        }

        public static bool ClaimBonus()
        {
            if (!AllClaimed() || BonusClaimed) return false;
            PlayerPrefs.SetInt(KeyBonus, 1);
            PlayerPrefs.Save();
            Grant(BonusCoins, BonusNx);
            Changed?.Invoke();
            return true;
        }

        private static void Grant(int coins, int nx)
        {
            var session = LudoNXSession.Instance;
            if (session != null) session.AddRewards(coins, nx);
        }

        /// <summary>Called by gameplay when something a mission counts happens.</summary>
        public static void Report(Kind kind, int amount = 1)
        {
            bool touched = false;
            foreach (var m in Today())
            {
                if (m.kind != kind || IsComplete(m)) continue;
                PlayerPrefs.SetInt(KeyProgress + m.id, Progress(m) + amount);
                touched = true;
            }
            if (!touched) return;
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>Time left before the set rolls over, for the countdown in the header.</summary>
        public static TimeSpan TimeUntilReset()
            => DateTime.Today.AddDays(1) - DateTime.Now;

        public static string ResetLabel()
        {
            var t = TimeUntilReset();
            return t.TotalHours >= 1
                ? $"New missions in {(int)t.TotalHours}h {t.Minutes}m"
                : $"New missions in {t.Minutes}m";
        }
    }
}
