# SPEC — 2.5D tokens (pawns) on a flat board

**Status:** owner brief, 26 Sep 2026 · **Owner words:** "my tokens are not 2D tokens — when viewed from above they should be seen in a 2.5D illusion, like it's 3D but on a 2D board."
**Applies to:** the match board, pre-match YOUR SET wells, the shop TOKENS tab, the album, the result screen.
**Reads with:** bible 2.6 (light and depth), 2.8 (imagery), 8 (image-tool style lock), `IMPLEMENTATION_PLAN.md` §7.3 (token catalogue).

---

## 1. The rule in one line

**The board is drawn flat from directly above. The pawns are drawn from a high three-quarter angle and stand up out of the board.** Nothing else creates the illusion — not an outline, not a bevel on the tile.

This is the convention every premium Ludo uses. The eye accepts the mismatch because the *shadow* re-anchors the piece to the flat plane.

---

## 2. The five cues, in order of how much they matter

| # | Cue | Why it matters | Spec |
|---|---|---|---|
| 1 | **Contact shadow on the board** | The single strongest height cue. Without it a pawn floats no matter how well it is rendered. | A separate flat ellipse sprite, always parallel to the board, never rotated with the pawn. Already implemented — `TokenMovement.useGroundShadow`. |
| 2 | **Viewing angle** | Tells you it is an object, not a sticker | Pawn rendered at **65° from vertical** (you see the top cap plus the full body silhouette). Same angle for every token skin, forever. |
| 3 | **One light direction** | Two light directions read as two different games | Warm key from **top-left**, cool rim from the **right**, per bible 2.6 and §8. |
| 4 | **Base anchoring** | The piece must sit *in* the tile, not *on top of* the artwork | Sprite pivot at the **bottom-centre of the contact ellipse**, not the sprite centre. |
| 5 | **Draw order by depth** | A pawn lower on the screen must overlap one above it | Sort by board row — see §5. |

---

## 3. Geometry in the 440 pt design frame

Board is 408 pt wide (screen width − 2 × 16 margin), 15 × 15 cells → **cell = 27.2 pt**.

| Measure | Value | Note |
|---|---|---|
| Contact ellipse width | **21 pt** (0.78 × cell) | Leaves a 3 pt breathing gap inside the tile |
| Contact ellipse height | **8 pt** | width × 0.38 — the foreshortening that sells 65° |
| Pawn total height | **34 pt** (1.25 × cell) | The cap breaks above the tile into the row behind |
| Pawn body max width | **20 pt** | Never wider than the contact ellipse |
| Head/cap diameter | **13 pt** | Classic Ludo silhouette: ball on a flared body |
| Shadow offset from base | **x +0.5, y −1.5 pt** | Opposite the top-left key light |

Stacked tokens on one tile use `TokenStackLayout`: scale each to **0.82**, fan them **±5 pt on x** and **−2 pt on y** per extra token, cap the visible fan at 3 and show a `×4` count bubble (bible 5.15) when there are four.

---

## 4. The token art itself

One render per skin, four house recolours (or one neutral render Unity tints — ask the owner which, per `IMPLEMENTATION_PLAN.md` §6 P1-5).

**Non-negotiable:** the house colour must cover **at least 70 %** of the piece. A skin that hides which house a pawn belongs to is rejected, however pretty it is — the board must stay readable at a glance.

**Image prompt** (paste after the bible §8 style lock):

```
A single Ludo pawn game piece, standing upright, viewed from a high three-quarter angle
65 degrees from vertical so the top of the cap and the full body are both visible.
Classic silhouette: a round ball head on a flared conical body with a wide circular base.
Body colour [house red #E73735 | house green #4DAE51 | house blue #1B89E7 | house yellow #FBC02B]
covering at least 70% of the piece. Material: glossy ceramic with a thin gold rim at the base.
A single bright specular hotspot on the upper-left of the ball, a soft cool rim light down
the right edge, and a dark colour reflection in the lower body. Warm key light from top-left.
No shadow baked into the image — the shadow is a separate asset.
1024 x 1024, centred, transparent background, no text.
```

Export the contact shadow **separately** as `fx_token_shadow@3x.png`: a soft black ellipse, blur 6 pt, 45 % alpha at the centre falling to 0 at the edge, on transparent.

> The owner's reference dome (glossy amber, concentric rings, hot centre) is the right **material** reference: hot specular core, saturated mid-band, dark rim, then the shadow outside the piece. Use it for the ball's shading, not for the silhouette.

---

## 5. Unity implementation

| Thing | Setting |
|---|---|
| Pawn renderer | `SpriteRenderer`, sorting layer `Tokens`, **`sortingOrder = Mathf.RoundToInt((boardTopY − worldY) * 100)`** so lower pawns draw in front |
| Shadow renderer | Child of the pawn, sorting layer `BoardFX` (always under every pawn), **never inherits the pawn's rotation or squash** |
| Pivot | Bottom-centre. Set it in the sprite importer, not with a transform offset |
| Filter / compression | Bilinear, no mipmaps, High Quality — a token is on screen for the whole match |
| Import size | 1024 px source, max size 512 in the atlas, one `Tokens` sprite atlas |

**Hop (already written — keep it, tune only these numbers):** as the pawn rises, the shadow scales `1.00 → 0.62` and fades `0.45 → 0.18` alpha. That contrast between a rising body and a shrinking, fading shadow *is* the 3D read. Per-tile hop ~0.13 s; the whole move is capped so a six never takes longer than ~0.8 s.

**Selection hint:** breathe scale 1.00 ↔ 1.06 over 1.2 s **plus** a soft house-colour rim glow. Never a hard outline — an outline flattens the piece and undoes everything above.

**Capture:** the captured pawn squashes to 0.7 y / 1.2 x over 90 ms, then pops up and arcs home over 420 ms. Its shadow stays flat on the board the whole time.

---

## 6. Definition of done for tokens

- [ ] A still capture with the board at 1080 px wide reads as pieces standing on a table, not stickers.
- [ ] Every pawn's highlight is on the **upper-left**. No exceptions, no mirrored art.
- [ ] Every pawn has a shadow, and the shadow never rotates, tilts or leaves the board plane.
- [ ] A pawn on row 14 visibly overlaps one on row 13.
- [ ] The house colour is identifiable in a 96 px-wide thumbnail of the board.
- [ ] Four in a home yard fan and stay individually countable.
- [ ] The same angle and light are used in the shop card, the YOUR SET well and the board.
