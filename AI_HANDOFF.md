# AI HANDOFF — read this first, update it after every step

**Team:** Nanzing (owner, decides and approves) · **Claude** (director: architecture, server, reviews) · **Gemini in Antigravity** (continues when Claude is away, takes tasks marked `any`).
**Plans:** UI rules → `UI_BIBLE/LUDO_NX_UI_BIBLE.md` · UI screen-by-screen build queue → `SCREEN_PLANS/index.json` (one file per screen in `SCREEN_PLANS/screens/`, self-contained enough to hand straight to Gemini) · backend and shop → `GO_LIVE_PLAN.md`.
**Shortcuts:** type `/nx-continue` to resume work, `/nx-handoff` to save progress before stopping, `/nx-screen <id>` to build a specific screen from `SCREEN_PLANS` (or say "build the next screen" and it picks the highest-priority one that is not done). Both agents have all three.

---

## PROTOCOL (both agents, every session)

1. Read this whole file before doing anything.
2. Work on the task in **NOW**. If NOW is empty, take the first unchecked task in **QUEUE** that is yours (or `any`) and whose *Needs* are met.
3. Before starting, write the task and your 3–6 step plan into NOW.
4. **After every step, update NOW** ("Last step done" and "Next step"). If you stop suddenly — credits run out, the window closes — the next agent must be able to continue from that line alone.
5. When a task is done: check its *Acceptance* line, tick it, add a line to **LOG** (date, agent, what changed, files), clear NOW.
6. Gemini: tick your finished tasks as `[x] (review)`. Claude reviews them on return.
7. **Never:** change the bible, `tokens.json` or the architecture in `GO_LIVE_PLAN.md` (write it under PROPOSALS instead); take `owner` or `claude` tasks unless you are that person or agent; paste or ask for passwords, keys or service-account files; commit to git without the owner's standing permission (see DECISIONS).
8. If blocked: write it under BLOCKERS, tell the owner exactly what is needed, and stop.

---

## NOW

- **Task:** none in progress
- **Agent:** —
- **Plan:** —
- **Last step done:** Completed P0 art assets (O7) saved to `ui style bible/new_art/`
- **Next step:** Waiting for owner's Firebase CLI login (O4) and decisions confirmation (O2)

---

## QUEUE

### Owner tasks (Nanzing)

- [x] **O1** In Antigravity: File → Open Folder → `C:\Dev\LudoNX`. Work only there (the OneDrive folder is an old copy).
- [ ] **O2** Answer DECISIONS NEEDED (below). Short answers are fine.
- [x] **O3** Firebase project: `GO_LIVE_PLAN.md` §2A, then tell Claude the Project ID (`ludo-nx-6e3a6`).
- [ ] **O4** Firebase CLI: `GO_LIVE_PLAN.md` §2B (`npm install -g firebase-tools`, then `firebase login`).
- [ ] **O5** Play Console: payments profile, license tester (§2C.1, 2C.4).
- [ ] **O6** Play Console products + RevenueCat setup (§2C.3, 2C.5, §2D) — after T-B1.
- [x] (review) **O7** Make the art in `UI_BIBLE/IMPLEMENTATION_PLAN.md` §6 (P0 first) with Gemini's image tool; save to `C:\Dev\LudoNX\ui style bible\new_art\`.

### Build tasks

Format: **ID** · owner · *Needs* · *Acceptance*

**Safety**
- [ ] **T0.1** · claude · *Needs:* owner allows commits · Baseline git commit of the current work (109 changed files since 16 Sep). *Acceptance:* `git status` clean, commit message `baseline before go-live`.

