# LUDO NX — UI BIBLE

**Version:** 1.0 (proposal — becomes law once the owner approves `specimens/bible_specimen_v1.png`)
**Date:** 26 Sep 2026
**Art direction:** Nanzing (owner). **Maintained by:** Claude.
**Files in this folder:**

| File | What it is |
|---|---|
| `LUDO_NX_UI_BIBLE.md` | This file. The rules. |
| `tokens.json` | Every colour, size, radius, shadow and timing as data. The only allowed values. |
| `IMPLEMENTATION_PLAN.md` | Review of the current build, asset audit, what to make, and the order of work. |
| `specimens/bible_specimen_v1.png` | The rules drawn out: panels, buttons, shop cards, rank rows, pills. |
| `audit/` | Captures of the game as it looked on 26 Sep 2026, before the bible. |
| `source/` | Canonical source art (starts with `bg_menu_emboss.png`). |
| `tools/` | `bk.py` + `specimen.py`: the code that draws the kit. Rerun to regenerate. |

---

## 0. How to use this bible

**People:** read sections 1–4 once. Use sections 5 and 6 every time you design a screen. Run section 11 before calling a screen done.

**AI agents (Claude, Gemini, any other) — follow these steps every time:**

1. Read this whole file before designing, generating or building any UI.
2. Use only values from `tokens.json`. Never invent a colour, font, size, radius or shadow.
3. Build only from the components in section 5. If you need something that is not there, **stop and propose it** (anatomy, tokens, states) instead of improvising.
4. Follow the screen type rules in section 4 and the template in section 6.
5. Run the Definition of Done in section 11 and report every unchecked item.

**Precedence:** this bible → the owner's reference image for that screen → older documents.
This bible replaces the visual rules (colours, fonts, icons, panels, buttons) in `LUDO_NX_UI_UX_SPECIFICATION.md` v2.0, `LUDO_NX_ASSET_PRODUCTION_GUIDE.md` v2.0 and the current `UITheme.asset` values. Game content, economy and flows in those documents stay valid unless this file says otherwise.
If a reference image disagrees with this bible, **ask the owner which one wins.** Never pick silently.

---

## 1. The look

> **A luxury game table at night.** Deep navy felt under warm card-stock panels, ivory and gold pieces, and the four Ludo house colours used as signals, never as decoration.

**Five principles**

1. **One world.** Every menu screen sits on the same navy ground. Every popup is the same NX Card. A player should never wonder which game they are in.
2. **Every colour means one thing.** Red = play. Green = get or confirm. Blue = social or info. Amber = options. Tan = back or neutral. Grey = locked.
3. **Soft, real light.** Light comes from above. Top edges are lit; bottom edges have a lip and a soft shadow. Nothing is flat, nothing is neon.
4. **Clarity first.** Big numbers, short words, one main action per screen, nothing a player must read below 12 pt.
5. **Earned celebration.** Wins, rewards and level-ups get the big moments (rays, confetti, count-ups). Everything else stays calm so those moments land.

---

## 2. Foundations

### 2.1 Frame, units and export

- **Design frame: 440 × 956 pt** (iPhone 16 Pro Max). Every size in this bible is in points in this frame.
- **Export every bitmap @3x** (the frame is 1320 × 2868 px). Never export @1x — today's 17–25 px top-bar icons are why the game looks soft next to Monopoly GO.
- **Unity:** the canvas is 1080 wide, so **1 pt = 2.4545 units** (1080 ÷ 440). The main menu was built on a 402-pt frame (factor 1080 ÷ 402) and keeps that factor until it is rebuilt.
- **Safe area:** top content starts at the device's safe inset, never less than 45 pt. Bottom nav sits on the bottom inset.
- **Scale uniformly. Never stretch on one axis.** Plates widen only through their 9-slice middle. Bar items keep their size and spread with the width; profile pinned left, menu button pinned right.
- **Screen side margin:** 16 pt. Hub panels are 408 pt wide; popups are 380 pt wide.

### 2.2 Colour

All values are in `tokens.json → color`. Summary:

**Ground (behind everything)**

| Token | Hex | Use |
|---|---|---|
| `ground.base` | `#1E2229` | Letterbox and fallback behind the wallpaper |
| `ground.deep` | `#14171C` | Vignette edges |
| `ground.match` | `#162B3C` | Behind the board during a match |
| `ground.scrim` | `#0B0D12` at 72% | Behind every popup |

Ground art: **`bg_menu_emboss`** (the navy wallpaper with embossed game icons) behind every menu screen. `ground.match` (or one approved navy-metal wallpaper) behind the board.

**Surface (panels, rows, wells)**

