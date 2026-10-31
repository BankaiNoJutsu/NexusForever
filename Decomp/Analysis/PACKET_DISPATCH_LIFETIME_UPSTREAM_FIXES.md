# Packet dispatch lifetime reconciliation

Source: closed [upstream PR #565](https://github.com/NexusForever/NexusForever/pull/565).
The maintainer's recorded comment asks for investigation of earlier scoped-provider
runtime problems before adopting packet scopes. This pass investigates the current
fork rather than assuming the old patch can be enabled unchanged.

## Concrete scope escape

Before this change, GameSession resolved messages and handlers from its injected
provider; the per-packet scope statements were commented out. AuthSession and
WorldSession both inherit this dispatch path. STS uses a separate protocol path.

The current fork contains a specific lifetime hazard if scopes are enabled alone:

1. IEntityFactory was registered transient and captures IServiceProvider.
2. CharacterSelectHandler resolves IEntityFactory and creates a Player which is
   stored on the session and queued for map entry after HandleMessage returns.
3. EntityFactory creates IDisposable entities from that provider and stores
   provider-backed runtime resolvers in WorldEntity and UnitEntity.
4. Disposing the packet scope disposes the newly created entity and leaves its
   later dependency lookups, summons and nested entity creation bound to a
   disposed provider.

This is a demonstrated hazard in the current source, not a claim that it was
the historical root cause on every upstream revision. The regression fixture
includes a negative control constructing the previous scope-bound EntityFactory
and verifies premature entity disposal and ObjectDisposedException from later
lookups/creation.

## Changes and ownership

- GameSession now creates and disposes one scope per HandlePacket invocation and
  resolves both the keyed message and its handler from that scope. Error and
  early-return paths remain inside the using boundary. Packed/encrypted wrapper
  recursion gets an independent nested dispatch scope.
- The stateless EntityFactory is now singleton/root-owned. Its only constructor
  dependency and only instance field is the readonly IServiceProvider. Entity
  creation and deferred runtime resolution therefore retain the same root
  ownership they had before packet scoping was enabled.
- Entity instances and mutable per-entity factories remain transient. Shared
  factories, sessions, map ownership and explicit entity disposal are not
  converted to scoped lifetimes.

The inspected auth/world handlers use transient handler registrations. Deferred
account lookup, character-list/create/delete and catalog callbacks retain POCO
message/state plus existing singleton managers/database boundaries. No direct
scoped repository/context injection into those packet handlers was found. The
entity factory was the identified provider escape on character selection.

Future asynchronous work must explicitly own its own scope or retain only
appropriately longer-lived dependencies. A packet scope ends when the synchronous
IMessageHandler.HandleMessage call returns; it does not extend automatically to
queued EventQueue callbacks.

No wire format, game policy, entitlement default or database model is changed.

## Verification

PacketDispatchLifetimeTests adds nine regression cases covering shared scoped
dependencies, distinct subsequent scopes, message/handler/dependency disposal,
read failure, both handler exception paths, absent messages/handlers, the real
ClientPackedHandler, root-owned real WorldEntity lifetime and deferred work after
dispatch. Existing GameSessionHeartbeatTests are included in the focused gate.

The initial focused run passed 13/13. After the coordinated dependency refresh,
the final expanded run passes **62/62**, including the negative control, existing
entity-create packet tests and creature-respawn packet tests. The command is:

```powershell
dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-restore -v minimal --nologo --filter 'FullyQualifiedName~PacketDispatchLifetimeTests|FullyQualifiedName~GameSessionHeartbeatTests|FullyQualifiedName~EntityCreatePacketTests|FullyQualifiedName~CreatureRespawnPacketTests'
```

Results and build/test logs are recorded in
`I:\GIT\NexusForever-assessment-report\remaining-goal\DISPATCH_LIFETIMES.md` and
`dispatch-focused-final.log`. Parent integration runs the full solution gates.

Subsequent isolated live harness verification traversed real MySQL, RabbitMQ and
the STS/auth/world listeners: one single-user login and twelve three-user samples
received ServerPlayerEnteredWorld after character selection and asynchronous map
entry. An additional single-user sample passed through the default-port loopback
forwarders. These runs exercise the long-lived Player after packet dispatch ends.
They also exposed separate lazy read-model insertion races. After their dedicated
fixes, the 6,249-test checkpoint passed 36/36 fresh six-user login/world samples
against real MySQL with no new read-model errors; historical test deadletters
were preserved and successfully replayed, leaving the owned broker queues clean.
Details and artifacts are in
`I:\GIT\NexusForever-assessment-report\remaining-goal\RUNTIME_VERIFICATION.md`.

The later I-install native session entered world 51 after fixing the isolated
complete-asset path; the user confirmed that it loaded and responded normally.
That manual session used the earlier copied checkpoint. World was subsequently
restaged to the full 6511 source checkpoint and passed a separate 28/28 fresh
Eastern/NPE synthetic gate. Later gameplay/summon interactions remain distinct
live gates; these observations are not complete gameplay-parity claims.
