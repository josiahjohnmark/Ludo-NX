# LUDO NX — UI Bible: Review and Implementation Plan

**Date:** 26 Sep 2026 · **For:** Nanzing (owner) · **By:** Claude
**Read with:** `LUDO_NX_UI_BIBLE.md` (the rules) and `specimens/bible_specimen_v1.png` (the rules drawn out).

---

## 1. Verdict

Your parts are good; the whole is not yet a system. You already own the three things that make Monopoly GO look professional:

1. **A signature panel** — your notched corners with the six-pip die (`Group 68/69/78/82`) are as distinctive as Monopoly GO's corner flourishes.
2. **A strong mascot** — the ivory ceramic NX pawn is premium, ownable and rare in the Ludo genre.
3. **A real material palette** — navy felt, warm tan, cream, gold, brown wood doors.

What is missing is the discipline that makes every screen feel like the same game: one ground, one panel, one title treatment, one meaning per button colour, one export resolution. The bible supplies that. **Almost nothing needs to be thrown away** — it needs to be unified, re-exported at @3x and used the same way everywhere.

---

## 2. What the Monopoly GO reference teaches (principles, not art)

| What they do | Why it works | Our version |
|---|---|---|
| Every popup is the same cream card with four corner ornaments | Instant recognition; the frame becomes the brand | The NX Card with Lucky Six corners |
| Title always top-centre, same font | You never search for "where am I" | The nameplate on the top edge |
| Close X always at the bottom centre | Muscle memory | Close X 24 pt under every popup |
| Green = go, orange = options, grey = disabled — never mixed | Colour tells you what a button does before you read it | Six button kinds, one meaning each |
| The main button straddles the card's bottom edge in a cream collar | Looks physical and finishes the card | Same, with our collar |
| White wells group settings inside the cream card | Order without extra lines | Wells and rows (E0/E1) |
| Chunky outlined numbers; section headers flanked by the item icon ("🎲 ROLLS 🎲") | Readable at a glance, and playful | Lilita One numbers; item-flanked section labels in the shop |
| Tier frames give each gallery card an identity (wood, blue, purple, gold) | Value is visible before you read the price | Rarity rim, glow and ribbon on shop cards |
| Rank rows: trophy flags for top 3, your own row pinned in blue | You always know where you stand | Rank flags + pinned self row |
| 3D logotypes only for special moments (JOIN US!, STICKERS FOR REWARDS) | Big moments feel big because the rest is calm | Logotypes only for celebrations and events |
| All art is high resolution | Crisp = expensive-looking | @3x everywhere |

We copy these principles, not their art. Monopoly GO's assets belong to Scopely and Hasbro — see the wood floor in §4.3.

---

## 3. Diagnosis of the current build

Captured in play mode at 1080 × 2400 on 26 Sep (files in `audit/`).

| # | Problem | Where | Fix (bible section) |
|---|---|---|---|
| 1 | **Five different backgrounds**: teal `#163434` (Home), slate `#252D37` (Shop, Missions, Result), periwinkle `#6C7A9C` (Profile), navy `#162B3C` (match), `#282D38` (Friends) | All screens | One ground: `bg_menu_emboss` for menus, navy for the match (2.2, 5.1) |
| 2 | **Every screen titles itself differently**: huge black text (Friends), tan header (Missions), icon + text (Shop), nothing (Profile), text on the ground (Result) | All screens | One nameplate on the panel's top edge (5.3) |
| 3 | **Three corner styles**: isometric dice stamps (pre-match cards, Friends), notched six dice (new zip), rivets (shop) | Panels | Lucky Six notches; rivets only on small cards (3) |
| 4 | **Button colours without meaning**: PLAY AGAIN and HOME are the same tan; two greens (`#00C38A` price pill vs `#2DA830` SAVE); every product ribbon is red | Result, Shop, Settings mock | Six kinds (2.2); rarity-coloured ribbons (5.16) |
| 5 | **Low-resolution art**: top-bar coin 25 px, energy icon 17 px, nav icons 44–74 px, carousel cards 130 × 230, dice 138 px — all stretched on phones | Home, Shop | @3x exports (2.1) |
| 6 | **Text too small**: Profile stat labels about 5–7 pt, VS Computer "TAP TO EDIT" and "Costs 1 energy" about 7 pt | Profile, pre-match | 12 pt minimum; Profile shows at most 9 stats (2.3, 6.5) |
| 7 | **Baked text**: 30+ text PNGs in the new zip (SETTINGS, LEGAL, ENGLISH, HAUSA, IGBO, TAROK, CHANGE NAME…) and `play_btn_labelled.png` | New zip, pre-match | Live text — sharper, and lets you add languages later (English is the main language) (2.3) |
| 8 | **The result screen does not celebrate**: a win and a loss look the same flat list | Result | Takeover with logotype, rays, count-up (§8) |
| 9 | **Bugs**: Shop top bar has no pill backgrounds (numbers float); energy pill shows `1822/5` with the FULL label overlapping; result reward strip `+0 NX` is cut off at the right edge | Shop, Home, Result | Phase 1 |
| 10 | **Honesty rule broken**: the Quick Match popup is titled **ONLINE**, and Home's carousel still has an ONLINE card. You agreed on 16 Sep that the mode is QUICK MATCH and never claims real players (online play is not built) | Pre-match, Home | Rename to QUICK MATCH (Phase 1) |
| 11 | **Emoji art has no recorded source** | `Art/UI/Emotes`, `MatchSetup/Emoji` | Licence check before selling emoji (§7.5) |

