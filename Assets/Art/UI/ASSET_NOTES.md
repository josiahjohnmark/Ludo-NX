# Ludo NX — imported asset notes

Only reviewed keepers live under `Assets/`. The full bulk download stays outside the project in
`ludo nx, game asstes and ui and sounds/` so nothing unused is compiled into a build.

| Folder | Source pack | Why it is here |
|---|---|---|
| `Art/UI/Icons` | Google Material Symbols | The working icon set. Bold, uniform, already wired to `UIIconSet`. |
| `Art/UI/Icons_Board` | Kenney board-game icons | Fills the gaps Material has no glyph for: dice faces, pawn, crown, hourglass, **padlock**. Monochrome, so it tints to any theme colour. |
| `Art/UI/Flags` | flag-pack (Vector) | Country flag for the player card. Filenames are ISO 3166 codes, so the picker is a straight lookup. |
| `Art/UI/Medals` | Kenney medals | Candidate only — see the review. Flat 2D, not the 2.5D direction. |
| `Fonts/Candidates` | various | Shortlist that survived the specimen review. Not yet adopted. |
| `Audio/Music` | user-supplied | Menu bed, match bed, victory fanfare (still needs trimming). |
| `Audio/SFX` | casino-audio, ui-pack, clicks pack | One pick per role. |

## Not imported, deliberately

- **ranks-pack** — military insignia (chevrons, army stripes). Wrong metaphor for a family board game.
- **Free Icon Pack v3.1**, **FREE** — contain third-party trademarks (Twitter, Discord, Roblox, Apple, Steam) that cannot ship.
- **Ravenmore packs** — hand-painted fantasy weapons and potions. Nothing a Ludo game needs.
- **kenney_ui-pack-pixel-adventure** — pixel art, clashes with everything else here.
- **Extra Clean UI**, **ui-pack** surfaces — the procedural clay shader already produces better surfaces: any colour, any radius, resolution-independent, one draw call.
- **music-loop-bundle-\*** — 650 MB of source loops. Individual tracks get copied in as they are chosen; the bundles never belong in the project.
