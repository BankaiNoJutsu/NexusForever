# Official pull request comparison

Snapshot: 29 October 2026, Europe/Zurich. All 55 open official PR heads were fetched and their changes compared against the fork. The table distinguishes targeted source/test findings from inventory-only items; it is not a claim that every PR received a full behavioral review. Different or absent file paths do not establish missing functionality, especially across legacy develop and expedition architecture.

The fork is `legacy-di-storefront-hardening` at `f069e0b9`. Official game_rework is `1f643f63`. Source-derived notes and executable findings are explained in [ASSESSMENT.md](I:/GIT/NexusForever-assessment-report/ASSESSMENT.md).

| PR | Subject | Target branch | Relationship and assessment |
| --- | --- | --- | --- |
| [#604](https://github.com/NexusForever/NexusForever/pull/604) | Taxi: discover taxi kiosks and handle taxi flights | game_rework | Partial overlap: existing 0x00FF flight handler; reconcile node persistence and origin/faction checks. |
| [#607](https://github.com/NexusForever/NexusForever/pull/607) | Mount: one mount per summon, summon auras pass the global cooldown, Dismount clears a lost mount | game_rework | Partial overlap: dismount exists; pending summon protection and automatic aura/GCD behavior need review. |
| [#588](https://github.com/NexusForever/NexusForever/pull/588) | Galactic Archive: unlock articles from achievements and quests | game_rework | Substantial parallel implementation; fix wire widths first, then completion hooks and title reward semantics. |
| [#598](https://github.com/NexusForever/NexusForever/pull/598) | Fix exception when activating a datacube the character already has | game_rework | Already addressed centrally: AddDatacube merges and resends an existing entry. |
| [#606](https://github.com/NexusForever/NexusForever/pull/606) | Instance: return location for portal entries and the Leave button | game_rework | Partial overlap: Leave handler and instance return cache exist; persistent bind fallback differs. |
| [#605](https://github.com/NexusForever/NexusForever/pull/605) | Spell: handle AddSpell and Disembark for the riding licenses | game_rework | AddSpell and Disembark exist; compare learned-passive execution and login reapplication. |
| [#603](https://github.com/NexusForever/NexusForever/pull/603) | Spell: Recall House returns to the residence | game_rework | Housing teleport and stuck housing action exist; reconcile spell teaching and prerequisite dependencies. |
| [#602](https://github.com/NexusForever/NexusForever/pull/602) | Spell: Recall Transmat returns to the bind point | game_rework | Behavior differs: stuck transmat uses zone exit; zero-location Teleport does not recall to a saved bind. |
| [#601](https://github.com/NexusForever/NexusForever/pull/601) | Player: bind at transmat terminals and send the bind point | game_rework | Missing persistent bind-point path; useful addition with migration reconciliation. |
| [#600](https://github.com/NexusForever/NexusForever/pull/600) | Prerequisite: add UnhealthyTime (269) and Dueling (204) checks | game_rework | Corresponding checks absent; confirm semantics and dependency chain before adding. |
| [#597](https://github.com/NexusForever/NexusForever/pull/597) | Fix server dirty character jobs never ending when a character is marked dirty again during a run | game_rework | Strong integration candidate: fork retains shared tracked context in both dirty-character loops. |
| [#596](https://github.com/NexusForever/NexusForever/pull/596) | implement SpellEffect 73 ModifySpellCooldown | expedition-evil-from-the-ether | ModifySpellCooldown already implemented differently; compare effect semantics, do not replace by name. |
| [#595](https://github.com/NexusForever/NexusForever/pull/595) | Implement healing HP and Shields, also implement base values for focus, and fix oversight in ServerSpellConfirmCast/ServerSpellCastResult | expedition-evil-from-the-ether | Healing/shield/spell work overlaps fork runtime; expedition architecture requires focused semantic review. |
| [#594](https://github.com/NexusForever/NexusForever/pull/594) | Align prerequisite comparison enum with the client values | expedition-evil-from-the-ether | Comparison values 3-6 differ; client evidence check needed before changing behavior. |
| [#590](https://github.com/NexusForever/NexusForever/pull/590) | Entitlements: give every character the base entitlements of a full account | game_rework | Separate hidden-bit and insert corrections from debated automatic entitlement defaults. |
| [#581](https://github.com/NexusForever/NexusForever/pull/581) | Fall back to the player's last open world position when leaving a content instance | expedition-evil-from-the-ether | Instance return behavior overlaps existing fork cache; compare fallback and reconnect semantics. |
| [#586](https://github.com/NexusForever/NexusForever/pull/586) | NPE quest scripting example: Navigating Nexus → Claim to Stake | game_rework | Early-zone scripts substantially overlap; reconcile quest chains and evidence instead of wholesale import. |
| [#584](https://github.com/NexusForever/NexusForever/pull/584) | Handle /resetinstances for solo instances | expedition-evil-from-the-ether | Inventory and path comparison only; review solo reset behavior against fork instance lifecycle. |
| [#578](https://github.com/NexusForever/NexusForever/pull/578) | Coalesce movement relocations in the map grid action queue | expedition-evil-from-the-ether | Inventory and path comparison only; compare queue ownership/coalescing with fork movement changes. |
| [#568](https://github.com/NexusForever/NexusForever/pull/568) | Generalise prerequisite evaluation and spell contexts | expedition-evil-from-the-ether | Broad prerequisite/context refactor; architectural comparison, not a small cherry-pick. |
| [#582](https://github.com/NexusForever/NexusForever/pull/582) | Allow client side interaction spells on entities that aren't units | expedition-evil-from-the-ether | Inventory and path comparison only; verify non-unit activation against fork interaction/spell handling. |
| [#585](https://github.com/NexusForever/NexusForever/pull/585) | The Hycrest Insurrection: intro and first mission vote (adventure, world 1149) | expedition-evil-from-the-ether | Instance/script work overlaps; compare mission-vote producers and existing fork adventure scripts. |
| [#580](https://github.com/NexusForever/NexusForever/pull/580) | Fix players standing on platform entities | expedition-evil-from-the-ether | Inventory and path comparison only; verify platform movement with mounted/visible entity cases. |
| [#570](https://github.com/NexusForever/NexusForever/pull/570) | Fix Aspire setup and migrations | game_rework | Complementary bootstrap work; retain fork Windows/SQLite setup and compare migration ordering. |
| [#576](https://github.com/NexusForever/NexusForever/pull/576) | GameTable: allow retry after cache initialization fails | game_rework | Fork lock-based initialization avoids the stuck claimed-flag design; source comparison only. |
| [#575](https://github.com/NexusForever/NexusForever/pull/575) | GameTable: read supplementary Unicode characters in wide strings | game_rework | Missing: three supplementary UTF-16 regression cases fail on fork and official. |
| [#573](https://github.com/NexusForever/NexusForever/pull/573) | Network: reject reads past the end of a game packet | game_rework | Partly addressed: fork passes 8/10 probes; repeated EOF state remains broken. |
| [#572](https://github.com/NexusForever/NexusForever/pull/572) | Network: make listener startup and shutdown reliable | game_rework | Missing lifecycle fix: occupied-port and shutdown-wait probes fail on both branches. |
| [#566](https://github.com/NexusForever/NexusForever/pull/566) | Network: replace APM socket loop and FragmentedBuffer with System.IO.Pipelines | game_rework | Architectural alternative: fork retains APM with pooling; compare framing/STS/lifetimes before adopting. |
| [#521](https://github.com/NexusForever/NexusForever/pull/521) | Item / inventory messages | game_rework | Large protocol inventory; compare each layout and preserve fork handlers rather than replacing files. |
| [#557](https://github.com/NexusForever/NexusForever/pull/557) | Apply Armor Pierce before mitigation by reducing effective armor | game_rework | Inventory and path comparison only; validate armor-pierce order against fork combat semantics. |
| [#554](https://github.com/NexusForever/NexusForever/pull/554) | [WIP] [Expedition branch] Implementation of Prerequisites | expedition-evil-from-the-ether | Large expedition prerequisite work; overlaps fork checks, requires evidence and architecture review. |
| [#514](https://github.com/NexusForever/NexusForever/pull/514) | Unit messages - fixes and additions | game_rework | Protocol inventory only; many renamed/new paths, no blanket claim of coverage. |
| [#515](https://github.com/NexusForever/NexusForever/pull/515) | Player messages | game_rework | Protocol inventory only; reconcile field widths with fork player handlers and fixtures. |
| [#542](https://github.com/NexusForever/NexusForever/pull/542) | Ability messages, part 2 | game_rework | Protocol inventory; two changed files byte-identical, remaining files require semantic review. |
| [#540](https://github.com/NexusForever/NexusForever/pull/540) | Movement messages | game_rework | Protocol inventory only; compare widths and movement lifecycle with fork diagnostics/control. |
| [#537](https://github.com/NexusForever/NexusForever/pull/537) | Miscellaneous messages | game_rework | Protocol inventory only; absence of original filenames can reflect renamed fork models. |
| [#482](https://github.com/NexusForever/NexusForever/pull/482) | Update the game message opcodes list | game_rework | Opcode inventory overlap; preserve fork evidence-backed names and verify numerical registrations. |
| [#165](https://github.com/NexusForever/NexusForever/pull/165) | Gameplayer/Features: Transmat Support & Client Interaction - WIP | develop | Legacy develop target; compare bind/interaction concepts with modern fork architecture. |
| [#358](https://github.com/NexusForever/NexusForever/pull/358) | Spells/Telegraphs: DamageFlag MVP & Esper Support | develop | Legacy develop target; assess combat concepts, not direct file-path or cherry-pick compatibility. |
| [#471](https://github.com/NexusForever/NexusForever/pull/471) | Dependency injection rework of basic amp PR418 | game_rework | AMP/DI overlap; compare learned abilities and passive runtime in current architecture. |
| [#466](https://github.com/NexusForever/NexusForever/pull/466) | Prerequisite/Checks: Checks Update - rework | game_rework | Prerequisite overlap; compare exact check semantics and client comparison enum. |
| [#463](https://github.com/NexusForever/NexusForever/pull/463) | WIP [Spell/AMP] Make AMPs cast their related spell when learned | game_rework | AMP/passive overlap; compare casting and removal/relogin lifecycle. |
| [#455](https://github.com/NexusForever/NexusForever/pull/455) | Spell/Effects: Stealth, Cooldown and ForceRemove - WIP | game_rework | Spell-family overlap; fork has richer interpreter/runtime work, semantics still need review. |
| [#449](https://github.com/NexusForever/NexusForever/pull/449) | [Entity/Vehicle] Fix vehicle attach for players mounted already when they enter range | game_rework | Vehicle visibility overlap; compare attach packets for already-mounted arrivals. |
| [#283](https://github.com/NexusForever/NexusForever/pull/283) | Quests/Core: Checklist quests support - WIP | develop | Legacy checklist concepts overlap fork quests; port behavior only after validating current model. |
| [#275](https://github.com/NexusForever/NexusForever/pull/275) | Account/Inventory: Inventory management, item binding | develop | Legacy inventory/binding concepts overlap fork account inventory; migrations/APIs differ. |
| [#272](https://github.com/NexusForever/NexusForever/pull/272) | Loot/RewardTracks: Account & Character Tracks support added | develop | Legacy reward tracks overlap fork reward runtime; compare grant/persistence behavior. |
| [#260](https://github.com/NexusForever/NexusForever/pull/260) | Spell/Effects: Stealth, Cooldown and ForceRemove - WIP | develop | Legacy predecessor to newer spell work; no direct integration recommendation. |
| [#411](https://github.com/NexusForever/NexusForever/pull/411) | [Store/Purchases] Purchasing support with current character | develop | Fork has much broader storefront purchases/history/rollback; legacy PR depends on #275. |
| [#401](https://github.com/NexusForever/NexusForever/pull/401) | [Guilds/Core] Empty guild and chat name caches upon disbanding guild - Resolves #394 | develop | Legacy guild-cache fix; validate current guild lifecycle before porting concept. |
| [#399](https://github.com/NexusForever/NexusForever/pull/399) | [Item/Runes] Support Runes and Rune Slots | develop | Rune support already exists on fork; compare semantics in current item/microchip model. |
| [#391](https://github.com/NexusForever/NexusForever/pull/391) | [Guilds/Communities] Handle Residence(s) on Disband or Leave | develop | Legacy guild/housing lifecycle change; inspect current cross-service ownership before porting. |
| [#329](https://github.com/NexusForever/NexusForever/pull/329) | Auto move materials to satchel then the bag if full | develop | Legacy inventory routing behavior; verify modern satchel/bag flow before considering a port. |
| [#418](https://github.com/NexusForever/NexusForever/pull/418) | Basic AMP implementation | develop | Legacy basic AMP predecessor; compare newer #471 and current fork AMP runtime. |

## Relevant merged and closed work

| PR | Snapshot state | Relationship |
| --- | --- | --- |
| [#587](https://github.com/NexusForever/NexusForever/pull/587) | Merged | Archive IDs now 14 bits; fork fails all three compatibility probes. |
| [#591](https://github.com/NexusForever/NexusForever/pull/591) | Merged | Reader fixes; auction cancellation and resurrection differ demonstrably, some other wire shapes already agree. |
| [#593](https://github.com/NexusForever/NexusForever/pull/593) | Merged | Matching random-party packet registration; inspect fork equivalent before importing renamed definitions. |
| [#589](https://github.com/NexusForever/NexusForever/pull/589) | Merged | Web console character command context; compare with fork command/DI boundary. |
| [#571](https://github.com/NexusForever/NexusForever/pull/571) | Merged | Socket Close moved to finally; fork still has the vulnerable exception path. |
| [#489](https://github.com/NexusForever/NexusForever/pull/489) | Merged | Spell packet refinements; reconcile against fork evidence-backed models individually. |
| [#520](https://github.com/NexusForever/NexusForever/pull/520) | Merged | Account inventory messages; substantial overlap with fork's existing account/economy implementation. |
| [#563](https://github.com/NexusForever/NexusForever/pull/563) | Merged | Housing messages; fork has prior housing-message schema work, so compare fields/migrations. |
| [#364](https://github.com/NexusForever/NexusForever/pull/364) | Merged into loot branch, now in game_rework | Large loot generation/distribution and group-state reconciliation needed. |
| [#565](https://github.com/NexusForever/NexusForever/pull/565) | Closed without merge | Maintainer requests investigation of prior runtime failures from packet scopes; do not enable blindly. |
| [#574](https://github.com/NexusForever/NexusForever/pull/574) | Closed without merge | Upstream removed Blocking instead; fork retains API and fails 3/4 imported event tests. |

Raw metadata and descriptions: [pull-requests.json](I:/GIT/NexusForever-assessment-report/pull-requests.json). File counts, blob identity and overlap: [pr-file-comparison.csv](I:/GIT/NexusForever-assessment-report/pr-file-comparison.csv) and [pr-file-comparison.json](I:/GIT/NexusForever-assessment-report/pr-file-comparison.json). Maintainer discussion excerpts: [maintainer-comments.json](I:/GIT/NexusForever-assessment-report/maintainer-comments.json).