---

## 4. Asset audit — what you gave me

Source: `C:\Dev\LudoNX\ui style bible\` (the `Untitled (7).zip` of 168 files, 19 reference screenshots, 5 wallpapers), plus what is already in the project.

### 4.1 Keep — these become the kit

| Your file(s) | Becomes | Action |
|---|---|---|
| `Creating_seamless_vertical_scrol… 1.png` (navy leather, embossed game icons) | `bg_menu_emboss` — the ground for every menu screen | Done: copied to `source/bg_menu_emboss.png`. Files `2` and `3` are duplicates — drop. |
| The 5 navy-metal wallpapers | Match ground or splash | Pick one for the match (#2 and #4 are the calmest behind a busy board); the others are spares |
| `Group 68/69/70/78` (panels), `77` (header band), `84` (bottom tab), `82` (arch), `115/91` (drawer with mascot) | NX Card family: standard, arch, drawer | I regenerate them in code at @3x (they are simple shapes); only the drawer's mascot image needs your source file |
| `Group 116` TOURNAMENTS and `Group 90` MENU plaques | The nameplate | I regenerate it blank at @3x (specimen shows it) |
| `Group 117` leaderboard rows | Rank row design | Rebuilt from parts (plate, flag, live text); drop the energy column |
| `Group 53` + `Frame 11` + `Frame 12` | Shop product card | Rebuilt in code; ribbons take rarity colours |
| `image 44–57` (blue-gold medal, gift, trophy, crown shield, gear, mail, account shield, info, document, headset, lock shield) | **Emblem icon family** | Need 256 px sources (these are 30 px exports) + the missing icons in §6 |
| `left_door.png`, `right_door.png` | Match-entry transition | Already high-res — keep as is |
| `Star 1–3` | Result rating stars | Re-export @3x |
| `3d_dice.png`, `energy_icon.png`, `profile_frame.png` | Item art / avatar frame | Use the 1024 px versions already in `Art/UI/HomeScreen/` |
| Mascot portraits (`Rectangle 114/117`, `Ellipse 11`) | Mascot | Need clean cut-outs and poses (§6) |
| In the project: `crown_gold/silver/bronze`, frames Starter → Champion, `coin_icon_gold` and `energy_icon` (1024 px), `chest.png`, 10 dice renders | Podium, avatar tiers, currency, missions, shop | Keep; the dice need 1024 px re-renders for the shop |

### 4.2 Merge or re-map

| Your file(s) | Problem | Becomes |
|---|---|---|
| `Rectangle 127–131`, `Group 72–81`, `83`, `86` (blue, lime, orange, khaki, green pills) | Colours with no fixed meaning | The six kinds, generated by me in every size and state |
| Lime DISCONNECT, khaki ACCOUNT DELETION | Not in the system | DISCONNECT → `neutral`; ACCOUNT DELETION → `danger` inside a confirm dialog |
| Emerald price pill `#00C38A` vs SAVE green `#2DA830` | Two greens | One green: `#2DA830` |
| Tan fields `#CCA87E` (zip) vs `#CFAE84` (built popups) | Two tans | `#CFAE84` |
| Tan rim (zip) vs cream rim (built popups) | Two panel builds | Cream rim (open decision D1) |
| `Rectangle 137–140`, `157–167` (tan tiles and outlined squares) | Loose shapes | Wells and badge slots |

