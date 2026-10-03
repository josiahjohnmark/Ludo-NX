using System;
using System.Collections.Generic;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Weekly NX Points leaderboard and reward distribution.
    /// Tracks weekly points, ranks, and distributes weekly Energy rewards.
    /// </summary>
    public static class RankingsService
    {
        public class LeaderboardEntry
        {
            public int rank;
            public string name;
            public string playerId;
            public int weeklyPoints;
            public int energyReward;
            public bool isLocalPlayer;
        }

        private const string KeyWeeklyNx = "ludonx_weekly_nx";
        private const string KeyWeekStamp = "ludonx_weekly_stamp";

        public static int WeeklyNxPoints
        {
            get
            {
                CheckWeekRollover();
                return PlayerPrefs.GetInt(KeyWeeklyNx, 0);
            }
        }

        public static void AddWeeklyPoints(int points)
        {
            if (points <= 0) return;
            CheckWeekRollover();
            int current = PlayerPrefs.GetInt(KeyWeeklyNx, 0);
            PlayerPrefs.SetInt(KeyWeeklyNx, current + points);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Rewards based on weekly rank:
        /// 1st: 100 energy | 2nd-3rd: 75 energy | 4th-10th: 50 energy | 11th-50th: 30 energy | 51st-100th: 20 energy | Top 500: 10 energy
        /// </summary>
        public static int GetEnergyRewardForRank(int rank)
        {
            if (rank == 1) return 100;
            if (rank <= 3) return 75;
            if (rank <= 10) return 50;
            if (rank <= 50) return 30;
            if (rank <= 100) return 20;
            return 10;
        }

        /// <summary>Returns formatted time until the weekly tournament ends (every Sunday midnight UTC).</summary>
        public static string TimeRemainingFormatted
        {
            get
            {
                DateTime now = DateTime.UtcNow;
                int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
                if (daysUntilSunday == 0 && now.Hour == 0) daysUntilSunday = 7;
                DateTime end = now.Date.AddDays(daysUntilSunday);
                TimeSpan diff = end - now;
                if (diff.TotalDays >= 1) return $"{diff.Days}d {diff.Hours}h";
                return $"{diff.Hours}h {diff.Minutes}m";
            }
        }

        public static List<LeaderboardEntry> GetWeeklyLeaderboard()
        {
            CheckWeekRollover();
            var list = new List<LeaderboardEntry>();
            int playerPts = WeeklyNxPoints;
            var session = LudoNXSession.Instance;
            string localName = session != null ? session.PlayerName : "You";
            string localId = session != null ? session.PlayerId : "608315";

            // Synthetic top competitors around the player's score bracket
            string[] topNames = { "Musa D.", "Chioma E.", "Amara O.", "Tunde B.", "Kofi A.", "Omar H.", "Zainab K.", "Chidi N.", "Fatima Y.", "Emeka V." };
            int baseScore = Mathf.Max(2400, playerPts + 350);

            for (int i = 0; i < 10; i++)
            {
                int score = baseScore - i * 180;
                list.Add(new LeaderboardEntry
                {
                    rank = i + 1,
                    name = topNames[i],
                    playerId = (200000 + i * 111).ToString(),
                    weeklyPoints = score,
                    energyReward = GetEnergyRewardForRank(i + 1),
                    isLocalPlayer = false
                });
            }

            // Insert local player into ranking
            int localRank = 1;
            for (int i = 0; i < list.Count; i++)
            {
                if (playerPts < list[i].weeklyPoints) localRank = i + 2;
            }

            var playerEntry = new LeaderboardEntry
            {
                rank = localRank,
                name = localName,
                playerId = localId,
                weeklyPoints = playerPts,
                energyReward = GetEnergyRewardForRank(localRank),
                isLocalPlayer = true
            };

            if (localRank <= list.Count)
            {
                list.Insert(localRank - 1, playerEntry);
                for (int i = 0; i < list.Count; i++) list[i].rank = i + 1;
            }
            else
            {
                list.Add(playerEntry);
            }

            return list;
        }

        private static void CheckWeekRollover()
        {
            string savedStamp = PlayerPrefs.GetString(KeyWeekStamp, "");
            DateTime now = DateTime.UtcNow;
            string currentWeek = $"{now.Year}-W{GetIso8601WeekOfYear(now)}";

            if (savedStamp != currentWeek)
            {
                if (!string.IsNullOrEmpty(savedStamp))
                {
                    // Grant energy rewards for previous week
                    int previousRank = 12; // default placement
                    int reward = GetEnergyRewardForRank(previousRank);
                    EnergyService.Grant(reward, allowOverfill: true);
                    Debug.Log($"[Ludo NX] Weekly tournament ended! Granted {reward} energy.");
                }

                PlayerPrefs.SetString(KeyWeekStamp, currentWeek);
                PlayerPrefs.SetInt(KeyWeeklyNx, 0);
                PlayerPrefs.Save();
            }
        }

        private static int GetIso8601WeekOfYear(DateTime time)
        {
            DayOfWeek day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }
            return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
