# LUDO NX — Go-Live Plan (online play, friends, real shop)

**Date:** 26 Sep 2026 · **Owner:** Nanzing · **Director:** Claude · **Also builds:** Gemini (Antigravity)
**Status:** waiting on the owner's setup steps in §2. Progress is tracked in `AI_HANDOFF.md`.

---

## 1. Decision: Firebase (not Supabase)

On 16 Sep Claude suggested Supabase for a small backend. Now that the goal is **real online matches, real friends and a real shop inside a Unity game on Google Play**, Firebase is the better fit:

| Need | Firebase | Supabase |
|---|---|---|
| Unity support | **Official Google Unity SDK** (Auth, Firestore, Realtime Database, Functions, Messaging, Crashlytics, Analytics) | Community C# library only — risky on Android builds that take 30 minutes each |
| Sign-in on Android | Guest (anonymous), Google, **Google Play Games** | Guest, Google — no Play Games |
| Online presence ("online" dot) | Built in (Realtime Database `onDisconnect`) | Possible, more work |
| Push notifications (friend requests, challenges) | Built in (Cloud Messaging) | Needs Firebase anyway |
| RevenueCat | Official RevenueCat integration + webhooks | Webhooks only |
| Crash reports and analytics | Built in (Crashlytics, Analytics) | Separate tools |
| Your tools | Same Google account as Play Console and Antigravity/Gemini | Separate account |
| Cost | Pay-as-you-go "Blaze" plan with a free monthly allowance; a small launch usually stays inside it | Free tier, then $25/month |

**One rule for money safety:** set a budget alert (§2A step 3). **One rule for fairness:** the server decides dice rolls, moves and rewards — never the phone.

---

## 2. What you do (the owner) — step by step

Do these in order. None of them need you to send a secret to anyone. **Never paste passwords, service-account files or private keys into a chat.**

### 2A. Firebase project (about 20 minutes)

1. Go to <https://console.firebase.google.com> → **Create a project** → name it `Ludo NX`. Use the **same Google account** as your Play Console. Turn Google Analytics **on**.
2. **Upgrade to Blaze:** click the plan name at the bottom-left → Blaze → add a card. (Cloud Functions, which run the game rules, need it.)
3. **Budget alert:** Google Cloud console → Billing → **Budgets & alerts** → create a budget of **$10/month** with email alerts at 50%, 90%, 100%.
4. **Add an Android app** (the Android icon on the project home):
   - Package name: `com.ludonx.game`
   - App nickname: `Ludo NX Android`
   - SHA certificate fingerprints: in **Play Console → your app → Test and release → Setup → App integrity → App signing**, copy **both** the *App signing key certificate* SHA-1 and SHA-256 **and** the *Upload key certificate* SHA-1 and SHA-256, and add all four in Firebase (Project settings → your Android app → Add fingerprint).
   - Download **`google-services.json`** and save it to `C:\Dev\LudoNX\Assets\google-services.json`. (It is not a secret; it ships inside the app.)
5. **Authentication** → Get started → Sign-in method → enable **Anonymous** and **Google**.
6. **Firestore Database** → Create database → **Production mode** → location **`europe-west1` (Belgium)**. (The location can never be changed, so pick it carefully.)
7. **Realtime Database** → Create database → location **Belgium (europe-west1)** → **Locked mode**.
8. Tell Claude the **Project ID** (shown in Project settings, e.g. `ludo-nx-4f2a1`). It is not a secret.

### 2B. Your computer (about 5 minutes)

Node.js is already installed. Then:

1. Open a terminal (in Antigravity: Terminal → New Terminal) and run:
   `npm install -g firebase-tools`
2. Run `firebase login`. A browser opens — sign in with the same Google account. After this, Claude (or Gemini) can deploy the server code from your machine.

### 2C. Google Play Console (for the real shop)

1. **Payments profile:** Settings → Payments profile must be complete (bank and tax details), or Play will not let you sell anything.
2. **A build with billing:** Play only lets you create products after a build that contains RevenueCat (it adds the billing permission) is uploaded to a track. Claude makes this build (version code **5 or higher**); you upload it to **Internal testing**.
3. **Create the products** (Monetize with Play → Products → **In-app products**, and **Subscriptions** if you say yes to NX Club). Use the IDs in §4 exactly.
4. **License testing:** Settings → License testing → add your own Gmail, so your test purchases are free.
5. **Service account for RevenueCat:** follow RevenueCat's guide *"Google Play: Creating Service Account Credentials"*. You will create a service account in Google Cloud (the Firebase project is fine), invite its email in Play Console → Users and permissions with **View financial data** and **Manage orders and subscriptions**, and download a JSON key. **Upload that JSON straight into RevenueCat** — do not send it to anyone. It can take up to 36 hours before RevenueCat accepts it.