### 4.3 Drop

- **All baked-text PNGs**: `ABOUT LUDO NX`, `ACCOUNT DELETION`, `ACCOUNT(S)`, `ARCHIEVMENTS`, `CHANGE NAME`, `CONTACTS`, `DAILY REWARDS`, `DISCONNECT(-1)`, `ENGLISH`, `HAUSA`, `IGBO`, `TAROK`, `HELP & SUPPORT`, `INBOX`, `JOSIAH`, `LANGUAGE`, `LEGAL`, `LUDO NX(-1/-2/-3)`, `MANAGE ACCOUNT(-1)`, `MINIMUM OF 3 CHARACTER`, `NEWS SUBSCRIPTION`, `PLAYER`, `PRIVACY POLICY`, `RANKING`, `SELECT YOUR AVATAR`, `SETTINGS`, `SHOWROOM`, `SUPPORT`, `TERMS OF SERVICE`, `dice 1`. Many are 5–13 px tall and cannot be used. They become live text.
- `image 58.png` (wood floor with baseboard) — a copy of Monopoly GO's Net Worth Gallery floor. Do not ship it.
- `Rectangle 199` (plain white card) and `Subtract`, `Subtract-1`, `Rectangle 118/132` (see-through panels) — not part of the system.
- `Ellipse 12–22` — eleven copies of the same avatar. Keep one.
- `NX.png`, `NX-1/2/3.png`, `Subtract-2..5` (30 px grey watermarks) — replaced by generated versions.

### 4.4 Is it enough?

- **For the chassis (panels, buttons, rows, tabs, pills, badges): yes.** I can generate all of it from the tokens. You do not need to draw more of these.
- **For the content: no.** Still missing: mascot poses, celebration logotypes, the rest of the emblem icons, and shop items (token skins, emoji, energy and coin tiers, chests, packs, rarer dice). The list, in priority order and with prompts, is in §6.

---

## 5. What I can create (in code, @3x, 9-slice ready)

The specimen was drawn this way. From the tokens I can produce:

- **NX Card panels**: standard, hero (house-colour dice), small card (rivets), drawer, tall popup, arch — any size, Lucky Six corners.
- **Nameplate** (blank, 9-slice), section labels and hairlines.
- **Buttons**: all 7 kinds × XL/L/M/S/chip × normal, pressed and disabled, collar version, round icon buttons, price buttons.
- **Wells, rows, row lips, input fields, toggles, checkboxes, radio rows, folder tabs, segmented controls.**
- **Progress bars** (track and fills in amber, green, blue, rarity and house colours).
- **Currency pills, multiplier chips and the multiplier coin.**
- **Rank flags** (gold, silver, bronze, other, self) and podium plinths.
- **Badges**: notification dot, count bubble, NEW, sale, lock, OWNED, EQUIPPED.
- **Rarity ribbons, card frames, glows, rays**, sparkles, confetti pieces in house colours, soft shadows, scrim, vignette.
- **Close X, back button, loading die spinner, skeleton rows.**
- **Glyph icons** exported white and ink @3x from the Material Symbols SVGs you already have in `icons_to_review/`.
- **Recolours**: dice colourways from one render; house-colour tints for tokens and avatar rings.
- **Unity**: update `UITheme` to these tokens, create the TMP presets, a kit builder so every screen uses the same parts, and rebuild each screen through its editor script with play-mode checks on four devices.

**What I cannot make well:** photoreal 3D renders (mascot poses, new dice materials, token skins, chests), illustrated logotypes and emoji art. Those come from you, using the prompts in §6.

---

## 6. What I need from you — in priority order

**Before 30 Sep (your deadline) — only these:**

| # | Asset | Size | Notes |
|---|---|---|---|
| P0-1 | Your decisions on §10 (D1–D9) | — | Quick answers are enough |
| P0-2 | Mascot: **victory** pose and **defeat/determined** pose | 2048 px, transparent | For the result screen |
| P0-3 | Logotypes: **VICTORY!** and **NICE TRY!** | 2048 px wide, transparent | Chunky gold letters with navy outline, like your NX MISSIONS badge |
| P0-4 | Source file of the drawer mascot hero (`Group 115/91` top image) at full resolution | Original | For the side drawer |

