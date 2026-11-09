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
    /// Boss-independent tests for the provisional encounter entity script: it casts the runner's due spells
    /// through the owner and is inert unless enabled. Plan values are estimates, not retail values.
    /// </summary>
    public class ProvisionalEncounterEntityScriptTests
    {
        private sealed class TestScript : ProvisionalEncounterEntityScript
        {
            public TestScript(IFactory<ISpellParameters> factory)
                : base(factory, new ProvisionalEncounterPlan(
                [
                    new ProvisionalPhase(null,
                    [
                        new ProvisionalCast(1001u, 2d, 10d),
                        new ProvisionalCast(1002u, 2d, 10d, targetCurrentTarget: true)
                    ], "test")
                ]))
            {
            }
        }

        [Fact]
        public void ScriptIsInertByDefault()
        {
            (TestScript script, RecordingDispatchProxy<ICreatureEntity> owner, _) = Create();

            script.Update(100d);

            Assert.False(script.Enabled);
            Assert.Empty(owner.GetInvocations(nameof(IUnitEntity.CastSpell)));
        }

        [Fact]
        public void EnabledScriptCastsDueSpellsInCombat()
        {
            (TestScript script, RecordingDispatchProxy<ICreatureEntity> owner, _) = Create();
            script.Enabled = true;

            script.Update(1d);
            Assert.Empty(owner.GetInvocations(nameof(IUnitEntity.CastSpell)));

            script.Update(1.5d);
            var casts = owner.GetInvocations(nameof(IUnitEntity.CastSpell));
            Assert.Equal([1001u, 1002u], casts.Select(c => (uint)c.Arguments[0]));
        }

        [Fact]
        public void TargetedCastCopiesTheCurrentTargetIntoTheSpellParameters()
        {
            (TestScript script, _, RecordingDispatchProxy<ISpellParameters> parameters) = Create(targetGuid: 77u);
            script.Enabled = true;

            script.Update(3d);

            var sets = parameters.GetInvocations("set_" + nameof(ISpellParameters.PrimaryTargetId));
            Assert.Single(sets);
            Assert.Equal(77u, (uint)sets[0].Arguments[0]);
        }

        [Fact]
        public void NothingCastsOutOfCombatAndLeavingCombatResetsTheSchedule()
        {
            (TestScript script, RecordingDispatchProxy<ICreatureEntity> owner, _) = Create(inCombat: false);
            script.Enabled = true;

            script.Update(50d);
            Assert.Empty(owner.GetInvocations(nameof(IUnitEntity.CastSpell)));

            owner.SetProperty(nameof(IUnitEntity.InCombat), true);
            script.Update(3d);
            Assert.Equal(2, owner.GetInvocations(nameof(IUnitEntity.CastSpell)).Count);

            ((IUnitScript)script).OnLeaveCombat();
            script.Update(1d);
            Assert.Equal(2, owner.GetInvocations(nameof(IUnitEntity.CastSpell)).Count);

            script.Update(1.5d);
            Assert.Equal(4, owner.GetInvocations(nameof(IUnitEntity.CastSpell)).Count);
        }

        private static (TestScript, RecordingDispatchProxy<ICreatureEntity>, RecordingDispatchProxy<ISpellParameters>) Create(
            bool inCombat = true, uint? targetGuid = null)
        {
            ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy);
            IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
            factoryProxy.SetMethodReturnFactory(nameof(IFactory<ISpellParameters>.Resolve), () => parameters);

            ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> ownerProxy);
            ownerProxy.SetProperty(nameof(IUnitEntity.Health), 100u);
            ownerProxy.SetProperty(nameof(IUnitEntity.MaxHealth), 100u);
            ownerProxy.SetProperty(nameof(IUnitEntity.InCombat), inCombat);
            ownerProxy.SetProperty(nameof(IUnitEntity.TargetGuid), targetGuid);

            var script = new TestScript(factory);
            script.OnLoad(owner);
            return (script, ownerProxy, parametersProxy);
        }
    }
}
