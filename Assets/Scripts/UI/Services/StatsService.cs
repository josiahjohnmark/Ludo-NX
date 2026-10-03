using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Career statistics, persisted as JSON.
    ///
    /// <c>PointsManager</c> already computes captures, tokens home, times captured, doubles and
    /// fire streak for every match — and then throws all of it away when the match ends. This is
    /// where it goes instead, so Profile and Stats have real numbers rather than placeholders.
    /// </summary>
    [System.Serializable]
    public class CareerStats
    {
        public int matches;
        public int wins;
        public int losses;
        public int captures;
        public int timesCaptured;
        public int tokensHome;
        public int mvps;
        public int bestStreak;
        public int currentStreak;
        public int highestScore;
        public int fastestWinSeconds;

        /// <summary>Cumulative match-points across all games. Fed by RecordMatch.</summary>
        public int totalPoints;

        /// <summary>Total time spent in matches, seconds. Fed by RecordMatch.</summary>
        public int totalPlaySeconds;

        // Single-match bests, for the profile.
        public int mostCapturesInGame;
        public int mostTokensHomeInGame;

        /// <summary>
        /// NX earned from matches over the whole career. Only ever grows: a staked loss takes NX
        /// off the balance, but it does not un-earn what was won before. Fed by RecordNx.
        /// </summary>
        public int nxEarned;

        /// <summary>Most NX earned from a single match. Fed by RecordNx.</summary>
        public int bestNxInGame;

        /// <summary>Tournaments won. Stays 0 until tournaments exist; fed by RecordTournamentWin.</summary>
        public int tournamentWins;

        // Finish positions 1st..4th
        public int finish1, finish2, finish3, finish4;

        // Split records
        public int twoPlayerMatches, twoPlayerWins;
        public int fourPlayerMatches, fourPlayerWins;
        public int casualMatches, casualWins;
        public int rankedMatches, rankedWins;
        public int privateMatches, privateWins;

        // ── Computed ──────────────────────────────────────────────────────────────────────────
        public float WinRate => matches == 0 ? 0f : wins / (float)matches;
        public float TwoPlayerWinRate   => twoPlayerMatches  == 0 ? 0f : twoPlayerWins  / (float)twoPlayerMatches;
        public float FourPlayerWinRate  => fourPlayerMatches == 0 ? 0f : fourPlayerWins / (float)fourPlayerMatches;
        public float CasualWinRate      => casualMatches     == 0 ? 0f : casualWins     / (float)casualMatches;
        public float RankedWinRate      => rankedMatches     == 0 ? 0f : rankedWins     / (float)rankedMatches;
        public float PrivateWinRate     => privateMatches    == 0 ? 0f : privateWins    / (float)privateMatches;

        public float CapturesPerGame => matches == 0 ? 0f : captures / (float)matches;
        public float NxPerGame       => matches == 0 ? 0f : nxEarned / (float)matches;

        /// <summary>Alias for highestScore so screens can use a self-describing name.</summary>
        public int bestScore => highestScore;

        /// <summary>Alias for timesCaptured — how many times the player was sent home.</summary>
        public int captured => timesCaptured;
    }

    public static class StatsService
    {
        private const string Key = "ludonx_career";

        private static CareerStats _cache;

        internal static void ResetStatics() { _cache = null; }

        public static CareerStats Data
        {
            get
            {
                if (_cache != null) return _cache;
                string json = PlayerPrefs.GetString(Key, null);
                _cache = string.IsNullOrEmpty(json) ? new CareerStats() : JsonUtility.FromJson<CareerStats>(json);
                if (_cache == null) _cache = new CareerStats();
                return _cache;
            }
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Banks a finished match.
        /// </summary>
        /// <param name="placement">1-based finishing position.</param>
        /// <param name="playerCount">2 or 4.</param>
        /// <param name="score">The player's PointsManager score.</param>
        /// <param name="captures">Opponents captured this match.</param>
        /// <param name="timesCaptured">Times the player was sent home.</param>
        /// <param name="tokensHome">Tokens the player got home.</param>
        /// <param name="wasMvp">Whether the player took MVP.</param>
        /// <param name="durationSeconds">Match length, for the fastest-win record.</param>
        public static void RecordMatch(int placement, int playerCount, int score, int captures,
                                       int timesCaptured, int tokensHome, bool wasMvp, int durationSeconds)
        {
            var d = Data;
            bool won = placement == 1;

            d.matches++;
            if (won) d.wins++; else d.losses++;
            d.captures += Mathf.Max(0, captures);
            d.timesCaptured += Mathf.Max(0, timesCaptured);
            d.tokensHome += Mathf.Max(0, tokensHome);
            if (wasMvp) d.mvps++;
            if (score > d.highestScore) d.highestScore = score;
            if (captures > d.mostCapturesInGame) d.mostCapturesInGame = captures;
            if (tokensHome > d.mostTokensHomeInGame) d.mostTokensHomeInGame = tokensHome;

            switch (Mathf.Clamp(placement, 1, 4))
            {
                case 1: d.finish1++; break;
                case 2: d.finish2++; break;
                case 3: d.finish3++; break;
                default: d.finish4++; break;
            }

            if (playerCount >= 4) { d.fourPlayerMatches++; if (won) d.fourPlayerWins++; }
            else { d.twoPlayerMatches++; if (won) d.twoPlayerWins++; }

            // A streak is consecutive wins; a loss ends it.
            if (won)
            {
                d.currentStreak++;
                if (d.currentStreak > d.bestStreak) d.bestStreak = d.currentStreak;
                if (durationSeconds > 0 && (d.fastestWinSeconds == 0 || durationSeconds < d.fastestWinSeconds))
                    d.fastestWinSeconds = durationSeconds;
            }
            else
            {
                d.currentStreak = 0;
            }

            Save();
        }

        /// <summary>
        /// Banks the NX a match paid out. Called after the session settles the stake, because
        /// that is where the multiplier is applied. A loss (negative NX) earns nothing.
        /// </summary>
        public static void RecordNx(int nxFromMatch)
        {
            if (nxFromMatch <= 0) return;
            var d = Data;
            d.nxEarned += nxFromMatch;
            if (nxFromMatch > d.bestNxInGame) d.bestNxInGame = nxFromMatch;
            Save();
        }

        public static void RecordTournamentWin()
        {
            Data.tournamentWins++;
            Save();
        }

        public static void Reset()
        {
            _cache = new CareerStats();
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        /// <summary>"4m 12s", or "—" when there is no record yet.</summary>
        public static string FormatDuration(int seconds)
        {
            if (seconds <= 0) return "—";
            return (seconds / 60) + "m " + (seconds % 60).ToString("00") + "s";
        }
    }
}
