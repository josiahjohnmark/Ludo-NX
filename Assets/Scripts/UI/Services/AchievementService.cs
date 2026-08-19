using System.Collections.Generic;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// One achievement definition. Progress is derived from <see cref="CareerStats"/> rather than
    /// stored separately, so nothing can drift out of sync with the numbers on the profile.
    /// </summary>
    public class Achievement
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Description;
        public readonly int Target;

        /// <summary>Current value, read from career stats.</summary>
        public readonly System.Func<CareerStats, int> Progress;

        public Achievement(string id, string title, string description, int target,
                           System.Func<CareerStats, int> progress)
        {
            Id = id; Title = title; Description = description; Target = target; Progress = progress;
        }

        public int Current(CareerStats d) => Mathf.Min(Progress(d), Target);
        public bool IsUnlocked(CareerStats d) => Progress(d) >= Target;
        public float Fraction(CareerStats d) => Target <= 0 ? 0f : Mathf.Clamp01(Progress(d) / (float)Target);
    }

    /// <summary>
    /// The achievement list for v1 — twenty goals covering the things a Ludo player actually
    /// does: showing up, winning, capturing, getting tokens home and finishing well.
    ///
    /// They deliberately span very different effort levels. The first few unlock inside one
    /// session so a new player sees the system work; the last few take weeks, so there is
    /// something left to chase.
    /// </summary>
    public static class AchievementService
    {
        private static List<Achievement> _all;

        internal static void ResetStatics() { _all = null; }

        public static List<Achievement> All
        {
            get
            {
                if (_all != null) return _all;
                _all = new List<Achievement>
                {
                    // ── Getting started ──────────────────────────────────────────────────
                    new Achievement("first_match",  "First Roll",       "Play your first match",              1,   d => d.matches),
                    new Achievement("first_win",    "Winner",           "Win your first match",               1,   d => d.wins),
                    new Achievement("first_capture","First Blood",      "Capture an opponent's token",        1,   d => d.captures),
                    new Achievement("first_home",   "Safe and Sound",   "Get a token all the way home",       1,   d => d.tokensHome),

                    // ── Playing regularly ────────────────────────────────────────────────
                    new Achievement("play_10",      "Regular",          "Play 10 matches",                    10,  d => d.matches),
                    new Achievement("play_50",      "Devoted",          "Play 50 matches",                    50,  d => d.matches),
                    new Achievement("play_200",     "Board Veteran",    "Play 200 matches",                   200, d => d.matches),

                    // ── Winning ──────────────────────────────────────────────────────────
                    new Achievement("win_10",       "Contender",        "Win 10 matches",                     10,  d => d.wins),
                    new Achievement("win_50",       "Champion",         "Win 50 matches",                     50,  d => d.wins),
                    new Achievement("win_100",      "Ludo Master",      "Win 100 matches",                    100, d => d.wins),

                    // ── Streaks ──────────────────────────────────────────────────────────
                    new Achievement("streak_3",     "On a Roll",        "Win 3 matches in a row",             3,   d => d.bestStreak),
                    new Achievement("streak_5",     "Unstoppable",      "Win 5 matches in a row",             5,   d => d.bestStreak),
                    new Achievement("streak_10",    "Untouchable",      "Win 10 matches in a row",            10,  d => d.bestStreak),

                    // ── Aggression ───────────────────────────────────────────────────────
                    new Achievement("cap_25",       "Hunter",           "Capture 25 tokens",                  25,  d => d.captures),
                    new Achievement("cap_100",      "Predator",         "Capture 100 tokens",                 100, d => d.captures),
                    new Achievement("cap_500",      "Board Tyrant",     "Capture 500 tokens",                 500, d => d.captures),

                    // ── Completion ───────────────────────────────────────────────────────
                    new Achievement("home_50",      "Homecoming",       "Bring 50 tokens home",               50,  d => d.tokensHome),
                    new Achievement("home_250",     "Pathfinder",       "Bring 250 tokens home",              250, d => d.tokensHome),

                    // ── Skill ────────────────────────────────────────────────────────────
                    new Achievement("mvp_10",       "Most Valuable",    "Earn MVP in 10 matches",             10,  d => d.mvps),
                    new Achievement("podium_25",    "Podium Regular",   "Finish 1st or 2nd 25 times",         25,  d => d.finish1 + d.finish2),
                };
                return _all;
            }
        }

        public static int UnlockedCount(CareerStats d)
        {
            int n = 0;
            foreach (var a in All) if (a.IsUnlocked(d)) n++;
            return n;
        }

        public static int Total => All.Count;
    }
}
