# Shared Quest and Challenge queue ownership

The internal queue boundary is now[implemented and verified at7831](QUEST_CHALLENGE_REWARD_QUEUE.md). The original plan below remains historical; live activation/mode/modifier and terminal producer integration is next, with earning still disabled.

Next connected implementation after checkpoint7774. Live earning remains
disabled until this queue boundary and validated producers are wired and tested.
The entire assessment Goal remains active, including other matrix/content rows.

The current Quest capture can execute a shared immutable SQL command in
isolation. The current Challenge queue separately owns Challenge persistence and
inventory mutation. Join them around one exact command and executor result;
do not execute once through each lease or construct an equivalent second result.
An earning completion has no item reservation: points/Quest/Challenge changes
commit together, and a later claim owns actual item delivery.

Extend a queued request with its actor-owned Quest capture. Preserve immediate
event census and concrete references when enqueueing; a delayed broad objective
event would credit Quests accepted later. Resolve the retained database identity
before invoking that event. Refuse capture before any provisional effects while
a full Player save owns its Auth/Character phase. Define and test the producer
policy for that refusal before enabling earning.

Retain the Challenge queue ticket, Challenge/inventory lease and Quest character
scope through preparation, execution, outcome recovery and actor adoption. An
uncertain Challenge load probe must finish before building the completion
command. A Quest probe must use copied values from the FIFO head while those
owners remain held. Attach its immutable changes exactly once, then execute the
resulting command through both retained ownership boundaries. A resumed result
must update the operation retained by both final-disposal tails.

Preflight Challenge runtime merge/baselines and every Quest result before
acknowledging either aggregate. On any mismatch, quarantine both without
publishing presentation or inventory effects. Apply successful adoption on the
actor without awaiting, preserving later values and dirty revisions. Release
both owners only after adoption; a later queued completion must see the new
baselines. Publishing occurs after all adoption succeeds.

Cancellation must dispose every queued Quest capture as well as its ticket and
delivery. An event already run requires reload even if its SQL never started.
Unknown original write units keep both owners pinned; final disposal may release
only after exact terminal proof, without actor mutation or UI publication. A
request retained during identity/Quest probe work must also be canceled safely.

Add integrated queue cases for terminal medal/positive-tier Finish, script-created
Quests, FIFO completions/replacements, later dirty fields, active normal-save
refusal, uncertain load/write/recovery, first/second adoption failure, queued
cancellation and final disposal without session-event polling. Run full build
and tests, freeze source/binaries, replay exact-source provider tests and retain
the existing protocol/default/original-data gates. Do not claim live producer,
real network earning/claim or native UI completion from internal queue tests.

After the queue slice, enable only evidence-backed activation/completion modes
across the16 supported catalogs with frozen per-medal modifiers and no direct
plus track double grant. Keep legacy NULL attempts, the five empty catalogs,
and unsupported or unvalidated producer paths on their existing behavior.
Computer Use remains stopped until the user explicitly resumes it.
