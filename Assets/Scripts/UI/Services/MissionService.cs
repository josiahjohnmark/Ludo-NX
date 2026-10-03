using System;
using System.Collections.Generic;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// The mission ladder, and the player's level with it.
    ///
    /// There are no daily missions: ten fixed sets of five, played in order. The player's level
    /// IS the set they are working on — finish all five missions in set N and they reach level
    /// N + 1. Finishing set 10 makes them an NX Champion (level stays at 10, the badge cap).
    ///
    /// Progress only counts from the moment a set becomes active, so "Move 50 spaces" in set 2
    /// means fifty new spaces, not fifty since install. Every set is a fresh challenge.
    ///
    /// Three kinds of mission:
    ///   Total    — a running count since the set opened (rolls, spaces, captures, wins…).
    ///   OneMatch — every requirement true at the same moment inside a single match.
    ///   Streak   — consecutive wins, reset by any loss.
    ///
    /// Only the local human colour is ever credited — a bot's six is not the player's six.
    ///
    /// NOTE (go-live): like StatsService this is local and trusts the phone. When the server
    /// wallet lands (GO_LIVE_PLAN T-A3) the level-up reward moves to the claimMission function.
    /// </summary>
    public static class MissionService
    {
        public enum Scope { Total, OneMatch, Streak }

        public class Req
        {
            public string key;
            public int min;
            public int max = int.MaxValue;
            public Req(string key, int min, int max = int.MaxValue) { this.key = key; this.min = min; this.max = max; }
        }

        public class Mission
        {
            public string id;
            public string title;
            public string desc;
            public Scope scope;
            public Req[] reqs;

            /// <summary>What the bar counts to. Compound and win-conditional missions are pass/fail.</summary>
            public int Target
            {
                get
                {
                    if (scope == Scope.OneMatch && (reqs.Length > 1 || reqs[0].key == K.Won)) return 1;
                    return Mathf.Max(1, reqs[0].min);
                }
            }
        }

        public class MissionSet
        {
            public int level;
            public string name;
            public string difficulty;
            public Mission[] missions;
        }

        /// <summary>Counter and match-stat keys. One spelling, used by the table and the trackers.</summary>
        public static class K
        {
            // Total counters
            public const string Rolls = "rolls";
            public const string Spaces = "spaces";
            public const string Out = "out";
            public const string Home = "home";
            public const string Sixes = "sixes";
            public const string Captures = "captures";
            public const string Column = "column";
            public const string Wins = "wins";
            public const string VictimColours = "victim_colours";
            // Match stats
            public const string Won = "won";
            public const string Lost = "lost";
            public const string MaxActive = "max_active";
            public const string Moved = "moved";
            public const string FirstHome = "first_home";
            public const string Counter = "counter";
            public const string HomeRush = "home_rush";
        }

        /// <summary>Coins granted on reaching a new level: this × the level reached. Placeholder until the owner sets rewards.</summary>
        public const int LevelUpCoinsPerLevel = 100;

        public const int MaxLevel = 10;

        // ======================================================================================
        // the table — owner's "LUDO NX — REFINED MISSION PROGRESSION", 27 Sep 2026
        // ======================================================================================

        private static Mission T(string id, string title, string desc, string key, int n)
            => new Mission { id = id, title = title, desc = desc, scope = Scope.Total, reqs = new[] { new Req(key, n) } };

        private static Mission M(string id, string title, string desc, params Req[] reqs)
            => new Mission { id = id, title = title, desc = desc, scope = Scope.OneMatch, reqs = reqs };

        private static Mission S(string id, string title, string desc, int n)
            => new Mission { id = id, title = title, desc = desc, scope = Scope.Streak, reqs = new[] { new Req(K.Wins, n) } };

        private static Req R(string key, int min, int max = int.MaxValue) => new Req(key, min, max);

        public static readonly MissionSet[] Sets =
        {
            new MissionSet { level = 1, name = "FIRST STEPS", difficulty = "BEGINNER", missions = new[]
            {
                T("s1_roll",    "Ready to Roll",  "Roll the dice 10 times.",          K.Rolls, 10),
                T("s1_move",    "Get Moving",     "Move your tokens 25 spaces.",      K.Spaces, 25),
                T("s1_out",     "Into Play",      "Bring 2 tokens out of Base.",      K.Out, 2),
                T("s1_home",    "Home Bound",     "Get 1 token Home.",                K.Home, 1),
                T("s1_win",     "First Victory",  "Win 1 match.",                     K.Wins, 1),
            }},
            new MissionSet { level = 2, name = "ON THE BOARD", difficulty = "BEGINNER+", missions = new[]
            {
                T("s2_move",    "Keep Moving",    "Move your tokens 50 spaces.",      K.Spaces, 50),
                T("s2_out",     "Full Squad",     "Bring all 4 tokens out of Base.",  K.Out, 4),
                T("s2_six",     "Lucky Six",      "Roll a 6 three times.",            K.Sixes, 3),
                T("s2_capture", "First Capture",  "Capture 2 opponent tokens.",       K.Captures, 2),
                T("s2_home",    "Two at Home",    "Get 2 tokens Home.",               K.Home, 2),
            }},
            new MissionSet { level = 3, name = "THE HUNT", difficulty = "INTERMEDIATE", missions = new[]
            {
                T("s3_hunter",  "Hunter",         "Capture 3 opponent tokens.",       K.Captures, 3),
                M("s3_double",  "Double Strike",  "Capture 2 opponent tokens in one match.", R(K.Captures, 2)),
                M("s3_counter", "Counterattack",  "Capture an opponent after one of your tokens has been captured.", R(K.Counter, 1)),
                T("s3_column",  "Home Runner",    "Move a token into the Home Column.", K.Column, 1),
                T("s3_wins",    "Winning Habit",  "Win 2 matches.",                   K.Wins, 2),
            }},
            new MissionSet { level = 4, name = "HOME STRETCH", difficulty = "INTERMEDIATE", missions = new[]
            {
                T("s4_move",    "Long Road",      "Move your tokens 100 spaces.",     K.Spaces, 100),
                M("s4_active",  "Three Active",   "Have 3 tokens active on the board at the same time.", R(K.MaxActive, 3)),
                T("s4_home",    "Almost There",   "Get 3 tokens Home.",               K.Home, 3),
                M("s4_triple",  "Triple Capture", "Capture 3 opponent tokens in one match.", R(K.Captures, 3)),
                S("s4_streak",  "Back-to-Back",   "Win 2 matches in a row.",          2),
            }},
            new MissionSet { level = 5, name = "BOARD CONTROL", difficulty = "INTERMEDIATE+", missions = new[]
            {
                T("s5_six",     "Perfect Roll",   "Roll a 6 five times.",             K.Sixes, 5),
                M("s5_corners", "Four Corners",   "Move all 4 of your tokens during one match.", R(K.Moved, 4)),
                M("s5_first",   "First to Home",  "Be the first player to get a token Home.", R(K.FirstHome, 1)),
                T("s5_capture", "Capture Master", "Make 5 total captures.",           K.Captures, 5),
                M("s5_set",     "Complete the Set","Get all 4 tokens Home.",          R(K.Home, 4)),
            }},
            new MissionSet { level = 6, name = "PRESSURE", difficulty = "ADVANCED", missions = new[]
            {
                T("s6_capture", "Relentless",     "Make 7 total captures.",           K.Captures, 7),
                M("s6_force",   "Full Force",     "Capture 4 opponent tokens in one match.", R(K.Captures, 4)),
                M("s6_homerun", "Home Run",       "Get 2 tokens Home in one match.",  R(K.Home, 2)),
                M("s6_motion",  "Four in Motion", "Have all 4 tokens active on the board at the same time.", R(K.MaxActive, 4)),
                S("s6_streak",  "Winning Streak", "Win 3 matches in a row.",          3),
            }},
            new MissionSet { level = 7, name = "DOMINANCE", difficulty = "ADVANCED+", missions = new[]
            {
                M("s7_threat",  "Triple Threat",  "Have 3 active tokens and make 3 captures in one match.", R(K.MaxActive, 3), R(K.Captures, 3)),
                T("s7_colours", "Color Hunter",   "Capture tokens belonging to 3 different opponents.", K.VictimColours, 3),
                M("s7_four",    "Final Four",     "Get all 4 tokens Home in one match.", R(K.Home, 4)),
                M("s7_comeback","Comeback",       "Win a match after losing 2 or more tokens.", R(K.Won, 1), R(K.Lost, 2)),
                T("s7_wins",    "Unstoppable",    "Win 5 matches.",                   K.Wins, 5),
            }},
            new MissionSet { level = 8, name = "ELITE PLAY", difficulty = "EXPERT", missions = new[]
            {
                T("s8_capture", "Board Predator", "Make 8 total captures.",           K.Captures, 8),
                M("s8_control", "Perfect Control","Get 3 tokens Home and make at least 3 captures in one match.", R(K.Home, 3), R(K.Captures, 3)),
                M("s8_rush",    "Home Rush",      "Get 3 tokens Home before any opponent gets all 4 tokens Home.", R(K.HomeRush, 1)),
                M("s8_clean",   "Clean Victory",  "Win a match without losing any of your tokens.", R(K.Won, 1), R(K.Lost, 0, 0)),
                S("s8_streak",  "Win Streak",     "Win 4 matches in a row.",          4),
            }},
            new MissionSet { level = 9, name = "MASTER", difficulty = "EXPERT+", missions = new[]
            {
                T("s9_capture", "Master Hunter",  "Make 10 total captures.",          K.Captures, 10),
                M("s9_domination","Complete Domination","Capture 5 opponent tokens and get 3 of your own tokens Home in one match.", R(K.Captures, 5), R(K.Home, 3)),
                M("s9_untouch", "Untouchable",    "Win a match after losing no more than 1 token.", R(K.Won, 1), R(K.Lost, 0, 1)),
                M("s9_finish",  "Perfect Finish", "Get all 4 tokens Home and make at least 3 captures in one match.", R(K.Home, 4), R(K.Captures, 3)),
                S("s9_streak",  "Winning Run",    "Win 5 matches in a row.",          5),
            }},
            new MissionSet { level = 10, name = "NX CHAMPION", difficulty = "MASTER", missions = new[]
            {
                T("s10_capture","Elite Hunter",   "Make 15 total captures.",          K.Captures, 15),
                T("s10_move",   "Board Master",   "Move 200 total spaces.",           K.Spaces, 200),
                M("s10_control","Absolute Control","Get all 4 tokens Home while making at least 4 captures in one match.", R(K.Home, 4), R(K.Captures, 4)),
                M("s10_flawless","Flawless Champion","Win a match without losing a single token.", R(K.Won, 1), R(K.Lost, 0, 0)),
                T("s10_wins",   "NX Champion",    "Win 10 matches.",                  K.Wins, 10),
            }},
        };

        // ======================================================================================
        // saved state
        // ======================================================================================

        [Serializable]
        private class State
        {
            public int setIndex;                                  // 0..9 working, 10 = champion
            public List<string> keys = new List<string>();        // set-scoped Total counters
            public List<int> values = new List<int>();
            public int[] best = new int[5];                       // best bar value per mission
            public bool[] done = new bool[5];
            public int streak;                                    // consecutive wins, global
            public int victimMask;                                // set-scoped colours captured
            public int pendingLevelUps;                           // celebrated when Levels opens
        }

        private const string PrefKey = "ludonx_missions_v1";
        private static State _s;

        public static event Action Changed;
        /// <summary>Raised with the level just reached.</summary>
        public static event Action<int> LevelUp;

        private static State S0
        {
            get
            {
                if (_s != null) return _s;
                string json = PlayerPrefs.GetString(PrefKey, "");
                try { _s = string.IsNullOrEmpty(json) ? new State() : JsonUtility.FromJson<State>(json); }
                catch { _s = new State(); }
                if (_s.best == null || _s.best.Length != 5) _s.best = new int[5];
                if (_s.done == null || _s.done.Length != 5) _s.done = new bool[5];
                return _s;
            }
        }

        private static void Save()
        {
            PlayerPrefs.SetString(PrefKey, JsonUtility.ToJson(S0));
            PlayerPrefs.Save();
            if (Changed != null) Changed();
        }

        internal static void ResetStatics() { _s = null; _m = null; }

        // ======================================================================================
        // what screens ask
        // ======================================================================================

        public static int SetIndex => Mathf.Clamp(S0.setIndex, 0, Sets.Length);
        public static bool IsChampion => S0.setIndex >= Sets.Length;
        public static int Level => Mathf.Clamp(S0.setIndex + 1, 1, MaxLevel);
        public static MissionSet CurrentSet => Sets[Mathf.Min(SetIndex, Sets.Length - 1)];

        public static int DoneCount
        {
            get
            {
                if (IsChampion) return 5;
                int n = 0;
                foreach (bool d in S0.done) if (d) n++;
                return n;
            }
        }

        /// <summary>0–1 through the current level.</summary>
        public static float LevelProgress01 => DoneCount / 5f;

        /// <summary>Status of a mission in any set: below the current set = done, above = locked.</summary>
        public static bool IsDone(int setIndex, int i)
        {
            if (setIndex < SetIndex) return true;
            if (setIndex > SetIndex) return false;
            return IsChampion || S0.done[i];
        }

        public static bool IsLocked(int setIndex) => setIndex > SetIndex;

        public static int Progress(int setIndex, int i)
        {
            var m = Sets[setIndex].missions[i];
            if (IsDone(setIndex, i)) return m.Target;
            if (setIndex != SetIndex) return 0;
            return Mathf.Clamp(S0.best[i], 0, m.Target);
        }

        /// <summary>Levels reached since the Levels screen last celebrated. Reading clears it.</summary>
        public static int TakePendingLevelUps()
        {
            int n = S0.pendingLevelUps;
            if (n == 0) return 0;
            S0.pendingLevelUps = 0;
            Save();
            return n;
        }

        // ======================================================================================
        // match tracking — per-match state is deliberately not saved: a match that is quit
        // half way must not leave half a "one match" achievement behind.
        // ======================================================================================

        private class MatchState
        {
            public int captures, home, lost, maxActive, victimMask;
            public bool firstHome, counter, homeRush, anyoneHome;
            public readonly HashSet<TokenMovement> moved = new HashSet<TokenMovement>();
            public readonly Dictionary<GameManager.PlayerColor, int> finished = new Dictionary<GameManager.PlayerColor, int>();
            public bool won;
        }

        private static MatchState _m;

        private static bool IsMe(GameManager.PlayerColor c)
        {
            var gm = GameManager.Instance;
            return gm != null && gm.humanColor == c;
        }

        public static void BeginMatch() { _m = new MatchState(); }

        private static MatchState Match => _m ?? (_m = new MatchState());

        public static void OnRoll(GameManager.PlayerColor who, int d1, int d2)
        {
            if (IsChampion || !IsMe(who)) return;
            Add(K.Rolls, 1);
            int sixes = (d1 == 6 ? 1 : 0) + (d2 == 6 ? 1 : 0);
            if (sixes > 0) Add(K.Sixes, sixes);
            Evaluate();
        }

        public static void OnTokenLeftBase(TokenMovement t)
        {
            if (IsChampion || t == null || !IsMe(t.tokenColor)) return;
            Add(K.Out, 1);
            Match.moved.Add(t);
            TrackActive(t.tokenColor);
            Evaluate();
        }

        public static void OnTokenMoved(TokenMovement t, int steps, bool enteredColumn)
        {
            if (IsChampion || t == null || !IsMe(t.tokenColor)) return;
            Add(K.Spaces, steps);
            if (enteredColumn) Add(K.Column, 1);
            Match.moved.Add(t);
            TrackActive(t.tokenColor);
            Evaluate();
        }

        public static void OnCapture(GameManager.PlayerColor capturer, GameManager.PlayerColor victim)
        {
            if (IsChampion) return;
            var m = Match;
            if (IsMe(victim)) { m.lost++; Evaluate(); return; }
            if (!IsMe(capturer)) return;

            m.captures++;
            if (m.lost > 0) m.counter = true;
            int bit = 1 << (int)victim;
            m.victimMask |= bit;
            if ((S0.victimMask & bit) == 0)
            {
                S0.victimMask |= bit;
                Set(K.VictimColours, CountBits(S0.victimMask));
            }
            Add(K.Captures, 1);
            TrackActive(capturer);
            Evaluate();
        }

        public static void OnTokenFinished(GameManager.PlayerColor who)
        {
            if (IsChampion) return;
            var m = Match;
            m.finished.TryGetValue(who, out int f);
            m.finished[who] = f + 1;

            if (IsMe(who))
            {
                if (!m.anyoneHome) m.firstHome = true;
                m.home++;
                Add(K.Home, 1);
                if (m.home >= 3 && !m.homeRush)
                {
                    bool opponentDone = false;
                    foreach (var kv in m.finished) if (kv.Key != who && kv.Value >= 4) opponentDone = true;
                    if (!opponentDone) m.homeRush = true;
                }
            }
            m.anyoneHome = true;
            Evaluate();
        }

        /// <summary>Called once when the result is known. Win-conditional missions resolve here.</summary>
        public static void EndMatch(bool won)
        {
            if (IsChampion) { _m = null; return; }
            var m = Match;
            m.won = won;
            if (won) { Add(K.Wins, 1); S0.streak++; }
            else S0.streak = 0;
            Evaluate();
            _m = null;
        }

        private static void TrackActive(GameManager.PlayerColor colour)
        {
            int active = 0;
            foreach (var t in UnityEngine.Object.FindObjectsByType<TokenMovement>(FindObjectsInactive.Exclude))
                if (t.tokenColor == colour && !t.isHome && !t.isFinished) active++;
            if (active > Match.maxActive) Match.maxActive = active;
        }

        private static int CountBits(int v) { int n = 0; while (v != 0) { n += v & 1; v >>= 1; } return n; }

        // ======================================================================================
        // counters and evaluation
        // ======================================================================================

        private static int Get(string key)
        {
            int i = S0.keys.IndexOf(key);
            return i < 0 ? 0 : S0.values[i];
        }

        private static void Set(string key, int value)
        {
            int i = S0.keys.IndexOf(key);
            if (i < 0) { S0.keys.Add(key); S0.values.Add(value); }
            else S0.values[i] = value;
        }

        private static void Add(string key, int amount) => Set(key, Get(key) + amount);

        private static int MatchValue(string key)
        {
            var m = Match;
            switch (key)
            {
                case K.Captures: return m.captures;
                case K.Home: return m.home;
                case K.Lost: return m.lost;
                case K.MaxActive: return m.maxActive;
                case K.Moved: return m.moved.Count;
                case K.FirstHome: return m.firstHome ? 1 : 0;
                case K.Counter: return m.counter ? 1 : 0;
                case K.HomeRush: return m.homeRush ? 1 : 0;
                case K.Won: return m.won ? 1 : 0;
                default: return 0;
            }
        }

        private static void Evaluate()
        {
            if (IsChampion) { Save(); return; }
            var set = CurrentSet;
            for (int i = 0; i < 5; i++)
            {
                if (S0.done[i]) continue;
                var ms = set.missions[i];
                int value;
                bool complete;
                switch (ms.scope)
                {
                    case Scope.Total:
                        value = Get(ms.reqs[0].key);
                        complete = value >= ms.reqs[0].min;
                        break;
                    case Scope.Streak:
                        value = S0.streak;
                        complete = value >= ms.reqs[0].min;
                        break;
                    default:
                        complete = true;
                        foreach (var r in ms.reqs)
                        {
                            int v = MatchValue(r.key);
                            if (v < r.min || v > r.max) { complete = false; break; }
                        }
                        // Pass/fail bars stay empty until they pass; single-stat ones show the best match.
                        value = ms.Target == 1 ? (complete ? 1 : 0) : MatchValue(ms.reqs[0].key);
                        break;
                }
                if (value > S0.best[i]) S0.best[i] = Mathf.Min(value, ms.Target);
                if (complete) { S0.done[i] = true; S0.best[i] = ms.Target; }
            }

            bool levelled = false;
            while (!IsChampion && DoneCount == 5)
            {
                S0.setIndex++;
                S0.keys.Clear();
                S0.values.Clear();
                S0.best = new int[5];
                S0.done = new bool[5];
                S0.victimMask = 0;
                S0.pendingLevelUps++;
                levelled = true;

                int reached = Mathf.Min(S0.setIndex + 1, MaxLevel);
                var session = LudoNXSession.Instance;
                if (session != null) session.AddRewards(LevelUpCoinsPerLevel * reached, 0);
                if (LevelUp != null) LevelUp(reached);
            }
            Save();
            if (levelled) Debug.Log($"[Missions] Level up -> {Level}{(IsChampion ? " (NX Champion)" : "")}");
        }

        // ======================================================================================
        // test helpers — screen tests and the owner's demo, never called by gameplay
        // ======================================================================================

        /// <summary>Completes the next unfinished mission in the current set.</summary>
        public static void DebugCompleteNext()
        {
            if (IsChampion) return;
            for (int i = 0; i < 5; i++)
            {
                if (S0.done[i]) continue;
                S0.done[i] = true;
                S0.best[i] = CurrentSet.missions[i].Target;
                break;
            }
            Evaluate();
        }

        public static void DebugReset()
        {
            _s = new State();
            _m = null;
            Save();
        }
    }
}