| Token | Hex | Use |
|---|---|---|
| `cream50` | `#FBF4E6` | Lit or selected row, highlights |
| `cream100` | `#F3E4C9` | Panel rim, rows, text on the ground |
| `tan200` | `#CFAE84` | Panel field |
| `tan300` | `#C09666` | Wells, bottom-nav plate, row lips, inactive tabs |
| `tan400` | `#A27E57` | Hairlines, dividers, field outline |
| `brown600` | `#7A5E40` | Corner dice, line icons on tan |
| `brown700` | `#5A4227` | Nameplate body |
| `brown800` | `#3E2C19` | Deep bevels, plaque shadow |

**Ink (text)**

| Token | Hex | Use |
|---|---|---|
| `ink.default` | `#2A1E14` | All text on cream or tan. **Never pure black.** |
| `ink.muted` | `#6B5234` | Secondary text on cream or tan |
| `ink.onDark` | `#F3E4C9` | Text on the navy ground |
| `ink.onDarkMuted` | `#B9B2A4` | Secondary text on the ground |
| `ink.plaque` | `#F0D3A6` | Nameplate titles |
| `ink.win` / `ink.lose` | `#1F7A22` / `#B7271F` | Gains and losses |

**Action — every button kind, one meaning each**

| Kind | Face | Lip | Highlight | Meaning |
|---|---|---|---|---|
| `play` | `#FF3930` | `#B7271F` | `#FF7A6E` | Start a match. **At most one per screen.** |
| `go` | `#2DA830` | `#1B6E1E` | `#6ED36F` | Buy, claim, confirm, save, submit, OK, accept |
| `info` | `#0051D5` | `#00307F` | `#4D8CF0` | Share, invite, message, connect, info |
| `option` | `#FCAB1E` | `#B8720A` | `#FFD07A` | Open an option or sub-page, watch an ad, the selected chip |
| `neutral` | `#C09666` | `#8A6A45` | `#DDBB8E` | Back, cancel, later, home, unselected chips (label `#FFF3E1`) |
| `disabled` | `#BDB3A3` | `#8F8676` | `#D9D1C4` | Locked or unavailable |
| `danger` | `#C62828` | `#7F1616` | `#E35D5D` | Delete, remove, leave — **only inside a confirm dialog** |

**House colours — player identity, never buttons**

| Token | Hex | Board position |
|---|---|---|
| `house.red` | `#E73735` | Top-left |
| `house.green` | `#4DAE51` | Top-right |
| `house.blue` | `#1B89E7` | Bottom-left |
| `house.yellow` | `#FBC02B` | Bottom-right |

Use for avatar rings in a match, seats, tokens, hero-panel corners, result rows and confetti.

**Rarity** — `common #9A7148`, `rare #1B89E7`, `epic #8E44D6`, `legendary #F2B51F`.
**Rank** — `gold #E3A632`, `silver #A9B1BB`, `bronze #D0743C`, others `#C09666`, self row blue gradient `#8DB8E6 → #5B82AD`.
**Notification dot** — `#FF3B30`.

**Currencies — each has one look and is never confused with another**

| Currency | What it is | Always shown as |
|---|---|---|
| Coins | Spendable soft currency | Gold coin icon **then** number: `[coin] 1,200` |
| Energy | Match fuel | Blue bolt icon then balance: `[bolt] 18/20` |
| NX Points | Score. Never spendable. | Number **then** crimson NX tag (`#9C0C33`, letters `#FFE3A3`): `850 [NX]` |

Never write "NX" after a coin amount, and never show NX Points with the coin icon.

### 2.3 Typography

| Family | File | Used for |
|---|---|---|
| **Lilita One** | `Assets/Fonts/LilitaOne-Regular.ttf` | Titles, buttons, numbers, names, labels |
| **Nunito Bold** | `Assets/Fonts/Nunito-Bold.ttf` | Descriptions, help text, meta lines, sentences |

Not for UI: Plus Jakarta Sans, Baloo 2, Titan One, anything in `Fonts/Candidates`. Keep the files; do not use them.

**Type scale (pt, 440 frame)**

| Role | Font | Size | Case | Colour |
|---|---|---|---|---|
| Hero number | Lilita One | 44 | — | White, ink outline |
| Nameplate title | Lilita One | 22 | CAPS | `ink.plaque` |
| Panel heading | Lilita One | 20 | CAPS | `ink.default` |
| Section label | Lilita One | 14 | CAPS | `ink.muted`, hairlines both sides |
| Row title | Lilita One | 17 | Title Case | `ink.default` |
| Number (rows, pills) | Lilita One | 18–19 | — | Ink on light, white on dark |
| Button XL (PLAY) | Lilita One | 34 | CAPS | White |
| Button L / M / S | Lilita One | 21 / 18 / 15 | CAPS | White |
| Body | Nunito Bold | 14 | Sentence | `ink.muted` |
| Caption | Nunito Bold | 12 | Sentence | `ink.muted` |
| Tag (NEW, -40%) | Lilita One | 12 | CAPS | White |

