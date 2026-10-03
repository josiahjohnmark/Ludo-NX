---
name: nx-handoff
description: Saves an exact progress note into AI_HANDOFF.md so another agent can continue Ludo NX work. Use when the user says handoff, save progress, I am running out of credits, switch to Gemini, switch to Claude, or before stopping a long task.
---

# Hand off Ludo NX work

1. Open `AI_HANDOFF.md` at the project root.
2. Update **NOW**:
   - Task ID and your agent name.
   - "Last step done": what you finished and which files changed.
   - "Next step": one concrete instruction the next agent can follow without asking anything.
   - Any gotcha the next agent must know.
3. If the build or the project is currently broken because of half-finished work, write at the top of NOW in capitals: `BUILD CURRENTLY BROKEN BY: <what>`.
4. Add a LOG line: date, your agent name, summary, files changed.
5. Tell the owner in one or two lines where you stopped and what the next agent will do first.
