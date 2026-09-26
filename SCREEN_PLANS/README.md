# SCREEN_PLANS — the screen-by-screen build store

This folder is the **queue of screens** for Ludo NX. One file per screen, machine-readable, so any agent can pick a screen and build it without asking the owner what it should look like.

Written by Claude (director). Built by whoever is available — usually Gemini in Antigravity.

---

## For the owner: how to use this

Say one of these to any agent:

| You want | Say |
|---|---|
| Build a specific screen | `Build the screen in SCREEN_PLANS/screens/match-hud.json` |
| Let the agent choose the next one | `Read SCREEN_PLANS/index.json and build the highest-priority screen that is not done and whose dependencies are met` |
| See what is left | `Read SCREEN_PLANS/index.json and show me what is not done, grouped by priority` |
| Change a screen's design | Tell Claude. Claude edits the plan file; the builder agent reads it next time. |

There is also a shortcut: **`/nx-screen <id>`** (for example `/nx-screen match-hud`), available to both Claude and Gemini.

You do not need to re-explain the bible, the tokens or the layout. It is all in the file.

---

## For agents: how to use this

1. Read `specs/SHARED_RULES.md` — once per session, applies to every screen.
2. Read `index.json` — the registry. Pick a screen, or use the one you were told.
3. Read `screens/<id>.json` — that screen's whole brief.
4. Read any file listed in its `specs` array.
5. Do the **preflight** in `SHARED_RULES.md` section 2. Post it. Wait if something is missing.
6. Build it, following the `steps` array in order.
7. Do the **postflight** in `SHARED_RULES.md` section 7, including updating `index.json` and `AI_HANDOFF.md`.

---

## Layout of this folder

```
SCREEN_PLANS/
  README.md            <- you are here
  index.json           <- the registry: every screen, its status, priority and dependencies
  _schema.json         <- what the fields in a screen file mean
  specs/
    SHARED_RULES.md    <- applies to every screen. Read first.
    MATCH_HUD_V2.md    <- the gameplay screen: wallpaper, house tint, action bar
    TOKENS_2_5D.md     <- how the pawns read as 3D on a flat board
  screens/
    <id>.json          <- one per screen
```

---

## Status values

| Status | Meaning |
|---|---|
| `missing` | The screen does not exist. Build it from nothing. |
| `rebuild` | Something exists but it does not follow the bible. Rebuild it from the plan. |
| `polish` | It follows the bible roughly; close the gaps listed in `gaps`. |
| `done` | Passes bible section 11 and the owner has seen a capture and said yes. |

Only the **owner** moves a screen to `done`. An agent moves it to `review` and says so.

---

## Priority

| Tier | What is in it |
|---|---|
| **P0** | The match itself — what the owner is working on now |
| **P1** | The screens a player sees every session: home, shop, result, pre-match, profile |
| **P2** | Supporting popups and the second tier of hubs |
| **P3** | Celebration, progression and system screens — after the core is right |

Inside a tier, build in `index.json` order.

---

## Rule of the folder

**A plan file is the design decision. If you disagree with one, say so in one line and build it as written** — unless the owner changes it. That is the whole point of the folder: the design argument happens once, in the file, not again in every session.
