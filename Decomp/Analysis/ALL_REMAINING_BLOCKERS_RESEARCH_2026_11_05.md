# All remaining blocker families - 5 November 2026

The audit covers all 36 roadmap feature areas, the five consolidated emitter
rows, and the cross-feature Contract, financial, content and deployment gates.
It separates missing semantics/data from unfinished Source behavior and from
execution/retail qualification. This is a family-level audit; it does not claim
row-by-row completion of the generated quest/instance inventory.

## Evidence index and complete scope

| Record | Coverage |
| --- | --- |
| [36-area inventory](ALL_BLOCKERS_INVENTORY_2026_11_05.md) | Every F-001..F-036 area, the five emitter gates and cross-feature requirements, with current local evidence |
| [Economy/progression research](ALL_BLOCKERS_ECONOMY_RESEARCH_2026_11_05.md) | Contracts, Fortune, reward rotation/tracks, marketplace, Primal allocation and guild financial/bank work |
| [Gameplay/social research](ALL_BLOCKERS_GAMEPLAY_SOCIAL_RESEARCH_2026_11_05.md) | Crafting, transport, recruitment/perks/warparty tokens, ICComm, duels, LAS and matching |
| [Native/authentication/combat research](ALL_BLOCKERS_NATIVE_RESEARCH_2026_11_05.md) | STS notification/parser guards, all16 diagnostic client surfaces, Server0015, five spell-runtime areas, content and host qualification |

Each family remains answered, partially answered or unresolved at its recorded
boundary. A mapped packet does not close a producer gate; a Source milestone
does not qualify a provider, physical failure, native UI or retail behavior.

## Incorrect blockers retired or corrected

| Old blocker premise | Current disposition |
| --- | --- |
| No static decompilation can be performed | Separate read-only Ghidra/LLVM inspection is available. REA's native-provider limitation remains; no installation is needed for the inspection path used here. |
| Marketplace still needs06DF/07D5 auxiliary consumers | Rejected ownership.06DF is Primal Matrix, now wire-implemented;07D5 is reward-track update, already routed. These are not remaining marketplace producers. |
| Reward Rotation waits for07CD content-context/Flag apply | Rejected ownership.07CD/07D3 add/reload reward tracks. Rotation still needs actual per-content catalog/schedule/throttle semantics and reward delivery; the missing-consumer premise is obsolete. |
| Crafting station keys have no names | Names and observed reset/replacement paths are mapped. They do not close discovery/complex craft or084B/0855. |
| Tracked updates need a separate unit-ID allocator followed by a trailing15-bit slot lookup | Rejected chain. First32 bits select TrackingSlot; final15 bits are neutral. The separate [Source correction](TRACKED_MARKER_IDENTITY.md) is now implemented; producer timing and row selection remain open. |
| All six entity-stat aux packets have only readers and no production emitter | Superseded. Six consumers are mapped;08CC scanner naming is implemented. Five other producer gates remain. |
| Primal save wire is missing | The separate [wire milestone](PRIMAL_MATRIX_WIRE_CONTRACTS.md) implements0072 decoding and06DF registration. The handler remains diagnostic; allocation gameplay is still open. |
| Quick Stats13 complete/21 partial is authoritative | Its own36 headings yield11 implementation-COMPLETE,23 partial/emulator-only, one diagnostic and one structural. Correct the aggregate without inventing feature reclassification. |

These corrections remove false research prerequisites. They do not mean the
underlying gameplay families are now retail complete.

## New usable inputs from this broader pass

- **STS:** the selected TokenKeyData notification uses flags `0x10`; its generic
  dispatcher now has a concrete non-null-message/status/virtual-predicate map.
  The base XML accessor has a final-LF parsing boundary. Status notification and
  crypto-filter completion are different rails. Complete wire grammar,
  trust/private-key/padding, startup order and accepted traffic remain open.
- **LAS:** the authored UI explicitly handles a later `ActionSetError` after an
  initially successful request, including reopening an already closed window.
  This supports distinct immediate acceptance and later outcome. The current
  public handler offers a Source-owned rejected-save/actor-replacement probe;
  the audit did not execute it or declare a reproduced defect.
- **Guild bank/perks:** the authored UI passes `idPerk` to `PurchasePerk`, with
  influence-spending and first-locked-tab guards. A tab ordinal is not the API
  purchase ID. Actual server permission, settlement and persistence still need
  implementation and verified rules.
- **Recruitment:** original PR536 reports that the Lua data API existed without
  a shipped Carbine UI. The sampled official-addon mirror likewise has no
  corresponding module. Native getter/readback and subscription semantics are
  useful next evidence; missing UI source is not proof no recruitment protocol
  exists.
- **Warparty tokens:** authored request/update/getter paths distinguish listing
  inventory from casting a token. Persistent inventory and cast results remain
  separate Source/policy work.
