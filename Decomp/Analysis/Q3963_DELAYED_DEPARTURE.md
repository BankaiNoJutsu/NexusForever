# Q3963 delayed departure ownership

6 November 2026. The Q3963 departure script now retains the original quest,
Player, character/realm, account/session epoch, event queue, cinematic manager,
map and admission generation through delayed transport. A replaced or unloaded
owner cannot move the old player. A queued departure executes once, and a
pre-queue cinematic refusal releases its own marker for another valid attempt.

This extends [CSI activation ownership](CSI_ACTIVATION_OWNERSHIP.md) into the
existing departure script and actual Player teleport admission. It is an
implemented Source boundary; the complete Q3963 journey and restoration Goal
remain unfinished.

## Reproduced source failures

The previous script keyed pending work only by CharacterId. Its delayed closure
captured Player but checked only the world number and CanTeleport. It removed
the marker before acting, so manually replaying the queued callback could
teleport twice. Its post-cinematic enqueue read Player.Session again, and a
cinematic refusal left the marker permanently occupied.

Eleven of12 baseline cases fail on parent387b7830; the original route control
passes. The failing cases cover changed session/queue/bound Player, same-world
map replacement, CharacterId changes, pending deletion/script reload, duplicate
callback, cinematic refusal, queue replacement during cinematic work and actor
replacement during the final teleport check.

A later executed review negative shows the first guarded candidate still acts
after IScript.OnUnload. Unload now retires pending work and clears the owner;
later state callbacks are ignored. Logging uses retained quest identity so
unload during admitted teleport does not dereference the retired owner.

## Owning changes

The character-keyed set is replaced by one concrete departure for this owned
quest script. Its snapshot and ticket remain current across preparation,
cinematic creation/queueing and delayed work. New load, unload or a state outside
Achieved/Completed invalidates it. An old callback's cleanup cannot remove a
later owner's departure. A failed preparation retires only its own marker and
preserves the exception.

The final ownership check, rotation and real Player.TeleportTo run under the
existing optional map-admission gate. Admission is rechecked around fallible
callbacks; original-player/session binding and account ownership are retained.
The queued callback is consumed once even if execution fails. The original
26,500ms delay, destination lookup, rotation conversion and Relocate reason
remain. This does not infer a new native timing or replay policy.

## Integrated qualification

All22 new cases and145 selected related cases pass, with no skips.
Four new cases connect both parsed CSI completion routes through real Player,
QuestManager, Quest state change, production script callback and delayed
departure. The real Player.TeleportTo reaches IMapManager.AddToMap once with
the original actor and expected destination, establishing pending transfer.
The fixture map manager begins admission and unloads the script during that
handoff; it does not perform actual map bootstrap or physical travel.

The two positive routes also exercise owned full Character save refusal, retry
and fresh Quest reload. Achieved objective5201 survives; duplicate departure
callbacks cannot request another transfer. The two negative routes begin a
same-map admission before departure and make no AddToMap call. Rotation asserts
that the admission gate is held. Existing quest/save and CSI controls are
included in the selected scope.

Queued callbacks are explicitly invoked in these tests. They do not wait for or
qualify real-time/native timer cadence. The pre-queue refusal fixture permits a
repeated state callback; no automatic relog/restart departure recovery is added.
Accepted quest, WorldSession, entity/map manager and cinematic collaborators
are prepared; actual cinematic playback and public quest acceptance/turn-in
are separate.

One existing left-map test was corrected to begin in426 and change to51 after
scheduling. Previously it started in51 despite its lifecycle name. Initial
xUnit2031 fixture feedback was fixed using Assert.Single's predicate overload;
the final test graph has only the existing ASPIRE010 warning.

The incremental owning Script.Main build has zero warnings/errors. No full
solution/full suite, services, native client or Computer Use ran.
Primary-agent consolidated spec and standards self-reviews have no remaining
actionable findings within this boundary. The unload finding was executed and
corrected; arbitrary partial script/queue failure recovery is not claimed.

## Recovered route and portability

The existing owning GameTable reader checked WorldLocation2.tbl and World.tbl
against the retained cache manifest. WorldLocation9801 resolves to world51 at
(3832.376, -1001.4483, -4497.1353), with quaternion
(-1.0037917E-12, -0.22710016, 4.3046105E-12, 0.9738714).
The new integrated fixture uses these full-precision fields. Older rounded
fixture controls keep their historical input boundary; production continues
to read the table rather than hardcoding these coordinates.

The [portable record](coverage/q3963-departure-20261106.json) binds changed
Source, owning/test DLLs, scoped outcomes and route inputs. Raw rows, local
audit/runner, baseline and review-negative logs/TRX are under
artifacts/q3963-departure and remain outside Git.

Full departure/cinematic/native timing, actual map bootstrap/location settlement,
public acceptance/turn-in, crash/relog recovery, configured provider/socket and
retail qualification remain open. Generated retail-completeness gates are
unchanged. Continue the broader canonical gameplay/assessment queue; native
client and Computer Use remain stopped.
