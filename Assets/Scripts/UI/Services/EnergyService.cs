using System;
using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Lives. A match costs one, they refill on a timer, and the shop sells more.
    ///
    /// Stored as "how many were left, and when" rather than as a running count, so the refill is
    /// computed from the clock on every read. That means energy accrues correctly while the game
    /// is closed — the alternative, ticking a counter only while running, silently punishes the
    /// player for quitting and is the classic way this system is got wrong.
    ///
    /// Clock note: this trusts the device clock, so moving it forward grants energy early. That is
    /// the accepted trade for a single-player refill — the fix is a server timestamp, which only
    /// becomes worth building when matches are online and there is something real to protect.
    /// </summary>
    public static class EnergyService
    {
        private const string KeyCount = "ludonx_energy_count";
        private const string KeyStamp = "ludonx_energy_stamp";   // ticks, UTC

        [Serializable]
        public class Config
        {
            public int max = 5;
            public int costPerMatch = 1;
            public int refillMinutes = 15;
        }

        public static readonly Config Rules = new Config();

        /// <summary>Raised whenever the balance or the countdown changes meaningfully.</summary>
        public static event Action Changed;

        private static int _cached = -1;
        private static DateTime _stamp;

        internal static void ResetStatics() { _cached = -1; }

        private static void Load()
        {
            if (_cached >= 0) return;
            _cached = PlayerPrefs.GetInt(KeyCount, Rules.max);

            long ticks = 0;
            string raw = PlayerPrefs.GetString(KeyStamp, "");
            if (!string.IsNullOrEmpty(raw)) long.TryParse(raw, out ticks);
            _stamp = ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.UtcNow;
        }

        private static void Save()
        {
            PlayerPrefs.SetInt(KeyCount, _cached);
            PlayerPrefs.SetString(KeyStamp, _stamp.Ticks.ToString());
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Brings the balance up to date with the clock, then reports it. Every read goes through
        /// here so no caller can observe a stale count.
        /// </summary>
        public static int Current
        {
            get
            {
                Load();
                if (_cached >= Rules.max) { _stamp = DateTime.UtcNow; return _cached; }

                double minutes = (DateTime.UtcNow - _stamp).TotalMinutes;
                if (minutes <= 0) return _cached;

                int earned = (int)(minutes / Rules.refillMinutes);
                if (earned <= 0) return _cached;

                int before = _cached;
                _cached = Mathf.Min(Rules.max, _cached + earned);

                // Advance the stamp by exactly what was consumed, never to "now" — rounding the
                // remainder away here would quietly reset the player's progress toward the next
                // life every time the screen refreshed.
                _stamp = _stamp.AddMinutes(earned * (double)Rules.refillMinutes);
                if (_cached >= Rules.max) _stamp = DateTime.UtcNow;

                Save();
                if (_cached != before && Changed != null) Changed();
                return _cached;
            }
        }

        public static int Max { get { return Rules.max; } }
        public static bool IsFull { get { return Current >= Rules.max; } }
        public static bool CanPlay { get { return Current >= Rules.costPerMatch; } }

        /// <summary>Time until the next life. Zero when full.</summary>
        public static TimeSpan UntilNext
        {
            get
            {
                if (IsFull) return TimeSpan.Zero;
                DateTime due = _stamp.AddMinutes(Rules.refillMinutes);
                TimeSpan left = due - DateTime.UtcNow;
                return left < TimeSpan.Zero ? TimeSpan.Zero : left;
            }
        }

        /// <summary>Formatted for a HUD chip: M:SS under an hour, H:MM above.</summary>
        public static string UntilNextText
        {
            get
            {
                TimeSpan t = UntilNext;
                if (t <= TimeSpan.Zero) return "FULL";
                return t.TotalHours >= 1d
                    ? string.Format("{0}:{1:00}", (int)t.TotalHours, t.Minutes)
                    : string.Format("{0}:{1:00}", t.Minutes, t.Seconds);
            }
        }

        /// <summary>Spends the cost of one match. False — and nothing spent — if short.</summary>
        public static bool TrySpendForMatch()
        {
            if (Current < Rules.costPerMatch) return false;

            // Starting the countdown from a full tank is what makes the first life feel like it
            // takes the advertised time rather than however long the tank happened to sit full.
            if (_cached >= Rules.max) _stamp = DateTime.UtcNow;

            _cached -= Rules.costPerMatch;
            Save();
            if (Changed != null) Changed();
            return true;
        }

        /// <summary>Grants energy from a purchase or a reward.</summary>
        public static void Grant(int amount, bool allowOverfill = false)
        {
            if (amount <= 0) return;
            Load();
            _cached = allowOverfill ? _cached + amount : Mathf.Min(Rules.max, _cached + amount);
            if (_cached >= Rules.max && !allowOverfill) _stamp = DateTime.UtcNow;
            Save();
            if (Changed != null) Changed();
        }
    }
}