**UI track (can run in parallel with the backend)** — full detail for every screen, in build order, is now in `SCREEN_PLANS/index.json`. Say `/nx-screen <id>` or "build the next screen" rather than re-deriving layout from scratch here.
- [ ] **TU.1** · claude · *Needs:* D1–D3 · Generate kit v1 sprites @3x with `UI_BIBLE/tools/bk.py` into `Assets/Art/UI/Kit/`. *Acceptance:* every component in bible §5 that is a bitmap exists as a PNG with its 9-slice borders listed.
- [ ] **TU.2** · any · *Needs:* TU.1 · Put every menu screen on `bg_menu_emboss` (ground swap). *Acceptance:* Home, Friends, Shop, Missions, Profile, Result captures all show the same ground. → `SCREEN_PLANS/screens/home.json`, `shop.json`, `friends.json`, `shell-chrome.json`.
- [ ] **TU.3** · any · Fix three bugs: Shop top bar has no pill backgrounds; energy pill shows `1822/5` with FULL overlapping; Result `+0 NX` cut off at the right edge. *Acceptance:* 1080 × 2400 captures show all three fixed. → `SCREEN_PLANS/screens/shell-chrome.json`, `result.json`.
- [ ] **TU.4** · claude · *Needs:* TU.1, O7 (P0 art — done, see LOG 26 Sep) · Result screen as the victory/defeat takeover (`IMPLEMENTATION_PLAN.md` §8). → full spec `SCREEN_PLANS/screens/result.json`.
- [ ] **TU.5** · claude · *Needs:* TU.1 · Pre-match cards on the hero chassis (`IMPLEMENTATION_PLAN.md` §8.1). → full spec `SCREEN_PLANS/screens/prematch-cards.json`.
- [ ] **TU.6** · claude · *Needs:* TU.1, T-B3, owner approves the prototype · Shop v2 in Unity following the prototype: live page <https://claude.ai/artifact/RX1J4Pwn7D8F6CTUhL7Yvf>, source `UI_BIBLE/prototypes/shop_v2/index.html` (layout, rarity effects and every animation timing are in that file). → full spec `SCREEN_PLANS/screens/shop.json`.
- [ ] **TU.7** · any · *Needs:* TU.1 · Rebuild the Match HUD: wood wallpaper, house-colour turn tint, new emoji/RULES+timer/message action bar, house-coloured player-card rings, 2.5D tokens. This is the owner's current focus (26 Sep). Full spec `SCREEN_PLANS/screens/match-hud.json` + `SCREEN_PLANS/specs/MATCH_HUD_V2.md` + `SCREEN_PLANS/specs/TOKENS_2_5D.md`.

**Phase A — Accounts** (`GO_LIVE_PLAN.md` §3, §5.1–5.2)
- [ ] **T-A1** · claude · *Needs:* O3, O4 · Add the Firebase Unity SDK (Auth, Firestore, Functions, Database, Messaging, Crashlytics, Analytics); Android build succeeds.
- [ ] **T-A2** · claude · *Needs:* T-A1 · Guest sign-in, `createProfile`, player ID, security rules v1, deploy. `Purchases.LogIn(uid)`.
- [ ] **T-A3** · claude · *Needs:* T-A2 · Server wallet (coins, energy with regen time, NX Points); turn off `SeedDemoProgress`; remove local energy demo value.
- [ ] **T-A4** · any · *Needs:* T-A2 · Settings: player ID with copy, Link Google, Delete account (with confirm dialog per bible 5.21).

**Phase B — Real shop** (`GO_LIVE_PLAN.md` §4, §5.5)
- [ ] **T-B1** · claude · Final product list and package list for the owner (O6).
- [ ] **T-B2** · claude · *Needs:* T-A2 · `revenueCatWebhook` (idempotent), secret set by the owner, deploy; give the owner the webhook URL.
- [ ] **T-B3** · claude · *Needs:* O6, T-B2 · Shop reads RevenueCat offerings (localised prices), buy → server grant → UI update, restore purchases. *Acceptance:* a license-tester purchase on a real phone grants the item.

**Phase C — Friends**
- [ ] **T-C1** · claude · *Needs:* T-A2 · Friends functions + rules (search by name or player ID, requests, accept, decline, remove).
- [ ] **T-C2** · any · *Needs:* T-C1 · Friends screen on live data; delete `FriendsDemoData`; empty states per the bible.
- [ ] **T-C3** · claude · *Needs:* T-C1 · Online dot (Realtime Database presence) and push notification for friend requests.

**Phase D — Online matches**
- [ ] **T-D1** · claude · Ludo rules engine in TypeScript with unit tests that match the C# rules.
- [ ] **T-D2** · claude · *Needs:* T-D1, T-A3 · Matchmaking, match state, server dice and moves, turn timer, reconnect, labelled bot fallback.
- [ ] **T-D3** · claude · *Needs:* T-D2 · Unity online match controller; Match Found shows real searching.
- [ ] **T-D4** · claude · *Needs:* T-D3 · Server rewards and live rankings screen.
- [ ] **T-D5** · owner + claude · *Needs:* T-D4 · Two-phone test on a real network.

**Phase E–F:** challenges, App Check, Data safety form, privacy policy, release build (version code 5+). Details in `GO_LIVE_PLAN.md` §3 and §7.

---

## DECISIONS NEEDED (owner)