- **Minimum 12 pt** for anything a player must read. 11 pt only for tags of six characters or fewer.
- **Never bake text into images.** The only exceptions are logotypes (2.8). English is the main language; keeping all text live means any other language can be added later without redrawing art.
- **Unity TextMeshPro presets** — every text uses one of these, never a one-off material:
  `TMP_Lilita_Ink`, `TMP_Lilita_Plaque` (underlay 0, −1.6 pt, `#2A1D10`), `TMP_Lilita_Number_White` (outline 0.2 ink, underlay 0, −1.5 pt), `TMP_Lilita_Button_<kind>` (outline 0.18 and underlay 0, −1.4 pt in that kind's lip colour), `TMP_Nunito_Body`.

**Writing rules**

- Numbers use separators: `12,480`. In pills and rows, above 99,999 shorten: `125K`, `1.2M`. Detail views show the full number.
- Timers use two units at most: `9h 24m`, `24m 10s`, `10s`.
- Caps for titles, buttons, labels and names in rank rows. Sentence case for descriptions.
- Tone: short, warm, confident. "So close — one more round?" not "You lost." Buttons are verbs: PLAY, CLAIM, BUY, SHARE, SAVE.

### 2.4 Space and size

- Base unit **4 pt**. Allowed steps: 2, 4, 8, 12, 16, 20, 24, 32, 40, 48.
- Panel rim **10** (cards under 200 pt wide: **6**). Field padding **16**. Corner clear zone **28** (see section 3).
- List rows **64–74** tall; rank rows **50–56**; gap between rows **8–10**.
- Tap targets at least **44 × 44**.
- Button heights: XL 66 (PLAY) · L 52 · M 46 · S 34 · chip 40.
- Avatars: top bar 52 · rows 36 · popup hero 84.

### 2.5 Shape

| Thing | Radius |
|---|---|
| Panel outer / field / corner notch | 22 / 12 / 24 |
| Rows | 14 |
| Shop card outer / field | 16 / 11 |
| Wells | 12 |
| Buttons | Full pill (height ÷ 2). **PLAY only** is a slab with radius 16. |
| Chips | Full pill |
| Avatars | Always circles |

### 2.6 Light, depth and effects ("soft realistic")

Light comes from above. Four depth levels:

| Level | Used by | Recipe |
|---|---|---|
| **E0 recessed** | Wells, progress tracks, inputs | Fill `tan300` + inner shadow (0, +2.5, blur 2.5, `#3C230A` 45%) |
| **E1 resting** | Rows, chips, small cards | 2 pt lip in `tan300` + drop shadow (0, 2, blur 2, `#3C2814` 27%) |
| **E2 raised** | Panels, shop cards | Drop shadow (0, 10, blur 16, black 50%) + lit top edge (1.4 pt white 65%) |
| **E3 floating** | Popups | E2 + scrim behind |

**Buttons:** a lip under the face (S 3–4, M/L 5–6, XL 7 pt) in the lip colour; face gradient from highlight (top) to face (50%); gloss band over the top 46% (white 37% → 5%); lit top edge (white 47%); label with outline and 1.4 pt drop in the lip colour.
**Pressed:** the face drops by (lip − 1), the lip becomes 1 pt, the face darkens 11%, scale 0.97, 80 ms.
**Panel field:** inner shadow under the rim (0, +3.5, blur 3.5, `#462D14` 37%) and a 0.9 pt `tan400` hairline, so the field sits below the rim.
**Texture:** optional paper grain on tan fields at 4–6%. Never on buttons.

**Linear colour space (Unity):** this project renders in Linear, so any semi-transparent layer made in Figma comes out lighter. Pre-blend shadows and glows that sit on a known colour into opaque sprites. For shadows over changing backgrounds, correct the alpha: `a' = 1 − (1 − a)^2.2`. Check by sampling flat areas of a play-mode capture against the reference.

### 2.7 Icons — three families, never mixed in one row

| Family | Look | Where | Source size |
|---|---|---|---|
| **Emblem** | Blue and gold badge, navy outline (your `image 44–57` set) | Menu rows, feature entry points, empty states, tab icons | 256 × 256 px, shown at 28–44 pt |
| **Glyph** | Flat, one colour, rounded (Material Symbols Rounded, filled, weight 700) | Inside buttons and small controls: close, back, check, edit, share, lock, plus | Exported @3x from the SVGs |
| **Item** | Glossy 3D render (coin, bolt, dice, tokens, chests, packs, emoji) | Currencies and anything you own or buy | 1024 px |

Rules: one family per row type. Emblems never go inside buttons. Glyphs are never used as feature icons. Items always come with a number or a price. Glyphs are white on buttons and `ink.default` on cream or tan.

### 2.8 Imagery

- **The mascot "NX"** is the ivory ceramic pawn with gold trim and a cap. It appears on the drawer, pre-match cards, results, empty states, loading and offers — at most once per screen. Lighting: warm key light from top-left, cool rim light from the right, soft studio shadow. Background removed. Never stretched. Poses are listed in `IMPLEMENTATION_PLAN.md`.
- **Logotypes** (images that contain words) are allowed only for celebrations and events: `VICTORY!`, `NICE TRY!`, `LEVEL UP!`, `DAILY REWARD`, `NX CUP`, `NX MISSIONS`, `STARTER PACK`, `NX CLUB`, seasonal events. Style: chunky gold letters, navy outline, 3D bevel — like the NX MISSIONS and NX CUP badges on Home. Every other title is live text on a nameplate.
- **3D renders** (dice, tokens, chests, coins, energy): the mascot's lighting, three-quarter view, soft contact shadow baked below, transparent background, 1024 px.
- **Do not reuse other games' art.** Monopoly GO is a reference for principles only.

### 2.9 Motion

| Moment | Spec |
|---|---|
| Popup opens | Scrim fades in 180 ms; card scales 0.86 → 1.04 → 1.00 in 260 ms (ease-out-back) |
| Popup closes | Scale 1.00 → 0.92 and fade, 160 ms (ease-in) |
| Button press | 80 ms down, 140 ms release with 4% overshoot |
| List rows appear | 30 ms stagger per row, slide up 12 pt, fade 180 ms |
| Reward count-up | 0.6–1.2 s ease-out, a tick sound per step |
| PLAY idle | Breathes 1.00 ↔ 1.03 every 1.6 s. The only idle animation on a screen. |
| Entering a match | Doors close 350 ms (ease-in) → load → doors open 450 ms (ease-out). Match entry only. |
| Victory | Rays rotate 12°/s; confetti in the four house colours for 1.5 s |

Never make two things compete for attention at the same moment.

### 2.10 Sound slots (audio comes after the screens)

Name every UI sound so it can be dropped in later: `ui_tap`, `ui_tab`, `ui_open`, `ui_close`, `ui_error`, `reward_tick`, `reward_big`, `match_start`, `doors_close`, `doors_open`.

---

## 3. The signature: the Lucky Six corners

In Ludo, a six is the roll that sets a token free. **Every NX Card carries four sixes.**

- Every panel has four **notched corners**: the field is cut by a quarter circle (radius 24) at each corner, and the rim colour fills the notch.
- In each notch sits a **six-pip die**, 17 pt, tilted 16° and mirrored per side so the dice face the centre.
- **Standard panels:** die body `brown600`, pips `cream100`.
- **Hero panels (match flow only — pre-match, match found, result):** die bodies in the house colours, placed like the board: top-left red, top-right green, bottom-left blue, bottom-right yellow. White pips, darker bevel.
- **Small cards (under 200 pt wide, e.g. shop cards):** four brass rivets instead of dice.
- **Never** put corner dice on buttons, rows, pills, chips or toasts. **Never** use another corner ornament (no isometric dice stamps, no flourishes).
- Keep a **28 pt clear zone** around each corner die. No content goes there.

---

## 4. Layout system

### 4.1 Three screen types — nothing else

| Type | What | Examples |
|---|---|---|
| **Hub** | Bottom-nav destination: ground + top bar + one screen panel + bottom nav | Home, Friends, Shop, Rankings, Album |
| **Popup** | Opened on top of a hub: scrim + NX Card + close X below. The hub stays visible, dimmed. | Settings, Language, Legal, Profile*, Missions*, item detail, pre-match cards, offers, confirm |
| **Takeover** | Full screen with its own ground, no bottom nav | Splash, Match found, Match HUD, Result, Level up, Daily reward, pack opening |

\* Proposed change — the owner decides. See `IMPLEMENTATION_PLAN.md`.

### 4.2 Hub skeleton

```
┌────────────────────── 440 ──────────────────────┐
│  safe inset (at least 45)                        │
│  TOP BAR  [avatar] [coins +] [energy +]    [≡]   │  56 tall, never scrolls
│                                                  │
│            ┌──── NAMEPLATE ────┐                 │  straddles the panel's top edge
│  ┌─────────┴───────────────────┴──────────┐      │
│  │ ◆  tabs / segmented control         ◆ │      │  panel 408 wide, 16 margins
│  │    content — only this part scrolls    │      │  12 pt fade under the field edge
│  │ ◆                                   ◆ │      │
│  └────────────────────────────────────────┘      │
│  BOTTOM NAV  album · friends · HOME · shop · lvl │  116 tall, tan300 plate
└──────────────────────────────────────────────────┘
```

### 4.3 Popup skeleton

```
                 scrim 72%
            ┌─── NAMEPLATE ───┐
   ┌────────┴─────────────────┴────────┐
   │ ◆                               ◆ │   380 wide, Lucky Six corners
   │   subtitle or timer (body 14)     │
   │   ────────── SECTION ──────────   │
   │   content: rows, wells, grids     │
   │ ◆                               ◆ │
   └──────────┐ [ MAIN ACTION ] ┌──────┘   button straddles the bottom edge,
              └─────────────────┘          sits in a 6 pt cream collar
                    ( X )                  close X, 24 pt below, always
```

- Maximum height = screen height − top inset − 120. If the content is taller, the content scrolls, not the card.
- A popup that belongs to a person (profile, pre-match) puts their **avatar straddling the top edge** instead of the nameplate, with the nameplate just inside.
- Only one popup is visible at a time; a second one replaces or stacks with its own scrim.

### 4.4 Takeover skeleton

Top third: logotype or big title. Middle: hero panel (house-colour corners). Then the reward strip. Bottom: buttons, main action first. See 6.10 and 6.11.

---

## 5. Components — the only building blocks

Each component lists anatomy, sizes and states. Colours come from `tokens.json`.

**5.1 Ground.** `bg_menu_emboss`, cover-scaled, cropped top and bottom, never stretched. Letterbox in `ground.base`. Optional vignette to `ground.deep` at the edges.

**5.2 Top bar.** Avatar (52, frame tier ring) pinned left · coins pill · energy pill (with timer when not full) · menu button (≡) pinned right. Pills spread with the screen width and keep their size. The bar stays on every hub. Tapping a pill's green + opens the matching shop tab.

**5.3 Nameplate (title plaque).** `brown700` body with top-to-bottom gradient `#735638 → #5A4227 → #44311C`, 3 pt rim gradient `#E9C99A → #B88E5C`, inner lit edge, 38–40 tall, 9-slice (width = text + 76). A diamond ◆ (`#E3C394`, 8 pt) 20 pt in from each end. Title: Lilita One 22, `ink.plaque`, 1.6 pt dark drop. **One per panel. It straddles the panel's top edge** (its centre sits on the rim).

**5.4 NX Card (panel).** Cream rim 10 (gradient `#FDF5E4 → #EBD9B8`, lit top edge), `tan200` field with inner shadow and `tan400` hairline, Lucky Six corners, E2 shadow.
Variants: **standard** · **hero** (house-colour dice) · **drawer** (side menu: full height, flush to one side, mascot hero image at the top) · **arch** (rounded top for the avatar picker) · **small card** (rim 6, rivets).
Inside the field: content keeps 16 pt from the field edge and stays out of the corner zones.

**5.5 Section label.** Lilita One 14 CAPS `ink.muted`, with a 1.4 pt `tan400` hairline on each side (10 pt gap). In the shop, the hairlines are replaced by the category's item icon on each side (e.g. a die on both sides of DICE).

**5.6 Well (E0).** Recessed slot, `tan300`, radius 12, inner shadow. Used for item slots, emoji loadouts, stat tiles, icon backplates (circle), grouped settings.

**5.7 Row (E1).** Cream plate (`#FBF1DE → cream100`), radius 14, 2 pt `tan300` lip, 64–74 tall. Layout left → right: icon well or avatar (16 in) · title (row title) over meta (caption) · trailing control (button S, toggle, chevron or number).
Variants: friend row (avatar + online dot + name + ⋮), request row (accept `go` + decline `neutral` round buttons), mission row (icon well, title, progress, reward, CLAIM), setting row (emblem icon, label, toggle).
Selected row: `cream50` plus a 2 pt `option` outline. Your own row in any list: the self blue gradient.

**5.8 Buttons.** Kinds from 2.2; sizes XL/L/M/S/chip from 2.4; pill shape (PLAY = slab).
States: normal · pressed · disabled (switch to the `disabled` kind — never just lower the alpha) · loading (spinner replaces the label, width stays the same).
**Collar:** a button that straddles a panel edge sits in a 6 pt cream collar.
**Icon + label:** glyph or item icon on the left, 6 pt gap.
**Price button:** `go` kind; coin prices show `[coin] 1,200`; real-money prices show the store's localised string from RevenueCat — never hard-coded dollars in production.
**Round icon button** (44): the same kinds, a glyph only (accept ✓, decline ✕, back ‹).

**5.9 Close and back.** Close: a 44 cream disc, `tan300` ring, `brown700` ✕, 24 pt under the popup, centred. Back (for pushed hubs only): round `neutral` button with ‹, top-left under the safe inset. A popup never has a back button and a hub never has a close X.

**5.10 Tabs and segmented control.** **Folder tabs** sit on the panel's top edge under the nameplate: selected tab is `option` amber, taller by 6 pt and joined to the field; others are `neutral` and slightly dimmed. Maximum 4 tabs. Labels live (never baked).
**Segmented control:** a `tan300` recessed track holding 2–3 segments; the selected segment is a `cream50` pill with `ink.default` text.

**5.11 Toggle, checkbox, radio.** Toggle 56 × 30: on = `go` track with a ✓ and the knob right; off = `tan300` track with ✕ and the knob left. Checkbox 26: cream with a `tan400` border; checked shows a `go` ✓. Radio lists (language): each option is a row; the selected row is `cream50` with a ✓.

**5.12 Progress bar.** Recessed `tan300` track, full-pill; fill gradient `#FFD07A → #FCAB1E → #D9870C` with a gloss line; value label (`3/5`) in Lilita One to the right. Rarity or house colours may replace the fill when the bar belongs to them.

**5.13 Currency pill.** Dark pill (`#0E1117` at 84%) with inner shadow and a faint rim; the item icon (40) overlaps the left end; number in Lilita One 19 white; a small `go` + button on the right end.

**5.14 Avatar.** Always a circle. Ring by context: frame tier outside a match (Starter → Champion frames), house colour inside a match, cream on popups. Online dot (green, 12, cream ring) at the top-left. Level or tier badge at the bottom.

**5.15 Badges.** Notification dot (`#FF3B30`, 10, cream ring) · count bubble (red, Lilita 12, white) · NEW tag (`info` blue) · sale tag (`#E7302A`, e.g. `-40%`) · lock (glyph on a `disabled` disc) · OWNED (green ✓ disc) · EQUIPPED (`option` outline on the card).

**5.16 Ribbon.** The name band across a product card: rarity colour with diagonal darker stripes (72% shade, 47% opacity), overhanging the card field by 4 pt each side, name in Lilita One 15 white with outline and drop in the stripe shade.

**5.17 Shop product card.** 112 × 158 (three per row inside a hub panel: 3 × 112 + 2 gaps of 10 = 356, the panel's content width; the specimen draws them larger only for legibility), small-card chassis (rim 6, rivets), the item render centred in the top 60%, rarity glow behind it (rays for legendary), ribbon at 61%, price button straddling the bottom edge. Rarity sets the rim line, glow and ribbon; legendary also gets a gold rim. Tags sit on the top-left corner. Tap = item detail popup.

**5.18 Rank row and podium.** Row fills the content width (356 in a hub panel: 408 − 2 × 10 rim − 2 × 16 padding) × 50–56: rank flag (46 wide, left) · avatar 36 · name (Lilita 17, CAPS) · score + NX tag (right). Flags: gold, silver, bronze for 1–3 with white numbers; `tan300` with ink numbers for the rest. Your row is pinned to the bottom of the list in the self blue, with white text, and says how far you are from the next reward band. **Podium** (top of the rankings panel): 1st in the centre and raised, 2nd left, 3rd right, each avatar on a plinth with its crown (`crown_gold/silver/bronze`).

**5.19 Multiplier chips and coin.** Chips (56 × 40, pill): unselected `neutral`, selected `option`, locked `disabled` with a lock and the unlock hint underneath ("Reach 800 max energy"). The multiplier coin (44, blue `info` gradient, cream ring, `X5`) sits on the PLAY button's top-right corner.

**5.20 Toast.** Dark pill (`#0E1117` at 90%), cream text (Nunito 14), optional item icon on the left, slides down under the top bar for 2.2 s. Success, info and error toasts only differ by their icon.

**5.21 Confirm dialog.** Small standard NX Card (380 wide): nameplate with the question ("REMOVE FRIEND?"), one line of body text, two buttons side by side: the safe choice on the left in `neutral`, the action on the right (`go`, or `danger` when destructive). No close X (the neutral button is the way out).

**5.22 Empty state.** Faded emblem icon (40% opacity) or the mascot, a title in Lilita 17, one line in Nunito 14 telling the player what to do, and optionally one button.

**5.23 Loading.** Spinner: a tumbling six-pip die. Lists show 3–5 skeleton rows (cream plates with `tan300` bars that shimmer). Never show a blank panel.

**5.24 Side drawer (menu ≡).** Drawer NX Card from the right edge, full height: mascot hero at the top, player name, then rows of emblem icon + label (SHOP, PROFILE, FRIENDS, MISSIONS, RANKINGS, INBOX, SETTINGS, SUPPORT, LEGAL). Scrim on the rest of the screen; close X at the bottom centre.

**5.25 Input field.** Recessed well (E0), 44–52 tall, radius 12, Nunito 16 `ink.default`, placeholder in `ink.muted` 70%, optional leading glyph (search) and trailing clear ✕. Error: a `danger` hairline and one caption line underneath.

---

## 6. Screen templates

| # | Screen | Type | Template notes |
|---|---|---|---|
| 6.1 | Home | Hub | Top bar, NX MISSIONS and NX CUP badges left/right, LUDO NX logo, mode carousel (ONLINE / QUICK PLAY / LOCAL PLAY), bottom nav. Swap the ground to `bg_menu_emboss`. |
| 6.2 | Friends | Hub | Screen panel with nameplate FRIENDS, folder tabs LIST / REQUESTS, search field above the list, friend rows. (Decisions of 14 Sep stay: two tabs, dots menu, Challenge = coming soon.) |
| 6.3 | Shop | Hub | See `IMPLEMENTATION_PLAN.md` §7. Featured banner carousel (daily deal, starter pack, NX Club) above folder tabs DICE / TOKENS / EMOJIS / ENERGY, 3-column product grid, section labels with item icons, restore purchases link at the bottom. |
| 6.4 | Rankings | Hub | Nameplate RANKINGS, folder tabs WEEKLY / ALL TIME, segmented GLOBAL / COUNTRY / FRIENDS, podium, rank rows, pinned self row, reward bands. |
| 6.5 | Profile | Popup (tall) | Avatar straddling the top, name, ID with copy glyph, joined date, SHARE (`info`). Tabs STATS / BADGES. Stats: 3 × 3 wells, big number + caption. Badges: grid of wells, locked ones faded with a lock. |
| 6.6 | Settings | Popup | Toggles grouped in a cream well (sound, music, vibration, emotes), `option` buttons for LANGUAGE, NOTIFICATIONS, SUPPORT, LEGAL, then player ID and version (caption), MANAGE ACCOUNT (`neutral`). |
| 6.7 | Missions | Popup or hub | Owner brief to come. Uses mission rows, BONUS chest well, CLAIM ALL straddling the bottom. |
| 6.8 | Pre-match cards | Popup (hero) | QUICK MATCH / VS COMPUTER / PASS & PLAY. Avatar or mascot straddling the top, nameplate, YOUR SET wells (dice, token), emoji loadout (4 × 2 wells), STAKE chips, WIN/LOSE line, PLAY slab with multiplier coin, energy line, close X. Pass & Play shows no stake and no energy. |
| 6.9 | Match found | Takeover | Diagonal split in both sides' house colours, avatars with house rings, names above, "VS" centre, countdown from 3 in a cream pill. No claim that a real person was found. |
| 6.10 | Match HUD | Takeover | `ground.match`, board centred. Top: pause (round `neutral`), stake pill (`+20 NX × X5`), timer pill. Four player plates at the corners (avatar with house ring, name, 4 home pips), active plate lit with its house colour and a soft glow. Bottom: turn banner, dice, emoji button. |
| 6.11 | Result | Takeover | See `IMPLEMENTATION_PLAN.md` §8: logotype, hero panel with place rows, reward count-up with multiplier, mission progress chips, PLAY AGAIN (`play`), HOME (`neutral`), SHARE (round `info`). |
| 6.12 | Daily reward | Takeover | Logotype DAILY REWARD, 7 day cards (small cards; today lit and bouncing; claimed ones ticked; day 7 a chest), CLAIM (`go`). |
| 6.13 | Level up | Takeover | Rays, logotype LEVEL UP!, new level number (hero number), unlocks listed as item icons, CONTINUE (`go`). |
| 6.14 | Offer / paywall | Popup | Logotype header (STARTER PACK, NX CLUB), contents as item icons with numbers, value line ("5× value"), price button (`go`, localised), restore purchases link (caption, underlined), terms line. |
| 6.15 | Side drawer | Drawer | See 5.24. |

---

## 7. Assets

### 7.1 Naming

`<type>_<name>_<variant>_<state>@3x.png` — lowercase, underscores, no spaces.
Types: `bg`, `panel`, `plate`, `btn`, `icon` (emblem), `glyph`, `item`, `badge`, `ribbon`, `frame`, `avatar`, `mascot`, `logo`, `fx`.
Examples: `panel_nxcard_standard@3x.png`, `btn_pill_go_pressed@3x.png`, `icon_settings@3x.png`, `glyph_close@3x.png`, `item_dice_sapphire@3x.png`, `mascot_victory@3x.png`, `logo_victory@3x.png`, `fx_rays@3x.png`.

### 7.2 Folders (Unity)

| Folder | Holds |
|---|---|
| `Assets/Art/UI/Kit/` | Panels, plates, buttons, badges, ribbons, wells, bars, close X, fx |
| `Assets/Art/UI/Icons/Emblem/` | Emblem icons |
| `Assets/Art/UI/Icons/Glyph/` | Glyph icons |
| `Assets/Art/UI/Items/{Currency,Dice,Tokens,Emoji,Chests,Packs}/` | Item art |
| `Assets/Art/UI/Mascot/`, `Assets/Art/UI/Logos/`, `Assets/Art/UI/Backgrounds/` | Mascot poses, logotypes, grounds |
| `Assets/Art/UI/<Screen>/` | Only art that is truly unique to one screen |

### 7.3 Export checklist

- @3x PNG, sRGB, transparent background.
- Trimmed, with even padding where a shadow needs room (say how much).
- **No text** except logotypes.
- 9-slice plates: plain middle, borders listed in the asset registry (e.g. `L24 R24 T20 B28`).
- Semi-transparent layers pre-blended where they sit on a known colour (Linear colour space, 2.6).

### 7.4 Unity import

Sprite (2D and UI), Single, Mesh Type Full Rect for 9-slice, no mipmaps, compression High Quality or None for UI, one Sprite Atlas per family (Kit, Icons, Items).

---

## 8. Style lock for AI image tools

Start every image prompt with this block, then add the asset-specific part from `IMPLEMENTATION_PLAN.md` §6.

```
Mobile game UI asset for "Ludo NX", a premium board game. Soft realistic 3D render,
glossy ceramic and polished materials, gentle bevels, warm key light from the top-left,
cool rim light from the right, soft contact shadow directly below, no harsh contrast.
Palette: ivory #F3E4C9, warm tan #CFAE84, brown #5A4227, gold #F2B51F, navy #1E2229,
house colours red #E73735, green #4DAE51, yellow #FBC02B, blue #1B89E7.
Three-quarter view, centred, isolated on a plain transparent (or pure white) background,
no text, no watermark, no frame, 1024 x 1024.
```

**Never:** text in the image (unless it is a logotype), neon glow, photographic backgrounds, flat vector style, other games' characters or logos, extra fingers or broken dice pips (always check that a die shows correct pip layouts: opposite faces add up to 7).

---

## 9. Unity build rules

- Screens are prefabs under `Resources/UI/Screens` (hubs and takeovers) and `Resources/UI/Modals` (popups), built by editor scripts. **Never hand-place UI in the scene** — ScreenManager's canvas draws over it and it never reaches a build.
- Colours come from `UITheme` (to be updated to `tokens.json`); components never hard-code colours.
- Text uses the TMP presets in 2.3.
- Layout follows 2.1: uniform scale, 9-slice plates, bar items spread with width, safe-area baseline 45.
- Verify every screen in **play mode** on 402 × 874, 440 × 956, a 20:9 Android and an iPad before calling it done.

---

## 10. Do and don't

| Do | Don't |
|---|---|
| One nameplate per panel, on the top edge | Big black titles floating on the ground |
| Put the main action on the panel's bottom edge, in a collar | Stack three equal buttons |
| Use the kind that matches the meaning | Pick a button colour because it looks nice |
| Show rarity with rim, glow and ribbon | Make every product ribbon red |
| Keep numbers big and short (`125K`) | Squeeze 30 tiny stats into one screen |
| Use `bg_menu_emboss` everywhere in menus | Give each screen its own background colour |
| Export @3x | Upscale 1x art |
| Live text for everything but logotypes | Bake SETTINGS, LEGAL, ENGLISH into PNGs |
| Celebrate wins with rays, confetti and a count-up | Show a win and a loss with the same flat list |

---

## 11. Definition of Done — check every screen

- [ ] The ground is `bg_menu_emboss` (menus) or the match ground (match flow).
- [ ] Exactly one nameplate title; exactly one main action; at most one red `play` button.
- [ ] Every colour, size, radius and shadow comes from `tokens.json`. No pure black text.
- [ ] Nothing a player must read is below 12 pt. No baked text. Numbers and timers follow 2.3.
- [ ] Corner dice appear only on NX Cards, and the 28 pt corner zones are clear.
- [ ] Every sprite is @3x and looks crisp in a device-sized capture.
- [ ] Buttons have a lip and a pressed state. Tap targets are at least 44 × 44.
- [ ] Popups have the scrim and a close X below; hubs have the top bar and bottom nav.
- [ ] Empty, loading and error states exist.
- [ ] Checked in play mode on the four devices in section 9; nothing is stretched on one axis.
- [ ] Linear-alpha check done: flat areas in the capture match the reference.
- [ ] The owner has seen a capture and said yes.
