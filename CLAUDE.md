# CLAUDE.md

Claude Code reads this file at the start of every session in this repository.
The project's instructions live in `AGENTS.md`, shared with Codex; it is
imported below so both tools follow the same rules. Edit `AGENTS.md`, not this
file, when the rules change.

@AGENTS.md

## Claude-specific notes

- The `restore-emulator-from-evidence` skill (personal skills repository
  `BankaiNoJutsu/claude-skills`) carries the working method for this project:
  the evidence ladder, one-question passes, native reads, data and video
  corpora, the live bug loop, bounded goal prompts and handoffs. Use it for
  restoration, decompile, data-mapping and spell work here.
- Check the branch before analysing or pushing: the real line is
  `legacy-di-storefront-hardening` (and feature branches built on it), not
  `upstream/game_rework`.
- Do not commit `.claude/` or `.mcp.json`; they hold machine-local settings.