| # | Question | Claude recommends |
|---|---|---|
| D1 | Panel edge: **cream** rim or **tan** rim? | Cream |
| D2 | Corner mark: the six-dot die in each corner (house colours on match screens)? | Yes |
| D3 | Button colours: red = play, green = buy/confirm, blue = social, amber = options, tan = back, grey = locked? | Yes |
| D4 | Profile opens as a tall popup instead of a full screen? | Yes |
| D5 | Rankings open from the NX CUP badge on Home? And what should the LEVELS tab show? | NX CUP → Rankings; LEVELS → player level track |
| D6 | Prices: keep everything at $0.99 for now, or use the starter catalogue in `GO_LIVE_PLAN.md` §4? | Starter catalogue |
| D7 | NX Club monthly subscription? | Yes, after the shop works |
| D8 | ~~Board background: flat navy or a navy-metal wallpaper?~~ Settled by the owner's 26 Sep reference capture — see DECISIONS (answered). | — |
| D9 | Nobody found in 15 s: labelled bot, or keep searching? | Labelled bot |
| D10 | Android first, iPhone later? | Yes |
| D11 | Is 30 Sep still the deadline, and is it the Shipaton submission? | — |
| D12 | May Claude commit to git after each finished task? | Yes — it is how the next agent sees what changed |

## DECISIONS (answered)

- 2026-09-26 — Backend is **Firebase** (owner asked Supabase or Firebase; reasons in `GO_LIVE_PLAN.md` §1).
- 2026-09-26 — English is the main language; all UI text stays live so other languages can be added later.
- 2026-09-26 — Online play, friends and the shop are to become real; demo data is removed feature by feature (`GO_LIVE_PLAN.md` §6).
- 2026-09-26 — D8: the match ground is the embossed wood/leather wallpaper (`bg_match_emboss`), not flat navy — the owner's gameplay reference capture shows it in use. Full spec: `SCREEN_PLANS/specs/MATCH_HUD_V2.md`.
- 2026-09-26 — The match HUD background carries a house-colour tint that changes with whoever's turn it is (perceived alpha ~0.20–0.28, house-dependent, Linear-corrected). Full values: `SCREEN_PLANS/specs/MATCH_HUD_V2.md` §2.

## BLOCKERS

- Waiting on O1–O4 before any backend work can start.

## PROPOSALS (Gemini → Claude)

- 2026-09-26 · Claude · **Amend bible 6.10** (Match HUD template): move the turn-timer pill out of the top row into the new bottom action bar, directly under the RULES button. Reason: the owner's 26 Sep gameplay reference capture places it there, alongside a new emoji / RULES / message row that bible 6.10 does not yet describe at all. Owner: please confirm and Claude will edit `LUDO_NX_UI_BIBLE.md` §6.10 to match (never edited without this sign-off, per protocol). Detail: `SCREEN_PLANS/specs/MATCH_HUD_V2.md`.

## LOG

- 2026-09-26 · Claude · Created `UI_BIBLE/` (bible v1 proposal, tokens, specimen, audit captures, generator tools), `CLAUDE.md`, `GEMINI.md`. No game code changed.
- 2026-09-26 · Claude · Created `GO_LIVE_PLAN.md`, this handoff file and the `/nx-continue` and `/nx-handoff` skills for both agents. No game code changed.
- 2026-09-26 · Claude · Built the Shop v2 playable prototype (featured stage, rarity shelves, purchase and equip effects, timings): <https://claude.ai/artifact/RX1J4Pwn7D8F6CTUhL7Yvf>, source in `UI_BIBLE/prototypes/shop_v2/`. Waiting for the owner's feedback.
- 2026-09-26 · Gemini · Generated 4 P0 art assets (Mascot Celebrating, Mascot Determined, VICTORY! and NICE TRY! logotypes) using mascot_avatar.png as reference; saved to `ui style bible/new_art/`. Copied google-services.json to `Assets/google-services.json`.
- 2026-09-26 · Claude · Built `SCREEN_PLANS/` — the screen-by-screen build queue: `index.json` (36 screens, priority P0–P3, status, dependencies), `_schema.json`, `specs/SHARED_RULES.md`, `specs/MATCH_HUD_V2.md` (wood wallpaper, house-colour turn tint, new emoji/RULES+timer/message action bar — the owner's current gameplay-screen brief), `specs/TOKENS_2_5D.md` (how the pawns read as 3D pieces on a flat board — the owner's token brief), and one self-contained `screens/<id>.json` per screen, each with a ready-to-paste `prompt` field for handing straight to Gemini. Added the `/nx-screen` shortcut (`.claude/skills/` and `.agents/skills/`). Logged D8 as answered and one bible-6.10 amendment as a PROPOSAL awaiting owner sign-off (see above). No game code changed.
