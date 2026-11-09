using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance;
using NexusForever.Script.Template;
using NexusForever.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Boss-independent tests for the wipe reset facility: when an owner leaves combat alive, tracked adds are removed,
    /// registered restores run and held invulnerable states are released; a dead owner (a kill) is not reset.
    /// </summary>
    public class EncounterWipeResetTests
    {
        private static IUnitEntity CreateOwner(out RecordingDispatchProxy<IUnitEntity> proxy, bool alive = true)
        {
            IUnitEntity owner = RecordingDispatchProxy<IUnitEntity>.Create(out proxy);
            proxy.SetProperty(nameof(IUnitEntity.IsAlive), alive);
            proxy.SetMethodHandler(nameof(IUnitEntity.RemoveUnitState), _ => true);
            return owner;
        }

        [Fact]
        public void WipeRemovesTrackedAddsAndRunsRestoresAndReleasesInvulnerability()
        {
            IUnitEntity owner = CreateOwner(out RecordingDispatchProxy<IUnitEntity> ownerProxy);
            IGridEntity add = RecordingDispatchProxy<IGridEntity>.Create(out RecordingDispatchProxy<IGridEntity> addProxy);
            int doorClosed = 0;

            EncounterWipeReset.TrackAdd(owner, add);
            EncounterWipeReset.RegisterRestore(owner, "door", () => doorClosed++);
            EncounterInvulnerability.Apply(owner, 910001u);

            int undone = EncounterWipeReset.Run(owner);

            Assert.Equal(3, undone);
            Assert.Single(addProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
            Assert.Equal(1, doorClosed);
            Assert.Single(ownerProxy.GetInvocations(nameof(IUnitEntity.RemoveUnitState)));
        }

        [Fact]
        public void KillIsNotAWipe()
        {
            IUnitEntity owner = CreateOwner(out _, alive: false);
            IGridEntity add = RecordingDispatchProxy<IGridEntity>.Create(out RecordingDispatchProxy<IGridEntity> addProxy);
            int restored = 0;

            EncounterWipeReset.TrackAdd(owner, add);
            EncounterWipeReset.RegisterRestore(owner, "door", () => restored++);

            Assert.Equal(0, EncounterWipeReset.Run(owner));
            Assert.Empty(addProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
            Assert.Equal(0, restored);
        }

        [Fact]
        public void ResetIsOneShotAndSameKeyReplacesTheEarlierRestore()
        {
            IUnitEntity owner = CreateOwner(out _);
            int first = 0;
            int second = 0;

            EncounterWipeReset.RegisterRestore(owner, "platform", () => first++);
            EncounterWipeReset.RegisterRestore(owner, "platform", () => second++);

            Assert.Equal(1, EncounterWipeReset.Run(owner));
            Assert.Equal(0, first);
            Assert.Equal(1, second);

            Assert.Equal(0, EncounterWipeReset.Run(owner));
            Assert.Equal(1, second);
        }

        [Fact]
        public void FailingRestoreDoesNotStopTheOthers()
        {
            IUnitEntity owner = CreateOwner(out _);
            int ran = 0;

            EncounterWipeReset.RegisterRestore(owner, "bad", () => throw new InvalidOperationException());
            EncounterWipeReset.RegisterRestore(owner, "good", () => ran++);

            EncounterWipeReset.Run(owner);

            Assert.Equal(1, ran);
        }

        [Fact]
        public void TrackingTheSameAddTwiceRemovesItOnce()
        {
            IUnitEntity owner = CreateOwner(out _);
            IGridEntity add = RecordingDispatchProxy<IGridEntity>.Create(out RecordingDispatchProxy<IGridEntity> addProxy);

            EncounterWipeReset.TrackAdd(owner, add);
            EncounterWipeReset.TrackAdd(owner, add);
            EncounterWipeReset.Run(owner);

            Assert.Single(addProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        }

        [Fact]
        public void NullArgumentsAreRejected()
        {
            IUnitEntity owner = CreateOwner(out _);

            Assert.Throws<ArgumentNullException>(() => EncounterWipeReset.Run(null));
            Assert.Throws<ArgumentNullException>(() => EncounterWipeReset.TrackAdd(null, null));
            Assert.Throws<ArgumentNullException>(() => EncounterWipeReset.RegisterRestore(owner, "k", null));
        }

        private sealed class WipeScript : ProvisionalEncounterEntityScript
        {
            public WipeScript(IFactory<ISpellParameters> factory)
                : base(factory, new ProvisionalEncounterPlan([new ProvisionalPhase(null, [new ProvisionalCast(1u, 1d, 0d)], "test")]))
            {
            }
        }

        [Fact]
        public void ProvisionalScriptLeavingCombatAliveRunsTheWipeReset()
        {
            IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _);
            ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
            ownerProxy.SetProperty(nameof(IUnitEntity.IsAlive), true);
            IGridEntity add = RecordingDispatchProxy<IGridEntity>.Create(out RecordingDispatchProxy<IGridEntity> addProxy);

            var script = new WipeScript(factory);
            script.OnLoad(owner);
            EncounterWipeReset.TrackAdd(owner, add);

            ((IUnitScript)script).OnLeaveCombat();

            Assert.Single(addProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        }
    }
}
