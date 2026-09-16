using System.Collections.Generic;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// NX Point multipliers, and which of them the player is allowed to stake.
    ///
    /// The ladder is gated by <b>energy capacity</b>, not by the current balance: a player with a
    /// 20-energy tank may stake up to x5 however much energy is in it today. Capacity is something
    /// the player grows over time, so the ceiling rises with investment rather than with luck, and
    /// a new account can never wipe out its rank on its first match.
    ///
    /// The gate accelerates on purpose — 100 capacity buys x20, but x200 needs 5000. Early steps
    /// feel close enough to chase; the top of the ladder stays rare, which is what keeps a
    /// leaderboard made of NX Points worth reading.
    ///
    /// A match always costs the same single energy. The multiplier scales the NX Points won or
    /// lost, never the entry price — charging more energy for a bigger stake would gate the same
    /// thing twice and price out exactly the players the ladder is meant to pull upward.
    /// </summary>
    public static class MultiplierService
    {
        /// <summary>Every multiplier in the game, in ladder order.</summary>
        public static readonly int[] All = { 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000 };

        /// <summary>Energy capacity required for each multiplier above x2.</summary>
        private static readonly Dictionary<int, int> Required = new Dictionary<int, int>
        {
            { 1, 0 },
            { 2, 0 },
            { 5, 20 },
            { 10, 50 },
            { 20, 100 },
            { 50, 800 },
            { 100, 2000 },
            { 200, 5000 },
            { 500, 10000 },
            { 1000, 25000 },
        };

        /// <summary>Energy capacity the player's tank holds.</summary>
        public static int Capacity => EnergyService.Rules.max;

        public static int CapacityFor(int multiplier)
            => Required.TryGetValue(multiplier, out int need) ? need : int.MaxValue;

        public static bool IsUnlocked(int multiplier) => Capacity >= CapacityFor(multiplier);

        /// <summary>The best multiplier this account may stake right now.</summary>
        public static int Highest()
        {
            int best = 1;
            foreach (int m in All)
            {
                if (IsUnlocked(m)) best = m;
            }
            return best;
        }

        /// <summary>Wording for a locked tile, e.g. "Reach 800 max energy".</summary>
        public static string UnlockHint(int multiplier)
        {
            int need = CapacityFor(multiplier);
            return need == int.MaxValue ? "Locked" : $"Reach {need:n0} max energy";
        }

        /// <summary>
        /// NX Points at stake. Win pays the full multiple; a loss costs half as much, so climbing
        /// is faster than falling and a bad run never feels unrecoverable.
        /// </summary>
        public static int PointsOnWin(int basePoints, int multiplier) => basePoints * multiplier;

        public static int PointsOnLoss(int basePoints, int multiplier)
            => Mathf.RoundToInt(basePoints * multiplier * 0.5f);
    }
}