**After 30 Sep:**

| # | Asset | Size |
|---|---|---|
| P1-1 | Emblem icons, same style as `image 44–57`: shop, profile, friends, rankings (podium), missions (clipboard), album, levels, language (globe), sound, music, vibration, notifications (bell), share, inbox, logout, delete (bin), calendar, chest, crown, emote (smiley), tokens (pawn), dice, energy, coins — plus 256 px versions of the 11 you have | 256 × 256 each |
| P1-2 | Mascot poses: shop (holding a dice box), rankings (holding a trophy), missions (holding a clipboard), loading (rolling a die), welcome (waving), offer (pointing) | 2048 px |
| P1-3 | Logotypes: LEVEL UP!, DAILY REWARD, STARTER PACK, NX CLUB | 2048 px wide |
| P1-4 | Dice renders at 1024 px: the 10 existing colours again, plus Marble, Mahogany, Pearl, Frosted Glass, Crystal, Galaxy, Lava, Golden NX, **Cowrie** | 1024 px |
| P1-5 | Token skins (see §7.3) | 1024 px |
| P1-6 | Energy tiers (single bolt, bundle, crate, vault) and coin tiers (small stack, pile, bag, chest) | 1024 px |
| P1-7 | Chests (common, rare, epic) and emoji-pack wrappers in the four rarity colours | 1024 px |
| P2-1 | Avatar set: 12–16 portraits (mascot variants in outfits and house colours) | 512 px, square |

### Prompts (paste after the style lock in bible §8)

**Mascot poses** — attach your mascot image each time so the character stays identical:
```
Use the attached character exactly: an ivory ceramic Ludo pawn with a rounded cap,
gold trim, and "NX" embossed on the chest, determined eyebrows, glossy porcelain finish.
Pose: [jumping with both arms up in celebration, gold confetti around | arms crossed,
determined half-smile, one eyebrow raised | holding a small wooden box of dice |
holding a gold trophy above its head | holding a clipboard with a pencil |
rolling a white die off its hand | waving hello | pointing to the right].
Full body, 2048 x 2048, transparent background.
```

**Logotypes:**
```
Game title logotype that reads exactly "VICTORY!" (check spelling), chunky rounded
display letters in glossy gold with a thick navy #1E2229 outline and a soft 3D bevel,
small sparkles, slight upward arc, centred, transparent background, 2048 x 1024,
no other text, no characters.
```
(Replace the word with NICE TRY!, LEVEL UP!, DAILY REWARD, STARTER PACK, NX CLUB. For NICE TRY! use cream letters with a brown outline, no sparkles.)

**Emblem icons** — attach two of your `image 44–57` icons as the style reference:
```
Game menu icon in the exact style of the attached icons: cobalt blue and gold,
thick navy outline, soft glossy bevel, centred, fills 85% of the square.
Subject: [a podium with a gold 1st place | a clipboard with a tick | a globe with a
speech bubble | a bell | a bin | a calendar | a treasure chest | a crown | a smiley |
a pawn | a die | a lightning bolt | a gold coin | ...].
256 x 256, transparent background, no text.
```

**Dice:**
```
A single six-sided die with rounded edges, three-quarter view showing faces 1, 2 and 3
with correct pip layouts. Material: [white marble with grey veins and gold pips |
polished mahogany wood with ivory pips | pearl with iridescent sheen | frosted glass with
white pips | clear crystal with an inner blue glow | deep-space galaxy with stars |
black stone with glowing orange lava cracks | polished gold engraved with "NX" on face 1 |
ivory with small cowrie shells as pips].
1024 x 1024, transparent background.
```

**Token skins:**
```
A Ludo pawn game piece, glossy, three-quarter view. The body is [house red #E73735]
and that colour covers most of the piece. Style: [ivory ceramic with a gold rim |
carved wood | chrome | wearing a chef hat | wearing a small crown | wearing a pirate hat |
wearing a graduation cap | wearing a Yoruba fila cap | wearing a gele headwrap |
astronaut helmet | made of living flame | crystal].
1024 x 1024, transparent background.
```
Make each skin in all four house colours (or one version I can tint — ask me first).

