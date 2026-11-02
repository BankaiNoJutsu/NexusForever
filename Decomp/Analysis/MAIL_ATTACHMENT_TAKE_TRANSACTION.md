# Individual mail attachment collection transaction

1 November 2026 destination continuation. All 10,636 full tests pass with zero
failures/skips and a zero-warning/error full rebuild. All 10,620 prior test names
remain; 16 new individual-collection cases pass. The refreshed-binary mail run
passes all 184 cases. The complete 6086-input manifest, archive and eight DLLs
verify separately from the earlier 10620 outgoing-mail proof.

Individual collection now stages the selected Item transfer, complete mail
header and selected attachment-link deletion in one existing Character save
capability. A pending receipt excludes competing cash, take, return, delete,
expiry and normal-mail saves. Later read and Item intent retains its own dirty
revisions. Preparation retains the incoming Item's complete state and restores
it when SQL never starts. Incoming collection does not require a shared-bank
entitlement or change account/character grant defaults.

Storage witnesses check the whole mail membership, selected link custody and
all sibling Items, as well as the transferred Item and header. Duplicate mail
custody is already rejected by the schema's unique Item link. A simultaneous
realm-bank row is rejected rather than consumed as an ordinary withdrawal.
Fresh retries accept an already-removed link only after its row owner reconciles
the command's retained before/after candidates; unexplained first-attempt absence
still fails. Source/recipient ownership and captured database identity remain
material. Adoption and success packets follow confirmed storage on the original
actor. Failed or abandoned publication requires authoritative reload.

The executed cases cover SQL-trigger rejection at each written component,
replaced links, sibling Item corruption, preparation rollback/retry, later read/
Item intent, concurrent mail actions and normal saving, source-manager replacement,
no shared-bank entitlement, realm-bank conflicts, schema uniqueness and replay.
An actual SQLite TransactionCommittedAsync IOException is injected through a
test-only private-context factory and manually retried in a fresh context. It
produces one stored Item and one success publication. This qualifies the source
unit, not a configured MySQL retry strategy or a physical network failure.

Three separate source epochs remain frozen with complete inputs and binaries:

| Epoch | Inputs | Failed | Passed | Qualification |
| --- | ---: | ---: | ---: | --- |
| negative | 6082 | 3 | 1 | Premature success and failed/replaced-link acceptance; ordinary committed control passes |
| prototype | 6085 | 2 | 2 | Invalid non-null character freeze token breaks success paths; failure controls do not prove their intended faults were reached |
| recovery-negative | 6086 | 2 | 13 | One real replay failure; one fixture setup failure from the existing unique custody constraint |

The prototype token failure is recorded in its runtime log. The corrected private
command uses the existing mail command's null-token scope pattern. A later
sibling-corruption control initially used `stack_count` instead of the real
`stackCount` column, so it passed on an unrelated SQL error. That raw log remains;
the final test requires the intended `Mail Item SQL differs` witness error and
unchanged sibling data. The invalid duplicate-custody fixture is now a schema-
guard control. These corrections are explicit and are not promoted as feature
failures. Intermediate 4/53/183-case passes retain their narrower meaning.

The full run took 9m24s. `Tools/Handoff/Verify-Continuation.py` rejects the changed
working source against its historical 10277 manifest, which remains unchanged.
The independent local `mail-take/verify.py` passes the final full/focused results,
all source/binary bindings, original epochs and retained fixture corrections.
Upstream at 1 November 00:19:43 UTC still has 56 open PRs and 56 unchanged heads,
including the previously reconciled #608 and #609.

Bulk-selection cash/attachment composition remains the next required slice.
The existing public loop starts separate asynchronous actions back-to-back;
this individual-action suite does not qualify it. Manual return/delete reply
timing, marketplace settlement, durable recovery after a consumed uncertain
command and broader feature work also remain. The [native index investigation](MAIL_ATTACHMENT_INDEX_EVIDENCE.md)
is mapped-only: public single-index lookup and reply interpretation are unchanged.
No fresh MySQL/configured-strategy, copied-upgrade, socket/deployment/load,
native presentation or retail completeness is established. Defaults remain
15:1/17:1 with no automatic44 or seven baseline grants. No push or Computer Use/
native client launch was performed; those stops remain and the Goal is active.