### 2D. RevenueCat (about 15 minutes)

Your Android public key (`goog_…`) is already in the game, so the project exists.

1. <https://app.revenuecat.com> → your project → the Google Play app → upload the service-account JSON from 2C.5.
2. **Products:** import the products you created in 2C.3.
3. **Entitlements:** keep `premium` (the code already uses it) and attach NX Club to it if you say yes to D7.
4. **Offerings:** create an offering called `default` with one package per product (Claude will give the exact package list when the shop is built).
5. **Webhook:** Integrations → Webhooks → Claude gives you the URL after deploying the server. Make up a long random password for the *Authorization header*. You type the same password into the terminal when Claude runs `firebase functions:secrets:set REVENUECAT_WEBHOOK_AUTH` — it never goes into a chat or into the code.

### 2E. Decide

Answer the decisions listed in `AI_HANDOFF.md` → DECISIONS NEEDED.

---

## 3. What gets built — phases

| Phase | What | Needs from you | Honest effort |
|---|---|---|---|
| **A. Accounts** | Firebase in Unity; guest sign-in on first launch; player profile (name, avatar, short player ID); coins, energy and NX Points stored on the server; security rules; delete-account flow | 2A, 2B | 1–2 days |
| **B. Real shop** | RevenueCat live; shop screen driven by RevenueCat offerings with localised prices; server grants purchases through the webhook; restore purchases; tested with your license-tester account | 2C, 2D | 1–2 days after your products exist |
| **C. Friends** | Search by name or player ID, requests, accept/decline, remove, online dot, push notification for requests | A done | 2 days |
| **D. Online matches** | Matchmaking by stake, server dice and moves, 15 s turn timer with auto-move, reconnect window, labelled bot fallback (D9), server rewards, live rankings | A done | 1–2 weeks including testing on two phones |
| **E. Challenges** | Challenge a friend to a private match; push invite | C and D done | 2–3 days |
| **F. Launch hardening** | App Check (Play Integrity), rate limits, Crashlytics, analytics events, Play Data safety form, privacy policy update, release build | All | 2 days |

**About the 30 Sep deadline:** real online matches cannot be finished safely by then. If 30 Sep is the RevenueCat Shipaton submission, the right order is **A → B first** (a real, working shop is what that contest is about), then C and D.

---

## 4. Starter product catalogue (suggested — the owner sets final prices)

Keep the first catalogue small so it is quick to create in Play Console. Local prices (Naira and others) are set by Play from the base price.

| Product ID | Type in Play | Suggested base price | Gives |
|---|---|---|---|
| `coins_1000` | In-app product | $0.99 | 1,000 coins |
| `coins_5500` | In-app product | $4.99 | 5,500 coins (10% more) |
| `energy_250` | In-app product | $1.99 | 250 energy |
| `energy_1000` | In-app product | $4.99 | 1,000 energy |
| `bundle_starter` | In-app product | $1.99 | One time only: Golden NX dice + Ceramic tokens + Naija Vibes emoji + 100 energy |
| `dice_golden_nx` | In-app product | $2.99 | Legendary dice |
| `emoji_naija_vibes` | In-app product | $0.99 | Emoji pack |
| `nx_club_monthly` | Subscription (base plan `monthly`) | $2.99 / month | `premium` entitlement: daily energy bonus, one exclusive dice per month, double mission rewards (only if D7 = yes) |

Common and rare cosmetics are bought with **coins**, handled by the server — they are not store products.

---

## 5. Architecture (for Claude and Gemini)

**Rule: phones request, the server decides.** The client never writes wallets, match state, inventories or leaderboards.

### 5.1 Identity
- First launch: Firebase **anonymous** sign-in → create `users/{uid}` via a callable function (name, avatarId, country, short numeric `playerId` for search).
- Later: link Google (and Play Games) so progress survives a new phone.
- **RevenueCat `Purchases.LogIn(uid)`** right after sign-in, so every purchase event carries the Firebase UID.

