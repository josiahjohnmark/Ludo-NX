# Ludo NX — instructions for AI agents

**Start of every session:** read `AI_HANDOFF.md` and follow its PROTOCOL. It says what is in progress, what to do next and who does what. Claude is the director (architecture, server code, reviews of Gemini's `(review)` tasks); Gemini in Antigravity continues when Claude is away. Shortcuts: `/nx-continue` and `/nx-handoff`.

For UI work (screens, popups, buttons, icons, colours, fonts, layout, image prompts):

1. Follow `UI_BIBLE/LUDO_NX_UI_BIBLE.md` strictly.
2. Use only the values in `UI_BIBLE/tokens.json`. Never invent a colour, font, size or shadow.
3. Before building, list the components you will use and any missing assets. After building, report the Definition of Done checklist (bible section 11).

For backend, online play, friends and the shop: follow `GO_LIVE_PLAN.md` (Firebase + RevenueCat). The server decides dice, moves and rewards — never the phone.

Project facts:

- Unity 6 (6000.5). Screens are prefabs under `Assets/Resources/UI/Screens` and `Assets/Resources/UI/Modals`, built by editor scripts in `Assets/Scripts/Editor/`. Never hand-place UI in the scene.
- The project renders in Linear colour space: semi-transparent layers from Figma come out lighter (bible 2.6).
- Until real online play ships, never label a mode "online" and never claim a real player was found. Bots are always labelled BOT.
- Never paste or ask for passwords, keystore passwords, service-account files or private keys.
- The kit generator lives in `UI_BIBLE/tools/` (`bk.py`, `specimen.py`); rerun it to regenerate the specimen.
- The Unity Editor exposes an MCP bridge at `http://127.0.0.1:8080/mcp`; screen test menus are under `Ludo NX/UI/Screen Test/`.
