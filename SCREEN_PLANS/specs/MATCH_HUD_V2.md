# SPEC — Match HUD v2 (the gameplay screen)

**Status:** owner brief + owner reference capture, 26 Sep 2026.
**Reference:** the owner's iPhone 16/17 gameplay capture. It is the layout that wins where this spec does not say otherwise.
**Reads with:** bible 6.10, 5.2, 5.13, 5.14, 4.4, 2.6, 2.9 · `SCREEN_PLANS/specs/TOKENS_2_5D.md`.

---

## 0. What changes from what is built today

1. The match ground becomes the **embossed wood/leather wallpaper**, not flat navy.
2. A **house-colour tint** sits over that wallpaper and **changes to whoever's turn it is**.
3. The bottom bar becomes **emoji · RULES (timer under it) · message**.
4. Player-card avatar rings become **house colours** (they are all blue today — that is a bug against bible 5.14).
5. The empty plaque at the top-centre gets the **stake pill**.

---

## 1. Layer stack — bottom to top

| # | Layer | Spec |
|---|---|---|
| 0 | Letterbox | `ground.base` `#1E2229` flat. Only ever visible outside the wallpaper's crop. |
| 1 | **Match wallpaper** | `bg_match_emboss` — the embossed wood/leather panel from the owner's capture. **Cover-scaled, cropped top and bottom, never stretched on one axis** (bible 2.1). Use `BackgroundCoverFitter`. |
| 2 | **House tint** (new) | Full-bleed `Image`, flat fill in the **active player's house colour**. Alphas in section 2. Cross-fades on every turn change. |
| 3 | Vignette | Radial fade to `ground.deep` `#14171C`, 0 at the centre to 38 percent at the corners. Keeps the board the brightest thing on screen. |
| 4 | Board | Centred, 408 pt wide, its own dark navy frame. **The tint never covers the board** — the four house colours on the board must stay pure. |
| 5 | Tokens + token shadows | Per `TOKENS_2_5D.md`. |
| 6 | HUD chrome | Player cards, top row, dice tray, ROLL, action bar. |
| 7 | Overlays | Emoji tray, message tray, rules popup, pause popup, toasts. |

> **Why the tint sits at layer 2 and not over everything:** the owner asked for the *background* to carry the turn colour. Tinting the board or the cards as well would desaturate the house colours the game is actually played with, and would make yellow's turn unreadable.

---

## 2. House tint values — the owner's core ask

The owner asked for "about 25 to 30 percent, decreased professionally so the background is still clear and the colour is seen clearly". Colours do not read equally at the same alpha over warm wood, so each house gets its own value.

| House | Hex | **Perceived alpha** (design intent) | Raw Unity alpha (Linear-corrected) | Why |
|---|---|---|---|---|
| Red | `#E73735` | **0.26** | approx 0.48 | The baseline the owner named |
| Green | `#4DAE51` | **0.24** | approx 0.45 | Green over warm brown goes muddy fast |
| Blue | `#1B89E7` | **0.28** | approx 0.51 | Darkest against the wood, so it can take more |
| Yellow | `#FBC02B` | **0.20** | approx 0.39 | Closest in hue to the wood — 0.26 turns the screen into soup |

**Linear colour space:** this project renders in Linear, so a flat semi-transparent layer reads lighter than the value you typed (bible 2.6). Correction: `raw = 1 - (1 - perceived)^2.2`. Set the raw value, then **verify by capture**: screenshot at 1080 x 2400, sample the wallpaper away from the board, and confirm the wood grain is still legible and the hue unmistakable. Tune inside plus or minus 0.04 and write the final numbers back into this table.

**Turn change:** cross-fade old colour to new colour over **320 ms, ease-in-out**. Never cut. Do not fade through neutral in between.

**Nobody's turn** (match start, match over, disconnect): fade the tint to alpha 0 over 320 ms and leave the plain wallpaper.

**Local / pass-and-play:** the tint follows the seat whose turn it is, exactly the same.

The active player's card also gets its house glow (bible 6.10) — the tint and the glow together are how a player knows whose turn it is without reading anything.

---

## 3. Layout, top to bottom (440 x 956 frame)

```
+---------------------- 440 ----------------------+
| safe inset (min 45)                             |
| [pause 44]      ( stake plaque )      [snd 44]  |  top row, y = inset + 8
|                                                 |
|  +-- card P1 --+              +-- card P2 --+   |  top seats, house rings
|                                                 |
|         +------- BOARD 408 -------+             |  centred both ways
|         |                         |             |
|         +-------------------------+             |
|                                                 |
|  +-- card P3 --+ [dice tray] +-- card P4 --+    |  bottom seats
|                                                 |
|               +--  R O L L  --+                 |  XL slab 66, play red
|               |  TAP TO ROLL  |                 |
|               +---------------+                 |
|    [emoji 56]     [ RULES ]     [msg 56]        |  action bar (new)
|                   ( 00:37 )                     |
+-------------------------------------------------+
```

### 3.1 Top row

| Slot | Spec |
|---|---|
| Left | Round **44** button, `neutral`, the owner's door glyph. Opens the **Pause popup** (Resume, Sound, Rules, Leave). |
| Centre | **Stake plaque** — the recessed plate already in the capture, currently empty. Put the stake pill in it (bible 5.13): `+20 [NX] x X5`. Lilita 18. When there is no stake (Pass and Play) hide the plaque entirely rather than showing `+0`. |
| Right | Round **44** button, `neutral`, speaker glyph. **Quick toggle only** — mutes and unmutes on tap, no popup, icon swaps to the muted glyph. |