### 5.2 Data (Firestore)
| Path | Holds | Who writes |
|---|---|---|
| `users/{uid}` | Public profile: name, nameLower, avatarId, level, country, playerId, stats summary | Functions only |
| `private/{uid}` | coins, energy, energyUpdatedAt, nxPoints, inventory, equipped, premiumUntil | Functions only |
| `friends/{uid}/list/{friendUid}` | Friendship + since date | Functions only |
| `friendRequests/{toUid}/incoming/{fromUid}` | Pending requests | Functions only |
| `queue/{stakeKey}/tickets/{uid}` | Matchmaking tickets | Functions only |
| `matches/{matchId}` | Players, houses, token positions, turn, lastRoll, seq, turnDeadline, stake, status | Functions only |
| `leaderboards/{period}/entries/{uid}` | Score per week and all time | Functions only |
| `purchases/{eventId}` | RevenueCat events already processed (idempotency) | Webhook only |

Realtime Database: `status/{uid}` = online/offline with `onDisconnect` for the online dot.

### 5.3 Cloud Functions (TypeScript, 2nd gen, region `europe-west1`)
`createProfile`, `updateProfile`, `searchPlayers`, `sendFriendRequest`, `respondFriendRequest`, `removeFriend`, `joinQueue`, `leaveQueue`, `rollDice`, `moveToken`, `resign`, `turnTimeoutSweep` (scheduled), `finishMatch` (internal), `buyWithCoins`, `equipItem`, `claimMission`, `claimDaily`, `deleteAccount`, `revenueCatWebhook` (HTTP), `weeklyReset` (scheduled).

### 5.4 Online match protocol
1. `joinQueue(stake, multiplier)` checks energy, takes 1 energy, pairs two tickets with the same stake in a transaction, creates `matches/{id}`; after 15 s without a partner, a **labelled** bot takes the seat (D9).
2. Clients listen to `matches/{id}` and animate each change in `seq` order.
3. On your turn: `rollDice(matchId)` → the server rolls, saves `lastRoll`, lists legal moves; `moveToken(matchId, token)` → the server validates, applies captures and home entry, grants an extra turn on a six, bumps `seq`.
4. Turn timer 15 s; on timeout the server auto-moves. A disconnected player has 30 s to return, then a labelled bot plays their seat.
5. `finishMatch` writes placements, NX Points × multiplier, coins, stats and leaderboards in one transaction.
6. The rules engine lives in `functions/src/ludo/` with unit tests; its expected results must match the C# rules in `Assets/Scripts` (`LudoPath`, `GameManager`).

### 5.5 Purchases
`revenueCatWebhook` checks the Authorization header secret, ignores events already in `purchases/{eventId}`, then: `NON_RENEWING_PURCHASE` → grant coins, energy or the item; `INITIAL_PURCHASE` / `RENEWAL` → set `premiumUntil`; `EXPIRATION` / `CANCELLATION` → record it. The shop shows prices from RevenueCat offerings (localised), never hard-coded.

### 5.6 Security
- Firestore and Realtime Database rules: read your own private doc; read public profiles when signed in; **no client writes** to anything in §5.2.
- App Check with Play Integrity (phase F) so modified apps are refused.
- Rate limits on search and friend requests.
- Purchased coins are **never** staked in matches and nothing can be cashed out — this keeps the game clear of gambling rules. Stakes use NX Points only, as today.

---

## 6. Demo data to remove (each removed the moment its real version works)

| Demo | Where | Removed in |
|---|---|---|
| 15 fake friends and 6 fake requests | `FriendsDemoData` | C |
| Bots presented as Quick Match opponents | `BotRoster` in the Quick Match flow | D (bots stay for VS COMPUTER and as the labelled fallback) |
| Fake mission progress | `DailyMissionsData.SeedDemoProgress = true` | A |
| Local energy 1822 and local coins | Local save | A (server wallet) |
| Flat "$0.99" prices | `StoreScreen.FlatPrice` | B |
| "Seed Reference Career Stats" | Editor test menu | A (stats move to the server; the menu stays editor-only) |

Removing them earlier would leave empty screens in the live build, so each goes with its replacement, one task at a time.

---

## 7. Store and policy checklist (before the live release)

- **Account deletion:** Google Play requires an in-app way to delete an account **and** a web page that explains how. (`deleteAccount` + a simple page.)
- **Data safety form** in Play Console: update for Firebase (user IDs, profile data, crash logs, analytics) and RevenueCat (purchase history).
- **Privacy policy:** add Firebase and RevenueCat.
- **Honesty:** a mode can only say ONLINE once real online play is live; bots are always labelled BOT.
- **Version code:** 5 or higher.