---

## 7. The shop — selling dice, tokens and emoji

### 7.1 Layout (hub)

```
TOP BAR  [avatar] [coins +] [energy +] [≡]
┌──────────────── FEATURED (swipe) ────────────────┐
│ DAILY DEAL -40%  ·  STARTER PACK 5× value  ·  NX CLUB │   408 × 150, dots under it
└───────────────────────────────────────────────────┘
      ┌──── SHOP ────┐                              ← nameplate
 [ DICE ][ TOKENS ][ EMOJIS ][ ENERGY ]             ← folder tabs (4)
 ── 🎲 COMMON 🎲 ──
 [card] [card] [card]                               ← 112 × 158 product cards, 3 per row
 ── 🎲 RARE 🎲 ──
 [card] [card] [card]
 ...
 Restore purchases                                  ← caption link, bottom of every tab
BOTTOM NAV
```

- Each tab is split into sections by rarity, lowest first, so value climbs as you scroll.
- **Card states:** for sale (price button) · OWNED (green ✓, EQUIP button) · EQUIPPED (amber outline, no button) · LOCKED (disabled price with the unlock rule, e.g. "Reach level 10").
- **Tap a card → item detail popup:** big render on a plinth (a die slowly rotates; a token turns), rarity label, name, one line of description, what it changes ("Your dice in every match"), then BUY or EQUIP.
- **YOUR SET** wells on the pre-match cards open the matching shop tab with owned items first — that is the locker; no separate inventory screen is needed.

### 7.2 Rarity and price ladder (proposal)

Today every item is $0.99 — your instruction for this pass. When you want a ladder:

| Rarity | Paid with | Suggested price | What makes it that rarity |
|---|---|---|---|
| Common | Coins only | 1,200 coins | A colour |
| Rare | Coins or cash | 2,500–5,000 coins or $0.99 | A material |
| Epic | Cash | $1.99–$2.99 | An effect (glow, sparkle) |
| Legendary | Cash | $4.99 | Animated, with a roll trail or capture effect |
| Bundles and sets | Cash | 30% under the sum | Dice + token + emoji pack in one theme |

Players can always earn something by playing, and real money buys the special things. That keeps the game fair and still gives the shop a top end.

### 7.3 Catalogue ideas

**Dice**
- Common: Classic Ivory, Ruby, Sapphire, Emerald, Amber, Amethyst, Onyx, Blossom, Teal, Gold (your 10 renders).
- Rare: Marble, Mahogany, Pearl, Frosted Glass, Brushed Steel.
- Epic: Crystal (inner glow), Galaxy, Lava (glowing cracks), Neon Circuit.
- Legendary: **Golden NX** (engraved NX, gold sparkle trail), **Cowrie** (ivory die with cowrie-shell pips — West African heritage, nobody else has it), Dragon Scale (fire trail), Frost (ice-shard trail).

