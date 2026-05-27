# LaughingWS City And Checklist Smoke Gate Review

Date: 2026-05-27

## Decision

LWS-036 and LWS-037 are closed as mapped-only smoke blockers. The current WIP
city/checklist/entity rows stay labeled WIP/GUESSED until targeted client smoke
proves each row's behavior.

## LWS-036 Targeted Quest/Client Smoke

Blocked. No complete smoke bundle exists in this pass for Illium Ringo Hax,
Thayd/Illium housing intro story panels, Auroria, Crimson Isle, Levian Bay,
Everstar Grove, Wilderrun, or Northern Wilds checklist/objective rows.

Required evidence per row family:

- character/account state and active quest/objective id;
- world id, coordinates, creature/prop id, and checklist index;
- server logs and screenshots/video;
- positive objective progress and negative checks for inactive/wrong quest
  state;
- notes on whether the row is official-current, fallback-only, or branch-only.

## LWS-037 WIP/GUESSED Conversion Gate

Blocked. No WIP/GUESSED checklist/entity rows are converted to implemented
status without LWS-036 smoke proof. Current audit docs intentionally keep these
rows WIP/GUESSED, blocked, or rejected according to their evidence level.

Use `Start-BlockerEvidenceHarness.ps1` to capture future bundles, then promote
only the specific proven rows and update the corresponding audit and verifier
comments.
