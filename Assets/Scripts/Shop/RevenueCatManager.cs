using System.Collections.Generic;
using UnityEngine;

namespace LudoNX.Shop
{
    /// <summary>
    /// Owns the store connection: brings RevenueCat up, keeps the player's entitlements current,
    /// and holds the offerings the shop screen will read.
    ///
    /// This sits alongside the SDK's own <c>Purchases</c> component rather than replacing it.
    /// Purchases does the talking to Google Play; this decides what the game does with the
    /// answers, so shop UI never has to know about SDK types or callback ordering.
    ///
    /// Two things about this SDK are worth knowing before testing anything:
    ///
    ///   • In the Unity Editor the SDK binds a no-op backend. Nothing connects, nothing errors,
    ///     and every call silently does nothing. A clean console in the Editor therefore proves
    ///     the code compiles and runs — it does NOT prove the store works. That needs a real
    ///     build on a real device, signed with the same key as the Play Console upload.
    ///   • Purchases configures itself in Start() from its Inspector fields. Anything that reads
    ///     entitlements has to wait for the first customer-info callback rather than assuming
    ///     they are ready, which is what <see cref="Ready"/> is for.
    /// </summary>
    [AddComponentMenu("Ludo NX/Shop/RevenueCat Manager")]
    // AFTER Purchases (default order 0), never before. Purchases binds its platform backend in
    // its own Start(); calling into the SDK ahead of that dereferences a null wrapper. Shop UI
    // ordering does not matter here because it reads state through Ready and the events below
    // rather than by racing this component's Start.
    [DefaultExecutionOrder(50)]
    public class RevenueCatManager : MonoBehaviour
    {
        public static RevenueCatManager Instance { get; private set; }

        [Header("Entitlements")]
        [Tooltip("Entitlement identifier, exactly as spelled in the RevenueCat dashboard. " +
                 "Anything the player has bought permanently is granted through this.")]
        public string premiumEntitlementId = "premium";

        [Header("Diagnostics")]
        [Tooltip("Verbose SDK logging. Leave on until the first real purchase works on device, " +
                 "then turn it off — it is noisy and logs customer identifiers.")]
        public bool verboseLogging = true;

        /// <summary>True once the first customer-info response has arrived.</summary>
        public bool Ready { get; private set; }

        /// <summary>Raised when entitlements change, including the first time they load.</summary>
        public event System.Action EntitlementsChanged;

        /// <summary>Raised once the offerings are fetched, so a shop screen can populate.</summary>
        public event System.Action OfferingsLoaded;

        public Purchases.Offerings Offerings { get; private set; }

        private readonly HashSet<string> _activeEntitlements = new HashSet<string>();
        private Purchases _purchases;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _purchases = GetComponent<Purchases>();
            if (_purchases == null)
            {
                Debug.LogError("[RevenueCat] No Purchases component on this object. " +
                               "The SDK cannot configure itself without one.");
            }
        }

        private void Start()
        {
            if (_purchases == null) return;

            _purchases.SetLogLevel(verboseLogging ? Purchases.LogLevel.Debug : Purchases.LogLevel.Warn);

            // Safe here: this component is ordered after Purchases, so the SDK is configured.
            _purchases.GetCustomerInfo(OnCustomerInfo);
            _purchases.GetOfferings(OnOfferings);

#if UNITY_EDITOR
            Debug.Log("[RevenueCat] Editor session: the SDK is running its no-op backend, so no " +
                      "store connection is made and no purchase can be tested here. Build to an " +
                      "Android device to exercise the real flow.");
#endif
        }

        // ── SDK callbacks ─────────────────────────────────────────────────────────────────

        private void OnCustomerInfo(Purchases.CustomerInfo info, Purchases.Error error)
        {
            if (error != null)
            {
                Debug.LogWarning("[RevenueCat] Could not read customer info: " + error.Message);
                return;
            }
            ApplyCustomerInfo(info);
        }

        private void OnOfferings(Purchases.Offerings offerings, Purchases.Error error)
        {
            if (error != null)
            {
                Debug.LogWarning("[RevenueCat] Could not fetch offerings: " + error.Message);
                return;
            }

            Offerings = offerings;

            int count = offerings != null && offerings.All != null ? offerings.All.Count : 0;
            Debug.Log("[RevenueCat] Offerings loaded: " + count
                      + (offerings != null && offerings.Current != null
                          ? ", current = " + offerings.Current.Identifier
                          : ", no current offering set"));

            if (OfferingsLoaded != null) OfferingsLoaded();
        }

        private void ApplyCustomerInfo(Purchases.CustomerInfo info)
        {
            _activeEntitlements.Clear();

            if (info != null && info.Entitlements != null && info.Entitlements.Active != null)
            {
                foreach (var kv in info.Entitlements.Active) _activeEntitlements.Add(kv.Key);
            }

            Ready = true;
            if (EntitlementsChanged != null) EntitlementsChanged();
        }

        // ── What the game asks ────────────────────────────────────────────────────────────

        /// <summary>Does the player currently hold this entitlement?</summary>
        public bool HasEntitlement(string entitlementId)
        {
            return !string.IsNullOrEmpty(entitlementId) && _activeEntitlements.Contains(entitlementId);
        }

        /// <summary>Shorthand for the single "has paid for the premium bundle" question.</summary>
        public bool IsPremium { get { return HasEntitlement(premiumEntitlementId); } }

        /// <summary>
        /// True when the store answered and there is something to sell. False in the Editor,
        /// and on device until the first offerings call returns — the shop uses this to say so
        /// rather than presenting a buy button that cannot work.
        /// </summary>
        public bool StoreReady
        {
            get { return Offerings != null && Offerings.Current != null; }
        }

        /// <summary>
        /// Buys a product by its store identifier.
        ///
        /// Reports success as a plain bool because that is all a shop row needs to know; a
        /// cancellation and a failure both mean "do not grant anything", and only a real error
        /// is worth logging. Entitlements are refreshed on success so anything gated on them
        /// updates without the caller having to ask.
        /// </summary>
        public void Buy(string productId, System.Action<bool> done)
        {
            if (_purchases == null || string.IsNullOrEmpty(productId))
            {
                if (done != null) done(false);
                return;
            }

            _purchases.PurchaseProduct(productId, result =>
            {
                if (result == null || result.Error != null)
                {
                    if (result != null && result.Error != null && !result.UserCancelled)
                        Debug.LogWarning("[RevenueCat] Purchase failed: " + result.Error.Message);
                    if (done != null) done(false);
                    return;
                }

                if (result.UserCancelled) { if (done != null) done(false); return; }

                ApplyCustomerInfo(result.CustomerInfo);
                if (done != null) done(true);
            });
        }

        /// <summary>
        /// Restores prior purchases. Google Play requires a reachable restore path for
        /// non-consumable products, so the shop screen must expose this as a visible control.
        /// </summary>
        public void RestorePurchases(System.Action<bool> done = null)
        {
            if (_purchases == null) { if (done != null) done(false); return; }

            _purchases.RestorePurchases((info, error) =>
            {
                bool ok = error == null;
                if (ok) ApplyCustomerInfo(info);
                else Debug.LogWarning("[RevenueCat] Restore failed: " + error.Message);
                if (done != null) done(ok);
            });
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
