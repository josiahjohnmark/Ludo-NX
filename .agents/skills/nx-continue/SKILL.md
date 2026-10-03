---
name: nx-continue
description: Resumes Ludo NX work exactly where the last agent (Claude or Gemini) stopped, using AI_HANDOFF.md. Use at the start of any Ludo NX work session, or when the user says continue, carry on, resume, or pick up where the other agent stopped.
---

# Continue Ludo NX work

1. Read `AI_HANDOFF.md` at the project root, top to bottom. Follow its PROTOCOL.
2. If **NOW** has a task in progress:
   - Check that "Last step done" really happened (open the files it names, run `git status` / `git diff`).
   - Then do "Next step".
3. If NOW is empty: take the first unchecked task in QUEUE whose owner is you (Gemini may take tasks marked `any`; only Claude takes `claude`; only the owner does `owner`) and whose *Needs* are met.
4. Before starting, write the task, your agent name and a 3–6 step plan into NOW.
5. Follow `UI_BIBLE/LUDO_NX_UI_BIBLE.md` for UI work and `GO_LIVE_PLAN.md` for backend and shop work. Use only values from `UI_BIBLE/tokens.json`.
6. After **every** step, update NOW ("Last step done" and "Next step").
7. When the task is done: check its Acceptance line, tick it (Gemini writes `[x] (review)`), append a LOG line (date, agent, what changed, files), clear NOW.
8. If blocked: add it to BLOCKERS, tell the owner exactly what is needed, and stop.

Never change the bible, tokens or architecture (write a PROPOSAL instead), never ask for or paste secrets, and never commit to git unless DECISIONS says the owner allowed it.