**Tokens (pawn skins)** — rule: a skin must keep the house colour on most of the piece, so the board stays readable.
- Common: Classic Gloss (default), Matte.
- Rare: **Ceramic** (the mascot's own material — ivory glaze tinted to the house colour, gold rim), Carved Wood, Chrome.
- Epic (hats, like the mascot's cap): Chef, Crown, Pirate, Graduation, **Fila**, **Gele**, Astronaut.
- Legendary (animated): Flame, Crystal, Golden King (cape and crown, sparkle burst on a capture).

**Emoji (in-match emotes)** — the 8-slot loadout on the pre-match card is already built.
- Free starter 8 (only if the current art's licence allows it — §7.5).
- **Naija Vibes** pack — animated pidgin text stickers: WAHALA!, OYA ROLL!, E DON CAST!, NO SHAKING, SHOW ME SIX!, JUST WAIT. The most shareable and ownable idea in the shop.
- **NX Mascot** pack — the ceramic pawn laughing, crying, angry, dancing. Your own art, so no licence risk.
- Trash Talk (slow turtle, yawn, target, devil) and Good Sport (GG handshake, clap, 100, four-leaf clover).
- Fair play: a 3 s cooldown between emotes, and every player can mute another.

**Energy** — four tiers with growing art (bolt → bundle → crate → vault); "Most popular" ribbon on the 2nd, "Best value" on the 3rd; a watch-ad button (`option`) for +3 energy if ads are added.

### 7.4 RevenueCat (and standing out in the Shipaton)

- RevenueCat is already wired (`RevenueCatManager`, entitlement `premium`). Every real-money price must come from the offering's package as the store's localised string — never a hard-coded "$0.99" in production.
- **Restore purchases** must be reachable from the shop and from Settings (Apple requires it for non-consumables).
- **NX Club** (proposal) — a subscription on the existing `premium` entitlement: daily energy bonus, one exclusive dice per month, double mission rewards. Its paywall (bible 6.14) must show the price, the billing period, auto-renewal terms and links to Terms and Privacy.
- **Starter Pack** — a one-time, first-purchase bundle at around 5× value, shown in the featured banner until bought.
- What makes a monetised game look professional to judges and players: a shop that looks like part of the game, a clearly designed paywall, and honest value. The bible gives you all three.

### 7.5 Emoji licence check (before you sell any emoji)

The emoji flipbooks in `Art/UI/Emotes` and `Art/UI/MatchSetup/Emoji` have no recorded source. Find where they came from:
- Google Noto emoji and Twemoji are CC BY 4.0 (free to use, credit required).
- Microsoft Fluent Emoji is MIT (free to use).
- Apple's emoji artwork is not licensed for use in other apps.

Paid packs are safest when they are your own art — the Naija Vibes and NX Mascot packs above.

---

## 8. The match flow — before, during and after

### 8.1 Pre-match cards (hero popup)

Your Online and Local Play cards already have the right bones (avatar on the top edge, loadout, emoji grid, big PLAY with the multiplier coin). Changes:

| Card | Contents (top to bottom) |
|---|---|
| **QUICK MATCH** (renamed from ONLINE) | Your avatar on the top edge · nameplate · YOUR SET (dice, token wells) · EMOJIS (4 × 2 wells, tap to edit) · STAKE chips · WIN +x / LOSE −y line · PLAY slab with multiplier coin · "1 energy · you have 18" |
| **VS COMPUTER** | Mascot on the top edge · nameplate · PLAYERS segmented 2/3/4 · YOUR HOUSE (four house discs) · OPPONENTS rows (name, difficulty chip Easy/Normal/Hard) · PLAY · energy line |
| **PASS & PLAY** | Mascot on the top edge · nameplate · PLAYERS 2/3/4 · seat rows (house disc + name field) · PLAY · "Free — no energy" |

Fixes: 12 pt minimum (the VS COMPUTER captions are about 7 pt now), section labels between groups, Lucky Six hero corners instead of the isometric dice stamps, and the close X below the card (already there — keep it).

### 8.2 Match found and entering the match

Searching (animated dots) → "Opponent found" → countdown from 3 → **doors close** (your brown NX doors) → board loads → **doors open**. The doors are used only here, so they stay special.

### 8.3 After the match — victory

| Time | What happens |
|---|---|
| 0.0 s | Ground dims; gold rays start turning; **VICTORY!** logotype drops in (scale 1.3 → 1.0 with a bounce) |
| 0.3 s | Mascot victory pose rises behind the panel's top edge |
| 0.5 s | Hero panel: place rows arrive one by one — 1st with the crown and gold flag, every avatar ringed in its house colour |
| 1.0 s | Reward strip: base NX Points count up (+120), then the **X5 multiplier coin slams onto the number** and it counts to +600 [NX]; coins +50; stats as three wells with big numbers (tokens home, captures, turns) |
| 1.5 s | Mission chips tick ("Win 2 in a row 2/2 ✓") with a CLAIM shortcut |
| 1.8 s | Buttons: **PLAY AGAIN** (`play` slab, breathing), HOME (`neutral`), SHARE (round `info`) |

Confetti in the four house colours for 1.5 s.

### 8.4 After the match — defeat (2nd–4th)

- No rays. **NICE TRY!** logotype (or a nameplate "3RD PLACE" until the logotype exists) and the mascot's determined pose.
- The loss is shown honestly (−60 [NX] in `ink.lose`), and progress sits right next to it: mission chips ("Play 5 matches 3/5") and a nudge ("A win at X5 pays +600 NX").
- PLAY AGAIN stays the main action.

---

## 9. Rankings

- **Entry:** the NX CUP badge on Home and/or the LEVELS tab — decision D5.
- **Layout (hub):** nameplate RANKINGS · folder tabs WEEKLY / ALL TIME · segmented GLOBAL / COUNTRY / FRIENDS · "Resets in 2d 14h" · **podium** for the top 3 (1st raised in the centre, crowns from your existing art) · rank rows 4–50 · **your row pinned at the bottom** in the self blue with "Climb 3 places to reach Top 10".
- **Reward bands:** thin divider rows between bands ("TOP 10 · 50 energy + rare pack", with item icons), so every rank has a reason.
- **Tap a row** → mini-profile popup (avatar, name, level, win rate, ADD FRIEND in `info`).
- **From your `Group 117` design:** keep the rank flags and the blue self row; drop the energy column (energy is fuel, not a score); put NX Points on the right with the NX tag.
- **Honesty (important):** rankings go live with real players when online play ships (see `../GO_LIVE_PLAN.md`). Until then, never show made-up "global" players as if they were real — it breaks the rule you set for Quick Match and it is a store-review risk.

---

## 10. Decisions I need from you

| # | Question | My recommendation |
|---|---|---|
| D1 | Panel rim: **cream** (built popups, the specimen) or **tan** (your new zip)? | Cream — brighter edge, the Monopoly GO clarity, and less rework |
| D2 | Corner die: six-pip in a notch, tone-on-tone brown (house colours on hero panels)? | Yes |
| D3 | Button meanings: red play · green go · blue info · amber option · tan neutral · grey disabled? | Yes |
| D4 | Profile becomes a tall popup (like Monopoly GO)? Missions: popup or hub? | Profile → popup. Missions → your brief decides |
| D5 | Where do Rankings live: the NX CUP badge, the LEVELS tab, or both? What is LEVELS? | NX CUP badge opens Rankings; LEVELS = player level track |
| D6 | Shop pricing: keep a flat $0.99 for now, or the rarity ladder in §7.2? | Ladder after the deadline |
| D7 | NX Club subscription on the `premium` entitlement? | Yes, after the deadline |
| D8 | Match ground: flat navy `#162B3C` (today) or one of the navy-metal wallpapers? | Flat navy for the deadline; try wallpaper #2 later |
| D9 | Online matchmaking finds nobody within about 15 s: fill the seat with a bot clearly labelled BOT, or keep searching? | Labelled bot after 15 s |

---

## 11. Order of work

Your deadline is 30 Sep (4 days), so the plan puts the biggest visible wins first.

| Phase | When | Who | What |
|---|---|---|---|
| **0** | Now | You | Look at the specimen, answer D1–D9, send P0 art |
| **1 — Kit and ground** | Day 1 | Me | Generate the kit @3x (panels, nameplate, buttons, wells, rows, tabs, toggles, bars, pills, badges, ribbons, rank flags, fx) · update `UITheme` and TMP presets · swap every menu screen to `bg_menu_emboss` · fix the Shop top bar, energy pill and `+0 NX` bugs · rename ONLINE → QUICK MATCH |
| **2 — Re-skin what is built** | Days 2–3 | Me | Result (victory/defeat takeover) · pre-match cards (hero chassis) · Shop (rarity cards, tabs, featured banner) · Friends and Missions (nameplate, corners) · Profile · Settings · Home top bar and nav at @3x |
| **3 — New screens** | After 30 Sep | Me + your art | Rankings · Daily reward · Level up · Starter Pack and NX Club paywall · side drawer · item detail |
| **4 — Polish** | After 30 Sep | Me | Motion pass, doors transition, sounds, four-device QA, Play build (version code 5 or higher) |

**Missions:** you will send a separate brief. The bible already covers mission rows, the bonus chest well and CLAIM ALL.

---

## 12. How to direct me (or Gemini) for any screen

Copy this, fill it in, and send it:

```
Build the <SCREEN NAME> screen following UI_BIBLE/LUDO_NX_UI_BIBLE.md.
Type: <hub | popup | takeover>
Content, top to bottom: <list>
Main action: <verb, e.g. CLAIM ALL>
Reference image (optional): <path>
New assets I added: <paths>
Anything that breaks the bible on purpose: <none | describe>
```

The agent must answer with the component list it will use and any missing asset **before** it builds, then finish with the Definition of Done checklist (bible §11).
