# Quest and Challenge snapshots across normal Player saves

Later checkpoint7959: [live producers](CHARACTER_CHALLENGE_LIVE_PRODUCERS.md) connects mapped count-based earning and legacy compatibility. The earlier scope below is historical; real socket earning/claim and native UI remain open.

Later checkpoint 7903: [world readiness](CHARACTER_REWARD_WORLD_READINESS.md) loads durable Character rewards before real map admission and preserves bootstrap publication. Actual earning producers remain open; the earlier checkpoint below retains its original scope.

30 October2026, checkpoint7870. Real direct/deferred Player saves now freeze Quest and Challenge targets before Auth enrollment when the reward queue has a retained database identity. Immediate actor completion capture can coexist with that older save; the older target cannot persist or acknowledge provisional completion state. **Live character-track earning producers remain unwired.** Startup/custom/no-identity saves keep the existing path, and reward capture still refuses their overlap. Computer Use remains stopped.

## Persistence boundary

Normal-save capture is pure actor metadata work. It uses the queue identity already discovered on its worker, reserves the database/character and copies Quest owners, collection membership, root/objective values, deletion intent and dirty revisions plus Challenge values/revisions. It performs no new provider discovery or SQL on the actor. The existing inventory save lease spans Auth and Character, and the normal snapshot spans that same gap.

The actual actor completion event still runs immediately, in original order, with scripts and concrete Quest ownership. It does not replay a delayed broad update over later accepted Quests. Its reward reservation recognizes only the same manager's frozen save token. Ordinary root/direct-leaf saves and other loaded managers are excluded. The older normal save stages only its captured rows/values/revisions, even after an event creates, replaces, restores or edits a Quest. Newer changes stay dirty. Context/database/owner binding rejects stale or foreign enrollment.

The reward probe waits until the normal snapshot ends, and the queue waits for the inventory save lease. Successful normal settlement advances baselines from its real flush without acknowledging later or same-value revisions. The queued reward then resolves those updated/uncertain candidates and commits its exact after graphs. This order prevents a previously captured normal target from writing after reward adoption. Failed Auth/Character saves quarantine provisional actor state before inventory ownership releases. Ending a snapshot early cannot invent terminal proof for an unfinished real context; ordinary row/unit ownership remains until its actual outcome.

Only Quest/Challenge targets are frozen by this feature. Other Player aggregates retain their existing save behavior. Before wiring earning, producer eligibility must require initialized ownership and queue readiness; the fallback startup/custom path is not permission to lose an event or switch a CharacterTrack attempt to a direct prize.

## Objective identity and order

Provider qualification exposed two existing ordinal assumptions. The composite objective key's valid ordinal0 had been treated as generated because the column has a default. EF's temporary byte generator can then collide with explicit ordinal1. Objective ordinals are now ValueGeneratedNever in the runtime model and both snapshots. The physical default0 remains; SQLite retains its existing INTEGER store type, MySQL its unsigned tinyint. Two frozen model gates report zero relational differences and no pending model changes. No new migration or DDL was added.

Loaded objective collections are HashSets and database materialization does not guarantee ordinal order. MySQL returned an actual progression array[0,2] where[2,0] was expected. The loaded Quest constructor now sorts by persisted Index before sequential checks use positional access. A deterministic reversed-row regression verifies both event steps; the provider replay verifies the actual MySQL load/save path.

## Verification and scope

Evidence: `I:/GIT/NexusForever-assessment-report/remaining-goal/reward-normal-save/final-review`; runtime: `remaining-goal/runtime-checkpoint7870`.

