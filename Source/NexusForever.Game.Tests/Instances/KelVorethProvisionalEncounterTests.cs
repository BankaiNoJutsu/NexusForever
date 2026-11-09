using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance;
using NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Provisional Kel Voreth boss scripts: inert by default, attached to creature ids verified by name
    /// against Creature2, and using only tag-bound spell rows. Timings are estimates.
    /// </summary>
    public class KelVorethProvisionalEncounterTests
    {
        [Theory]
        [InlineData(typeof(TrogunProvisionalEncounterScript), 32531u, 32533u)]
        [InlineData(typeof(GrondProvisionalEncounterScript), 32534u, 32535u)]
        [InlineData(typeof(DrokkProvisionalEncounterScript), 32536u, 32539u)]
        public void ScriptsAttachToTheVerifiedCreatureRows(Type scriptType, uint normalId, uint veteranId)
        {
            ScriptFilterCreatureIdAttribute attribute = scriptType.GetCustomAttribute<ScriptFilterCreatureIdAttribute>();

            Assert.NotNull(attribute);
            Assert.Contains(normalId, attribute.CreatureId);
            Assert.Contains(veteranId, attribute.CreatureId);
        }

        [Fact]
        public void ScriptsAreDisabledByDefault()
        {
            IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _);

            Assert.False(new TrogunProvisionalEncounterScript(factory).Enabled);
            Assert.False(new GrondProvisionalEncounterScript(factory).Enabled);
            Assert.False(new DrokkProvisionalEncounterScript(factory).Enabled);
        }

        [Fact]
        public void TrogunUsesTheAgreedFiftyPercentPhaseAndTargetsTheTankWithVolcanicStrike()
        {
            ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> proxy);
            proxy.SetProperty(nameof(IUnitEntity.Health), 100u);
            proxy.SetProperty(nameof(IUnitEntity.MaxHealth), 100u);
            proxy.SetProperty(nameof(IUnitEntity.InCombat), true);
            proxy.SetProperty(nameof(IUnitEntity.TargetGuid), (uint?)9u);
            ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out RecordingDispatchProxy<ISpellParameters> parametersProxy);
            IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
            factoryProxy.SetMethodReturnFactory(nameof(IFactory<ISpellParameters>.Resolve), () => parameters);

            var script = new TrogunProvisionalEncounterScript(factory) { Enabled = true };
            script.OnLoad(owner);

            script.Update(12d);
            Assert.Equal([52059u], proxy.GetInvocations(nameof(IUnitEntity.CastSpell)).Select(c => (uint)c.Arguments[0]));
            Assert.Single(parametersProxy.GetInvocations("set_" + nameof(ISpellParameters.PrimaryTargetId)));

            proxy.SetProperty(nameof(IUnitEntity.Health), 50u);
            script.Update(1d);

            Assert.Contains(38882u, proxy.GetInvocations(nameof(IUnitEntity.CastSpell)).Select(c => (uint)c.Arguments[0]));
        }
    }
}
