namespace LudoNX.Shop
{
    /// <summary>
    /// The energy packs, in one place. Product IDs must match Google Play Console and the
    /// RevenueCat `default` offering exactly (package identifier = product ID).
    ///
    /// Prices are the owner's list (27 Sep 2026). They are only a fallback label — on a device
    /// the shop shows RevenueCat's localised price string instead, which is what Play charges.
    /// </summary>
    public static class EnergyCatalog
    {
        public struct Pack
        {
            public string productId;
            public int energy;
            public string fallbackPrice;
            public string tag;   // optional ribbon text, empty for none
        }

        public static readonly Pack[] Packs =
        {
            new Pack { productId = "energy_10",   energy = 10,   fallbackPrice = "$0.49",  tag = "" },
            new Pack { productId = "energy_25",   energy = 25,   fallbackPrice = "$0.99",  tag = "" },
            new Pack { productId = "energy_50",   energy = 50,   fallbackPrice = "$1.49",  tag = "" },
            new Pack { productId = "energy_100",  energy = 100,  fallbackPrice = "$2.49",  tag = "POPULAR" },
            new Pack { productId = "energy_250",  energy = 250,  fallbackPrice = "$4.99",  tag = "" },
            new Pack { productId = "energy_500",  energy = 500,  fallbackPrice = "$7.99",  tag = "" },
            new Pack { productId = "energy_1000", energy = 1000, fallbackPrice = "$12.99", tag = "BEST VALUE" },
            new Pack { productId = "energy_2500", energy = 2500, fallbackPrice = "$24.99", tag = "" },
        };

        /// <summary>Energy granted for a product, 0 if it is not an energy pack.</summary>
        public static int EnergyFor(string productId)
        {
            foreach (var p in Packs) if (p.productId == productId) return p.energy;
            return 0;
        }

        /// <summary>RevenueCat's localised price when the store has answered, else the fallback.</summary>
        public static string PriceFor(Pack pack)
        {
            var rc = RevenueCatManager.Instance;
            if (rc != null)
            {
                var pkg = rc.FindPackage(pack.productId);
                if (pkg != null && pkg.StoreProduct != null && !string.IsNullOrEmpty(pkg.StoreProduct.PriceString))
                    return pkg.StoreProduct.PriceString;
            }
            return pack.fallbackPrice;
        }
    }
}
