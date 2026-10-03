using UnityEngine;

namespace LudoNX.UI
{
    /// <summary>
    /// Calculates player Level and Badge unlocks strictly from cumulative NX Points earned.
    /// Follows an exponential prestige curve across 10 Badges.
    /// </summary>
    public static class NXLevelService
    {
        public struct BadgeInfo
        {
            public int level;
            public string title;
            public int nxRequired;
            public int energyRewardOnUnlock;
        }

        public static readonly BadgeInfo[] Badges = new BadgeInfo[10]
        {
            new BadgeInfo { level = 1,  title = "ROOKIE",       nxRequired = 0,      energyRewardOnUnlock = 0 },
            new BadgeInfo { level = 2,  title = "APPRENTICE",   nxRequired = 250,    energyRewardOnUnlock = 10 },
            new BadgeInfo { level = 3,  title = "COMPETITOR",   nxRequired = 750,    energyRewardOnUnlock = 15 },
            new BadgeInfo { level = 4,  title = "TACTICIAN",    nxRequired = 1800,   energyRewardOnUnlock = 20 },
            new BadgeInfo { level = 5,  title = "STRATEGIST",   nxRequired = 4000,   energyRewardOnUnlock = 25 },
            new BadgeInfo { level = 6,  title = "MASTER",       nxRequired = 8500,   energyRewardOnUnlock = 30 },
            new BadgeInfo { level = 7,  title = "GRANDMASTER",  nxRequired = 18000,  energyRewardOnUnlock = 40 },
            new BadgeInfo { level = 8,  title = "CHAMPION",     nxRequired = 38000,  energyRewardOnUnlock = 50 },
            new BadgeInfo { level = 9,  title = "LEGEND",       nxRequired = 80000,  energyRewardOnUnlock = 75 },
            new BadgeInfo { level = 10, title = "MYTHIC",       nxRequired = 150000, energyRewardOnUnlock = 100 }
        };

        public static int GetLevel(int nxPoints)
        {
            for (int i = Badges.Length - 1; i >= 0; i--)
            {
                if (nxPoints >= Badges[i].nxRequired) return Badges[i].level;
            }
            return 1;
        }

        public static BadgeInfo GetBadge(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, Badges.Length - 1);
            return Badges[index];
        }

        public static float GetProgressToNextLevel(int nxPoints)
        {
            int lv = GetLevel(nxPoints);
            if (lv >= 10) return 1f;
            int floor = Badges[lv - 1].nxRequired;
            int ceil = Badges[lv].nxRequired;
            return Mathf.Clamp01((float)(nxPoints - floor) / (ceil - floor));
        }

        public static int GetPointsRemainingToNext(int nxPoints)
        {
            int lv = GetLevel(nxPoints);
            if (lv >= 10) return 0;
            return Mathf.Max(0, Badges[lv].nxRequired - nxPoints);
        }
    }
}
