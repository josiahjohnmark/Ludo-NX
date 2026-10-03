using System.Collections.Generic;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Stand-in opponents for quick matches.
    /// Includes 50 uniquely tuned bots:
    /// - 35 Smart Bots with mid-to-high levels (Levels 8–28) and strategic playstyles.
    /// - 15 Easy Bots with lower levels (Levels 2–7) for accessible introductory play.
    /// All bots use natural names, distinct tactical styles, and no external flag dependency.
    /// </summary>
    public static class BotRoster
    {
        public enum Style { Hunter, Runner, Guardian, Blocker, Gambler, Steady }

        public class Bot
        {
            public string name;
            public string country;      // empty for now since flags are disabled
            public Style style;
            public int level;           // Bot account level (2 to 28)
            public int avatar;          // index into the generated avatar set
            public int rating;          // rough NX standing

            public float Aggression => style switch
            {
                Style.Hunter => 0.95f,
                Style.Gambler => 0.80f,
                Style.Blocker => 0.55f,
                Style.Steady => 0.60f,
                Style.Runner => 0.40f,
                _ => 0.35f
            };

            public float Caution => style switch
            {
                Style.Guardian => 0.92f,
                Style.Blocker => 0.80f,
                Style.Steady => 0.65f,
                Style.Hunter => 0.30f,
                Style.Runner => 0.25f,
                _ => 0.20f
            };

            public float Haste => style switch
            {
                Style.Runner => 0.95f,
                Style.Gambler => 0.70f,
                Style.Hunter => 0.55f,
                Style.Steady => 0.50f,
                Style.Blocker => 0.30f,
                _ => 0.25f
            };

            /// <summary>Smart bots (level >= 8) rarely err; easy bots (level &lt; 8) make human mistakes.</summary>
            public float Blunder =>
                level >= 22 ? 0.02f :
                level >= 16 ? 0.04f :
                level >= 12 ? 0.06f :
                level >= 8  ? 0.08f : 0.28f;

            /// <summary>Simulated human reaction time.</summary>
            public Vector2 ThinkSeconds => style switch
            {
                Style.Gambler => new Vector2(0.40f, 0.85f),
                Style.Runner => new Vector2(0.45f, 1.05f),
                Style.Guardian => new Vector2(0.85f, 1.90f),
                Style.Blocker => new Vector2(0.75f, 1.60f),
                _ => new Vector2(0.55f, 1.35f)
            };

            public string StyleLabel => style switch
            {
                Style.Hunter => "Aggressive Hunter",
                Style.Runner => "Speed Rusher",
                Style.Guardian => "Tactical Defender",
                Style.Blocker => "Blocker",
                Style.Gambler => "Casual Player",
                _ => "Balanced Strategist"
            };

            public Bot(string name, Style style, int level, int avatar, int rating, string country = "")
            {
                this.name = name;
                this.style = style;
                this.level = level;
                this.avatar = avatar;
                this.rating = rating;
                this.country = country;
            }
        }

        public static readonly List<Bot> All = new List<Bot>
        {
            // ======================================================================================
            // 35 Smart Bots (Levels 8 to 28 — balanced, tactically aware, low blunder rate)
            // ======================================================================================
            new Bot("Marcus Vance",    Style.Hunter,   19, 0,  3450),
            new Bot("Elena Rostova",   Style.Steady,   24, 1,  4600),
            new Bot("Tariq Mansoor",   Style.Guardian, 22, 2,  4100),
            new Bot("Chloe Dupont",    Style.Runner,   16, 3,  2800),
            new Bot("Liam O'Connor",   Style.Hunter,   20, 4,  3750),
            new Bot("Priya Sharma",    Style.Steady,   25, 5,  4900),
            new Bot("Lucas Silva",     Style.Hunter,   18, 6,  3200),
            new Bot("Sofia Morales",   Style.Guardian, 21, 7,  3950),
            new Bot("Arjun Mehta",     Style.Steady,   27, 8,  5400),
            new Bot("Zoe Jenkins",     Style.Runner,   17, 9,  3050),
            new Bot("Amara Okafor",    Style.Hunter,   23, 10, 4400),
            new Bot("Daniel Kim",      Style.Steady,   26, 11, 5150),
            new Bot("Hannah Becker",   Style.Guardian, 19, 12, 3600),
            new Bot("Gabriel Santos",  Style.Runner,   15, 13, 2650),
            new Bot("Maya Lin",        Style.Steady,   28, 14, 5600),
            new Bot("Ethan Cole",      Style.Hunter,   14, 15, 2500),
            new Bot("Fatima Zahra",    Style.Guardian, 22, 16, 4250),
            new Bot("Samir Patel",     Style.Steady,   20, 17, 3800),
            new Bot("Nora Lindqvist",  Style.Runner,   18, 18, 3350),
            new Bot("Deji Adeleke",    Style.Hunter,   21, 19, 4000),
            new Bot("Isabella Rossi",  Style.Guardian, 16, 0,  2900),
            new Bot("Kai Takahashi",   Style.Steady,   25, 1,  4800),
            new Bot("Julian Mercer",   Style.Hunter,   19, 2,  3550),
            new Bot("Layla Benali",    Style.Runner,   23, 3,  4350),
            new Bot("Tomas Novak",     Style.Guardian, 15, 4,  2750),
            new Bot("Grace Campbell",  Style.Steady,   24, 5,  4700),
            new Bot("Mateo Alvarez",   Style.Hunter,   22, 6,  4150),
            new Bot("Aisha Bello",     Style.Runner,   17, 7,  3100),
            new Bot("Kenji Sato",      Style.Steady,   26, 8,  5200),
            new Bot("Olivia Bennett",  Style.Guardian, 18, 9,  3400),
            new Bot("Ravi Nair",       Style.Hunter,   20, 10, 3850),
            new Bot("Beatriz Costa",   Style.Runner,   27, 11, 5350),
            new Bot("David Miller",    Style.Steady,   16, 12, 2950),
            new Bot("Chioma Eze",      Style.Guardian, 21, 13, 3900),
            new Bot("Alex Rivera",     Style.Hunter,   23, 14, 4450),

            // ======================================================================================
            // 15 Beginner / Easy Bots (Levels 2 to 7 — friendly casual opponents, accessible play)
            // ======================================================================================
            new Bot("Oliver Fox",      Style.Gambler,  3,  15, 350),
            new Bot("Mia Wallace",     Style.Blocker,  5,  16, 620),
            new Bot("Leo Turner",      Style.Gambler,  2,  17, 280),
            new Bot("Emma Davies",     Style.Blocker,  4,  18, 490),
            new Bot("Noah Reed",       Style.Gambler,  6,  19, 780),
            new Bot("Ruby Cooper",     Style.Blocker,  3,  0,  390),
            new Bot("Jack Bell",       Style.Gambler,  7,  1,  920),
            new Bot("Lily Adams",      Style.Blocker,  4,  2,  530),
            new Bot("Harry Evans",     Style.Gambler,  5,  3,  670),
            new Bot("Ava Mitchell",    Style.Blocker,  2,  4,  310),
            new Bot("Oscar Price",     Style.Gambler,  6,  5,  840),
            new Bot("Ella Brooks",     Style.Blocker,  5,  6,  710),
            new Bot("Charlie Wood",    Style.Gambler,  3,  7,  420),
            new Bot("Sophie Ward",     Style.Blocker,  7,  8,  950),
            new Bot("Archie Gray",     Style.Gambler,  4,  9,  580),
        };

        private static string _lastName;

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