- **ICComm:** fresh pinned name/function pairs identify `SendMessage` and
  `SetJoinResultFunction`. Missing-entitlement enum documentation is not an
  entitlement ownership/restore policy or a chat-aux producer witness.
- **Contracts:** original upstream272 contains an all-chosen reset and carried
  points prototype. It is emulator Source, not an accepted retail reset-edge
  fixture. It cannot close unclaimed/final-first/excess-point policy.
- **Fortune:** publisher records supply historical catalog and pity-quality
  boundaries. They provide no numeric per-item probabilities or demonstrated
  final-build active rotation. Exact weights remain unavailable in this pass.

The domain records give immutable revisions, hashes, exact API/native seeds and
qualification limits. Earlier station/role/marker results are carried forward
as resolved inputs rather than counted as new findings.

## Work that can proceed without another retail capture

| Work item | Concrete boundary available now | What is still required |
| --- | --- | --- |
| LAS durability/publication probe | Existing public handler and original actor/save machinery; independent delayed-outcome UI evidence | Execute rejected-save and actor-replacement cases before deciding the Source change; exact retail transaction policy remains separate |
| Guild creation financial probe | Registration and currency debit are separate Source operations | Reproduce combined fee/parent/member/affiliation failure and design the original-owner transaction/recovery boundary |
| Reward Rotation claim delivery | Current grant service records a grant; delivery is a distinct unfinished path | Actual supported reward rows, delivery/recovery implementation and complete public-flow proof |
| Guild bank/recruitment/token state | Log-only/empty Source handlers plus named APIs and wire models | Durable ownership, permissions, settlement/subscriptions and scoped public-flow/provider checks; do not invent uncaptured policy |
| Evidence tooling/content review | Exact missing holder report, table/corpus hashes and generated next-slice queues are named | Obtain or regenerate the specific artifact within existing scope; a broad test count or empty report is not a substitute |

The [tracked-marker correction](TRACKED_MARKER_IDENTITY.md) was implemented in a
separate concurrent milestone while this audit ran: four executed baseline
failures,118 affected passes, owning build and reviews belong to that milestone.
Do not repeat it as pending work or count those tests as validation of this audit.

These are implementation or controlled-probe gates, not reasons to keep doing
the same broad web search. No new Source implementation or Source probe is
claimed by this research pass.

## Gates that survive the research

The full per-area table remains the authoritative inventory. Its unresolved
groups include diagnostic request intent/0015 ownership; five entity-aux
producer policies; tracked trailing value and publication lifecycle; legacy
neighborhood triggers; crafting discovery/complex/aux/item-patch rules; exact
Fortune weights/catalog; Contract reset edges and full actual-table delivery;
storefront billing/offer/coupon fields; broader taxi/vehicle, matching/raid,
guild, ICComm, loot and PvP policies; five spell/creature families; attributes,
paths, quests, instances, reputation, cinematics, archive/achievement/map
content and their remaining native/UI semantics.

The communicator's exact missing corpus and other absent-table gates require
actual reviewed rows or independently matching evidence. Contract's 726 choices
have already been quoted from six actual tables; its remaining gate is real
ItemInfo/binding/stacking metadata-backed delivery and recovery, as recorded in
[the award quotes](CONTRACT_REWARD_AWARD_QUOTES.md) and
[claim transaction boundary](CONTRACT_CLAIM_TRANSACTION_AND_ACCOUNT_HANDOFF.md).
A schema,
publisher feature description or another game's STS implementation cannot
replace them.

Provider/migration/copied-upgrade, physical socket, restart/outbox, broker,
deployment/load and native/retail qualification also survive. Fresh local
discovery finds dotnet but no queried Docker/Podman/MySQL command on PATH, no
queried MySQL/MariaDB/Docker process and no default Docker engine pipe. This is
bounded host readiness, not proof no remote
engine exists. No service was installed or started. Client/CUA remain stopped.

## Validation and ownership

The 36-area inventory verifies unique coverage and links. The domain notes retain
primary source revisions and hashes; the selected STS guards have a separate
[portable byte record](coverage/all-blockers-sts-guards-20261105.json) checked
against the pinned DLL and read-only Ghidra program. Local links, label-row
uniqueness and whitespace are checked at this documentation boundary.

Delivery validation also applies all 27 changed label rows (23 client, four STS)
to the owned analysis copies, with analysis disabled, and re-exports those exact
functions. Every address/name pair is present in both `functions.csv` and
`selected_decompiled.c`. Input and export hashes are retained under ignored
`artifacts/continuation-20261105/research-delivery/`; archived inputs remain
unchanged. This verifies label/export agreement, not runtime behavior.

No production Source, configuration, database, client or server was changed by
this research lane. Concurrent Primal/guild milestones retain their own tests
and scope; their results are not counted as tests of this audit. Git history and
the verified remote destination determine subsequent delivery status.
