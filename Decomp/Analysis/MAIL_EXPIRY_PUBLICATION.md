# Available-mail expiry publication

1 November 2026. The source gate passes10703 full tests, zero failures/skips,
including all251 reviewed mail cases on the frozen rebuilt DLLs. All10685 prior
cases remain and18 expiry cases are new. Full solution/test-copy rebuilds have
zero warnings/errors.6099 final/6096 negative/6099 failed-fixture inputs,
complete archives/eight DLLs and both parallel review axes verify. Local proof:
artifacts/continuation-20261031/mail-expiry/verification.json. The Goal is unfinished.

Three executed public Update(1) failures show ServerMailItemDeprecation sent
before normal Character commit, after rollback and after an outer commit with
no flush. One committed control passes. The6096-input negative archive/eight
DLLs bind unchanged production and only the new expiry test file. The fixture
uses a mutable injected WorldConfig to make an existing one-minute-old visible
mail expire; it does not sleep, change host config or execute the client.

The implementation extracts the existing manual deletion attempt/receipt state into
MailDeletionOwner. Manual result ownership and outgoing return ownership remain.
An available expiry batch captures exact mail references and IDs and retains
each complete normal-save deletion proof independently of expiredMail cleanup.
Its original-actor callback publishes one grouped deprecation only when every
member is confirmed and the normal save scope has ended. Dead actors retire
without publishing. Login/pending expiry stays silent; legacy non-concrete
adapters retain their grouped path. Expiration eligibility and existing Item
retention/destruction behavior are unchanged by this slice.

The31-case prototype and45-case corrected expiry/manual gate pass, including18
new expiry cases and all27 manual cases. Both parallel review axes have zero
findings on six-file pin903c1e7b4d7ee0050260f6f2dc0040f5e871c900df1771727054586305475421.
An intermediate45-case run failed all18 expiry cases in the fixture's unsupported
SQLite ulong ORDER BY, while27 manual cases passed. Its6099-input archive and
DLLs remain retained. Sorting after AsEnumerable fixes that setup error; those
18 failures are not production regressions. The251 reviewed mail case names and
multiplicities match the passing full run on rebuilt DLLs; no redundant focused
rerun was needed. Configured provider/MySQL/copied upgrade, cross-process
durability, post-consumption packet replay, native response/UI and retail expiry/
Item destruction policy remain separate. NoExpiry flag semantics are not newly
inferred. The prior Challenge cleanup sharing failure remains preserved with
unproven cause; later source passes do not erase it. Goal active, defaults15:1/
17:1, no automatic44/seven grants, no push and no Computer Use/native launch.

The02:04UTC upstream refresh has56 unchanged open PR heads,609 newest, and two
unchanged official branches. The merged-since29 October query returns598 only,
whose duplicate/progress behavior was already reconciled. Marketplace custody/
publication/economics, Contract/retail and broader feature work continue.