- Full solution build: **0 warnings, 0 errors**. Full suite: **7,870/7,870**, zero skipped. Full TRX SHA256:`DB4DCF6DCC26F5F79A688A6E67360A56B6E8EB9F56FA0944B88E4939F004830C`. **312 focused cases** pass, including the normal-save/reward/Quest/schema/bank gates.
- **39 new regressions** exercise real SaveDirect and deferred StartSave entry points, actual Auth-to-Character enrollment, immediate completion across all16 supported catalogs, after-staging events, older flushes versus newer dirty state, exact probe ordering, same-value touches, replacements, deletion/restoration, other owners/direct leaves, context/database rejection, failed commit outcomes, Auth failure, early snapshot ending, script-created/later accepted/sequential Quests, explicit ordinal0 after ordinal1 and reversed loaded rows, plus real cleanup and failure callback paths. Unrelated Player managers are mocked, and terminal intent is injected through the internal actor/queue API. These tests do not invoke a live Challenge activation/combat producer or client claim.
- **423 exact-source provider cases** pass without exclusions in fresh retained `nexus_assessment_reward_normal_save_30a4`:39 new plus384 previous reward/Quest cases. Each has hashed SQL snapshots before guarded synthetic Character42 cleanup. Actual build16042 reward catalogs are paired with synthetic Challenge sources. The count includes dispatch/provider-independent cases and SQLite guards, not423 concurrent MySQL workloads. Two separate frozen provider model gates bring combined replay/model checks to**425**; they are metadata checks, not a native migration claim.
- Verification binds**169 DLLs,5,768 Source files,19 copied test/helper files and19 changed Source paths since7831**.31 unchanged compatibility probes pass. Both model snapshots reconcile with explicit ordinal generation and unchanged defaults/store types. No new migration files exist; the owned Character column/index/FK/migration inventory hash is unchanged before/after staging. Pre-existing Humanizer/RabbitMQ.Client test-versus-World dependency differences remain separately bound. Entitlement/Loyalty sources, branch/head and clean original checkout are preserved. No new commit/push.
- Frozen WorldPID996 and Character APIPID29484, unchanged AuthPID9380, pass**236 complete protocol cycles** and236 initial composed reward reloads. All9 services remain alive; the other7 retain their process identities. Six queues drain naturally; latest generation markers close offline without repair/relog. Lifecycle errors, unfinished admissions and late/duplicate bootstraps are zero. Private configurations/assets, original data,15:1/17:1 defaults/no44 and empty new reward tables/null legacy attempt markers remain preserved. This existing login workload is not a live earning/claim proof.

The failed provider iterations remain under first-provider-review and second-provider-review, with retained isolated schemas. The first exposed a test fixture copying MySQL configuration after its password was removed on open; production configuration was intact. The second includes the safe sequential assertion diagnostic. Failed SQLite snapshot/store-type verification is retained separately. The earliest temporary-key failure has an extracted diagnosis; its original TRX was overwritten before archiving. Final verification uses the corrected source and fresh proof schema.

The first runtime checkpoint7866 is a failed gate, retained with its reports/logs
and FAILED_CHECKPOINT.json. Quiescent reward invalidation incorrectly blocked its
normal logout snapshot, and the synchronous failure path logged cleanup twice.
Normal snapshots now permit quiescent invalidated owners while reward work stays
disabled; cleanup invalidates Quest work too and completion/disposal callbacks
are single-shot. Four added regressions cover the real success, Auth failure,
synchronous snapshot failure and ordinary post-invalidation save paths. The strict
lifecycle audit is unchanged; only the corrected runtime checkpoint is accepted.

## Next implementation

Use the [live producer plan](CHARACTER_CHALLENGE_LIVE_PRODUCER_PLAN.md) to wire activation-time LegacyDirect/CharacterTrack selection, stable attempts, captured per-medal modifiers and validated activation/progress/completion/timeout/abandon/relog paths across all16 catalogs. Enforce readiness before mode selection, preserve historical NULL markers, five empty catalogs and unsupported producers, and prevent direct-plus-track double grants. Real protocol earning/choose/claim/relog/reset and native UI remain separate gates. The full assessment, other matrix/content rows and F-033 remain active/partial. All31 CSVs/167,484 rows remain not_retail_complete. The June19 Q5597 historical bundle is unavailable; its review does not recover native UI evidence. Computer Use stays stopped pending explicit resumption.
