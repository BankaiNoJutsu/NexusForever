# Friendship presence after rapid login/logout: observed follow-up

Later checkpoint7728: [Durable presence generations](FRIENDSHIP_PRESENCE_GENERATIONS.md) implements and verifies the received-event generation contract. The original plan/earlier verification below retains its date and scope; Quest and full-assessment requirements remain open.

Later7648→7666 follow-through: [Friendship presence transactions](FRIENDSHIP_PRESENCE_TRANSACTIONS.md) implements the reproduced stale-tracker overlap defect and a different-identity logout guard. The original observation below is retained; same-character session-generation ordering remains open.

30 October 2026, checkpoint7648. **Observed, not fixed.** The retained read-only
`runtime-checkpoint7648/RuntimeAudit/presence-before-recovery.json` shows synthetic
Account26 as `0:73:1` (presence/character/realm), while Accounts24/25 are
`3:NULL:NULL`. WorldPID22808 had completed every admitted player's cleanup;
the Friendship queue was empty. The initial final-presence audit failed.

One ordinary isolated `loadtest9603@example.local` login/world/logout, recorded
under `runtime-checkpoint7648/presence-recovery`, settled Account26 to
`3:NULL:NULL`. The subsequent full database audit passes. No direct SQL repair
was used. This recovery is one of242 completed world cycles across243 attempts;
the other unsuccessful attempt was a separately retained character-selection
rejection correlated with pending cleanup. Neither observation is erased by
the later105/105 mixed-world repeat or final settled database state.

The relevant source boundaries are:

- `Source/NexusForever.Server.Friendship/Network/Internal/Handler/Player/PlayerLoggedInHandler.cs`
  runs remote account/character resolution, active-character updates and the
  outbox in an execution-strategy transaction.
- `Source/NexusForever.Server.Friendship/Network/Internal/Handler/Player/PlayerLoggedOutHandler.cs`
  resolves the character/account and clears active identity through a separate
  context/save path.
- `Source/NexusForever.Server.Friendship/Game/Account/Account.cs`
  maps active identity to Available/Invisible presence and publishes updates.
- `PlayerLoggedInMessage` carries Identity/AccountId; `PlayerLoggedOutMessage`
  carries Identity. Neither currently carries a login generation or source
  session identity.

Overlapping or delayed login/logout writes are a source-backed hypothesis, not
yet a proven cause. Next, retain the relevant Friendship handler logs and build
deterministic interleaving tests around real contexts and outbox settlement.
Check logout during login bootstrap, delayed old login/logout after a new
session, repeated messages, different characters/realms, retries and restart.
Choose durable ordering/session semantics from that evidence; a process-local
mutex alone cannot establish order across services or redelivery. Preserve
existing presence preferences, friendship notifications and entitlement
defaults. Run full build/tests and repeat isolated source/provider/runtime
qualification after implementing the justified fix.

The Quest participant bundle is not enabled by any production caller at this
checkpoint. No causal claim ties this presence observation to that inactive
new path. The full assessment Goal remains active; native Computer Use remains
stopped.
