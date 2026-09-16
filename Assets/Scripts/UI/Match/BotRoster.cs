using System.Collections.Generic;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// The stand-in opponents for quick matches while there is no server.
    ///
    /// They exist because an empty matchmaker is worse than a simulated one: a player who taps
    /// QUICK MATCH and waits forever stops playing. Each bot carries a name, an avatar, a home
    /// country and — the part that matters — a <see cref="Style"/> that changes how it plays, so
    /// two matches in a row do not feel like the same opponent wearing a different face.
    ///
    /// Honesty rule: a bot is never described in the UI as a person who is online. The mode is
    /// called QUICK MATCH, not ONLINE, and nothing claims a real opponent was found. When a real
    /// server arrives, the matchmaker replaces <see cref="Pick"/> and no screen changes.
    /// </summary>
    public static class BotRoster
    {
        /// <summary>
        /// How a bot plays. These map onto the AI's weights — they are not cosmetic labels.
        /// Aggression drives how often it takes a capture it can see; caution drives how much it
        /// values safe squares; haste drives how eagerly it races a token home instead of
        /// developing all four; blunder is the chance it takes the second-best move on purpose,
        /// which is what stops a bot from feeling like a solver.
        /// </summary>
        public enum Style { Hunter, Runner, Guardian, Blocker, Gambler, Steady }

        public class Bot
        {
            public string name;
            public string country;      // two-letter code, matches the Flags art
            public Style style;
            public int avatar;          // index into the generated avatar set
            public int rating;          // rough NX standing, used to match like with like

            public float Aggression => style switch
            {
                Style.Hunter => 0.95f, Style.Gambler => 0.8f, Style.Blocker => 0.55f,
                Style.Steady => 0.5f, Style.Runner => 0.35f, _ => 0.25f
            };

            public float Caution => style switch
            {
                Style.Guardian => 0.9f, Style.Blocker => 0.75f, Style.Steady => 0.6f,
                Style.Hunter => 0.3f, Style.Runner => 0.25f, _ => 0.15f
            };

            public float Haste => style switch
            {
                Style.Runner => 0.95f, Style.Gambler => 0.7f, Style.Hunter => 0.5f,
                Style.Steady => 0.45f, Style.Blocker => 0.3f, _ => 0.25f
            };

            /// <summary>Deliberate imperfection. A bot that never errs reads as a cheat.</summary>
            public float Blunder => Mathf.Clamp01(0.30f - rating / 40000f);

            /// <summary>Seconds it appears to "think" before moving, so turns feel human.</summary>
            public Vector2 ThinkSeconds => style switch
            {
                Style.Gambler => new Vector2(0.35f, 0.9f),
                Style.Runner => new Vector2(0.45f, 1.1f),
                Style.Guardian => new Vector2(0.9f, 2.0f),
                Style.Blocker => new Vector2(0.8f, 1.8f),
                _ => new Vector2(0.6f, 1.5f)
            };

            public string StyleLabel => style switch
            {
                Style.Hunter => "Hunter",
                Style.Runner => "Runner",
                Style.Guardian => "Guardian",
                Style.Blocker => "Blocker",
                Style.Gambler => "Gambler",
                _ => "All-rounder"
            };

            public Bot(string name, string country, Style style, int avatar, int rating)
            {
                this.name = name; this.country = country; this.style = style;
                this.avatar = avatar; this.rating = rating;
            }
        }

        /// <summary>
        /// Twenty is enough that a regular player rarely meets the same name twice in a session,
        /// and few enough to keep every one of them hand-tuned.
        /// </summary>
        public static readonly List<Bot> All = new List<Bot>
        {
            new Bot("Amara O.",   "NG", Style.Hunter,   0,   420),
            new Bot("Deji A.",    "NG", Style.Runner,   1,   980),
            new Bot("Chioma E.",  "NG", Style.Guardian, 2,  1650),
            new Bot("Ravi S.",    "IN", Style.Blocker,  3,  2400),
            new Bot("Priya N.",   "IN", Style.Gambler,  4,   760),
            new Bot("Arjun M.",   "IN", Style.Steady,   5,  3100),
            new Bot("Lucas F.",   "BR", Style.Hunter,   6,  1200),
            new Bot("Beatriz C.", "BR", Style.Runner,   7,  5400),
            new Bot("Mei L.",     "CN", Style.Guardian, 8,   640),
            new Bot("Kenji T.",   "JP", Style.Blocker,  9,  2850),
            new Bot("Yuki H.",    "JP", Style.Gambler, 10,  1450),
            new Bot("Sofia N.",   "ES", Style.Steady,  11,   890),
            new Bot("Mateo R.",   "MX", Style.Hunter,  12,  4200),
            new Bot("Zara P.",    "PK", Style.Runner,  13,  1900),
            new Bot("Omar H.",    "EG", Style.Guardian,14,   520),
            new Bot("Layla K.",   "MA", Style.Blocker, 15,  6800),
            new Bot("Noah W.",    "US", Style.Gambler, 16,  2100),
            new Bot("Grace B.",   "GB", Style.Steady,  17,  3600),
            new Bot("Tomas V.",   "PL", Style.Hunter,  18,  1100),
            new Bot("Ivan D.",    "RU", Style.Runner,  19,  7500),
        };

        private static string _lastName;

        /// <summary>
        /// An opponent near the player's own standing, never the one they just played.
        /// Widens the net until something fits, so matchmaking cannot fail.
        /// </summary>
        public static Bot Pick(int playerRating)
        {
            for (float window = 0.35f; window <= 4f; window *= 2f)
            {
                var pool = new List<Bot>();
                int spread = Mathf.Max(400, Mathf.RoundToInt(playerRating * window) + 400);
                foreach (var b in All)
                {
                    if (b.name == _lastName) continue;
                    if (Mathf.Abs(b.rating - playerRating) <= spread) pool.Add(b);
                }
                if (pool.Count > 0)
                {
                    var choice = pool[Random.Range(0, pool.Count)];
                    _lastName = choice.name;
                    return choice;
                }
            }

            var any = All[Random.Range(0, All.Count)];
            _lastName = any.name;
            return any;
        }

        /// <summary>Distinct opponents for a vs-computer match, so no two share a name.</summary>
        public static List<Bot> PickMany(int count, int playerRating)
        {
            var taken = new List<Bot>();
            var pool = new List<Bot>(All);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int best = 0;
                float bestScore = float.MaxValue;
                for (int j = 0; j < pool.Count; j++)
                {
                    // Spread the styles out: prefer a bot whose rating is close but whose style
                    // is not already at the table.
                    float score = Mathf.Abs(pool[j].rating - playerRating);
                    foreach (var t in taken)
                    {
                        if (t.style == pool[j].style) score += 5000f;
                    }
                    score += Random.Range(0f, 900f);
                    if (score < bestScore) { bestScore = score; best = j; }
                }
                taken.Add(pool[best]);
                pool.RemoveAt(best);
            }
            return taken;
        }
    }
}
