---
name: nx-screen
description: Build or pick up a Ludo NX screen from SCREEN_PLANS. Use when the user says "build the X screen", "/nx-screen X", or "build the next screen" for Ludo NX.
---

# Build a Ludo NX screen from the plan store

1. Read `SCREEN_PLANS/specs/SHARED_RULES.md` once for this session.
2. Read `SCREEN_PLANS/index.json`.
   - If the user named a screen (an id, or a close match to a title), use it.
   - If not, pick the highest-priority screen where `status` is not `done` and every id in `depends_on` is `done`.
3. Read `SCREEN_PLANS/screens/<id>.json` in full, and every file listed in its `specs` array.
4. Do the **preflight** in `SHARED_RULES.md` section 2: post the component list, the tokens you will read, and any missing asset. Stop and wait if a P0 asset the plan calls a hard blocker is missing.
5. Build following the screen file's `steps` array, in order, using only values from `UI_BIBLE/tokens.json`.
6. Do the **postflight** in `SHARED_RULES.md` section 7: the Definition of Done (bible section 11), the screen's own `acceptance` list, captures, and update `SCREEN_PLANS/index.json`'s `status` for that screen (to `review`, never `done` — only the owner sets `done`) plus `AI_HANDOFF.md`.

If the plan file disagrees with the bible and carries no entry in its `deviations` array, stop and ask the owner — do not pick silently.
