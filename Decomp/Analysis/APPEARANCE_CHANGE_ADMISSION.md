# Appearance change input admission

5 November 2026. Parsed appearance requests now reject non-finite bones and
duplicate customisation labels before catalog validation or payment. The same
structural check runs at the start of the public `AppearanceManager.Update`,
before race/sex, customisations, visuals or bones change. Null collections and
more than256 bones are also rejected there. This prevents direct callers from
partially applying a malformed update before the old bone-count check throws.

## Reproduced failures and corrected behavior

The original public-handler probe reads the actual packet body with
`ClientCharacterAppearanceChange.Read` and uses concrete `AppearanceManager`,
Character `CurrencyManager` and `AccountCurrencyManager` owners. Player/session,
catalog validation and price/table rows are fixture collaborators.

The published-base run has15 failures and10 passing controls. Eight parsed
NaN/infinity/duplicate-label cases, split across Credits and Service Tokens,
were accepted. A diagnostic run records each selected wallet falling from100
to93, sex changing to Female and the malformed bones or duplicate-label update
being applied. Seven direct-manager cases also fail; notably,257 finite bones
throw only after sex/customisations/visuals have changed. Null collections
produce late null-reference failures. These are executed Source negatives,
not inferred retail behavior. Initial fixture compilation errors are retained
separately and are excluded from production-negative evidence.

The corrected handler raises the existing `InvalidPacketValueException` before
price calculation, debit, appearance mutation or any packet publication. Direct
manager calls raise `ArgumentException` without changing the loaded state.
Both wallets remain100, the original race/sex, label, visual and bone remain,
and no visual or wallet notice is emitted. No numeric bone range is invented:
empty arrays,256 finite bones and finite extremes keep their previous accepted
behavior. Valid requests pay7 from only the selected wallet and use the existing
appearance/visual path; unaffordable requests remain inert. Existing catalog,
race/faction, price and entitlement policies are unchanged.

## Verification and limits

All25 new and55 unique affected cases pass. Selection includes the original
protocol hardening and wardrobe manager controls. The owning WorldServer build
passes with0 warnings/errors in14.14s; the final test-only build also passes.
The scoped test body takes0.32s. Compilation and fixture correction dominate
this gate; no broader test batch or full solution build was run.
Both final review axes report0 findings.

The [portable record](coverage/appearance-change-admission-20261105.json) binds
the fixed published base, source/DLL identities,15 baseline failures, eight
explicit wallet/mutation observations, final55 cases, build logs and both review
axes. Validation uses an isolated published-source candidate; concurrent
PvP/prerequisite/group/friendship/native work is excluded and preserved. Local
evidence is under `artifacts/continuation-20261105/appearance-change-admission/`.

This closes malformed appearance input admission through the public handler
and direct manager. It does not qualify actual customization catalogs/costs,
durable atomic appearance/payment settlement, rejected-save recovery, committed
visual publication, provider/deployment/socket/native or retail behavior.
F-026 remains partial. Native client/CUA remain stopped; defaults15:1/17:1 and
the no-auto44/seven-baseline policy are unchanged.
