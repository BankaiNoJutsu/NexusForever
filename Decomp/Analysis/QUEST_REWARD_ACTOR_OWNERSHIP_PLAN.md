# Quest reward actor ownership

Internal actor work is now verified at[7774](QUEST_REWARD_ACTOR_OWNERSHIP.md). The original plan below is retained; Challenge queue/producer integration is the next connected slice, with live earning disabled.

Active implementation after7728. Retain the entire assessment Goal; do not
enable live earning before this ownership, queue integration and producer
validation are complete.

Capture the actual actor completion event once, with a complete before/after
QuestManager census. Keep concrete Quest/objective references and dirty-field
revisions. A character-wide process/database reservation must cover unknown
new quests too, block normal root/direct-objective saves and other loaded
owners, and remain held across queued completion units. Refuse capture before
invoking the event while a normal Quest unit or real Player save is active.
The live producer must explicitly handle the Auth/Character save-phase gap;
delaying an unbound broad objective update would credit later accepted quests.

Affected graphs include scalar or revision changes, creation/deletion and
replacement/reacceptance. Read copied baseline candidates on the actor when a
capture reaches the head, so a later queued completion can follow an earlier
commit. Only immutable graph/candidate values go to a fresh serializable worker
probe. Validate full SQL graphs, including extra/missing objectives and actual
provider reset precision. Build the existing immutable shared command from
resolved before snapshots and frozen after values, never replay a broad event.

Bind adoption to the exact executor command/database/terminal proof. Return
copied actual Quest SQL snapshots with the executor result. Preflight every
participant before acknowledging any of them. Advance persistence baselines;
clear only matching captured revisions/current values; never overwrite later
actor values. Retain deletion/restoration intent, retire replaced old owners,
and retire committed deletions on the actor. Do not infer an acceptance identity
from a quest ID alone.

Cancellation/failure after an actor event requires reload, not an ordinary save
of provisional completion progress. Unknown original write units remain pinned;
final disposal may release only after genuine terminal proof, without runtime
adoption or client publication. Test FIFO captures, duplicate/foreign proofs,
normal-save overlap, uncertain baselines, later/same-value revisions, scripts,
collection changes, deletion/restoration/replacement, reset conversion and
invalidation. Run full build/tests and exact provider/runtime qualification.
Then connect Challenge queue ownership and live earning across all16 catalogs.
