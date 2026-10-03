using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Whether the app should be blocked from playing right now, and why — server maintenance, or
    /// a client build too old to talk to the current backend.
    ///
    /// Both are local flags today, deliberately: there is no remote config yet (that is Firebase,
    /// Phase A, unstarted per GO_LIVE_PLAN.md). Splash reads these two properties and nothing else
    /// needs to change here once remote config exists — only how they get set does. Until then,
    /// flip <see cref="IsUnderMaintenance"/> by hand for a maintenance-mode test build.
    /// </summary>
    public static class MaintenanceService
    {
        public static bool IsUnderMaintenance = false;
        public static string MaintenanceMessage =
            "We're making a few improvements. Thanks for your patience.";

        /// <summary>The oldest client version the backend still accepts. "1.0.0" means every
        /// build so far is fine — raise it the day a build ships that an old client can't talk to.</summary>
        public static string MinimumVersion = "1.0.0";

        public static bool IsVersionSupported()
        {
            return CompareVersions(Application.version, MinimumVersion) >= 0;
        }

        /// <summary>Simple dotted-numeric compare (1.2.3 vs 1.10.0) — string comparison alone gets
        /// "1.10.0" < "1.2.0" wrong.</summary>
        private static int CompareVersions(string a, string b)
        {
            var pa = a.Split('.');
            var pb = b.Split('.');
            int len = Mathf.Max(pa.Length, pb.Length);
            for (int i = 0; i < len; i++)
            {
                int va = i < pa.Length && int.TryParse(pa[i], out int x) ? x : 0;
                int vb = i < pb.Length && int.TryParse(pb[i], out int y) ? y : 0;
                if (va != vb) return va.CompareTo(vb);
            }
            return 0;
        }
    }
}