### 3.2 Player cards (4)

Keep the owner's card design. Fix these against the bible:

- **Avatar ring = that player's house colour** (`house.red/green/blue/yellow`), not blue for everyone (bible 5.14). This is the current build's worst bug — four blue rings on a four-colour board.
- Name: Lilita One 17 CAPS, `ink.default`, with the country flag 16 pt to its left.
- `LEVEL 6` plaque under the name: small `brown700` plate, Lilita 12 CAPS, `ink.plaque`.
- Two stat cells, `NX POINTS` and `WIN PERCENT`: section-label caps at **12 pt minimum** (they are under that in the capture — fix), value in Lilita 18.
- The die badge at the avatar's bottom-left stays; it doubles as the **4 home pips** slot from bible 6.10 — show four pips, filling as tokens get home.
- **Active card:** lit to `cream50`, a soft house-colour outer glow, scale 1.03. **Inactive cards:** 82 percent brightness, no glow. Only ever one active.
- **Bot seats must carry a `BOT` tag** on the card (project rule — never imply a real player).

### 3.3 Dice tray and ROLL

- Dice tray: recessed well (E0) between the two bottom cards, holding the 1 to 2 dice at 44 pt each.
- **ROLL**: the one `play` red on the screen, XL slab, height 66, radius 16, lip 7. Label Lilita 34 CAPS, sub-label `TAP TO ROLL` in Nunito 12.
- States: **enabled** (breathing 1.00 to 1.03, the only idle on the screen) · **disabled** (`disabled` kind, sub-label becomes whose turn it is) · **rolling** (spinner die replaces the label, width unchanged).

### 3.4 Action bar — the owner's second core ask

A **match action bar**, not a hub bottom nav — no tabs, no HOME. It sits on the ground, 92 pt tall, on the bottom safe inset.

| Slot | Control | Kind | Why that kind |
|---|---|---|---|
| Left | **Emoji** — 56 rounded-square clay button, smiley item icon | `neutral` | Frequent, low-stakes. Keeps ROLL as the only loud thing on screen. |
| Centre | **RULES** — button M, height 46, pill, label `RULES` Lilita 18 CAPS, book glyph on the left | `option` | Bible 2.2: amber means "open an option or sub-page". Rules is exactly that. |
| Centre, under it | **Turn timer** — recessed pill (E0), 34 tall, `tan300` track, Lilita 18 `ink.default`, format `M:SS`. 8 pt gap under the RULES button. | — | See the deviation note below |
| Right | **Message** — 56 rounded-square clay button, speech-bubble item icon | `neutral` | Pairs visually with emoji; the symmetry is the point |

**Timer behaviour:** counts down the turn. Under **10 s** the digits go `ink.lose` `#B7271F` and the pill pulses 1.00 to 1.04 once per second. At 0 the turn auto-passes. On your turn the timer is full brightness; on someone else's it drops to 70 percent.

**Badges:** emoji and message each take a notification dot (bible 5.15) when something unseen arrived. Opening the tray clears the dot.

> **APPROVED DEVIATION — bible 6.10.** Bible 6.10 places the timer pill in the top row. The owner has moved it under the RULES button at the bottom. The owner's instruction wins. **Action for Claude:** amend bible 6.10 and log it, so the next agent does not "fix" this back.

---

## 4. Motion

| Moment | Spec |
|---|---|
| Turn passes | Tint cross-fade 320 ms · old card dims and new card lights over 220 ms · timer resets with a 120 ms scale pop |
| Roll | Button press 80/140 ms (bible 2.9) · dice tumble 600 ms · result settles with a 90 ms squash |
| Token hop | Per `TOKENS_2_5D.md` section 5 |
| Capture | Screen shake 6 px over 180 ms · vibration (captures only — project rule) · the captured pawn arcs home |
| Emoji fired | Bubble rises from the sender's card, holds 1.4 s, fades. **3 s cooldown**, and a muted player's emotes never appear |
| Entering | Doors open 450 ms (bible 2.9) — the doors belong to match entry only |

---

## 5. Do not

- Do not tint the board, the cards or the ROLL button with the house colour — the background only.
- Do not put a second red button anywhere on this screen.
- Do not show a bottom nav. This is a takeover.
- Do not let the tint drop below 0.16 or above 0.34 perceived — under that the turn is invisible, over it the wallpaper dies.
- Do not use free-text chat (see `screens/match-message-tray.json` for the reason and the approved alternative).
- Do not label anything "online" or claim a real opponent while online play is unbuilt.

---

## 6. Definition of done

Bible section 11 in full, plus:

- [ ] One capture per house with that house on turn — four captures; the colour is unmistakable in each and the wood grain still visible in all four.
- [ ] The four avatar rings are four different house colours in one capture.
- [ ] Every label on the player cards is 12 pt or larger.
- [ ] Timer turns red and pulses under 10 s; auto-pass fires at 0.
- [ ] Emoji and message trays open, close, and never cover the board's centre cross.
- [ ] ROLL is the only red button, and the only idling element.
- [ ] Checked in play mode at 402 x 874, 440 x 956, a 20:9 Android and an iPad. The board stays square on every one.
