using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance;
using NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Provisional Stormtalon's Lair boss scripts: inert by default, attached to the right creature ids,
    /// and casting only the tag-bound spell rows. Timings are estimates, so these tests pin the placeholder
    /// shape and never claim retail behaviour.
    /// </summary>
    public class StormtalonProvisionalEncounterTests
    {
        [Theory]
        [InlineData(typeof(BladeWindProvisionalEncounterScript), 17160u, 33405u)]
        [InlineData(typeof(AethrosProvisionalEncounterScript), 17166u, 32703u)]
        [InlineData(typeof(StormtalonProvisionalEncounterScript), 17163u, 33406u)]
        public void ScriptsAttachToTheDocumentedCreatureRows(Type scriptType, uint normalId, uint veteranId)
        {
            ScriptFilterCreatureIdAttribute attribute = scriptType.GetCustomAttribute<ScriptFilterCreatureIdAttribute>();

            Assert.NotNull(attribute);
            Assert.Contains(normalId, attribute.CreatureId);
            Assert.Contains(veteranId, attribute.CreatureId);
        }

        [Fact]
        public void ScriptsAreDisabledByDefaultAndDoNotCredit()
        {
            IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _);

            foreach (ProvisionalEncounterEntityScript script in new ProvisionalEncounterEntityScript[]
                     {
                         new BladeWindProvisionalEncounterScript(factory),
                         new AethrosProvisionalEncounterScript(factory),
                         new StormtalonProvisionalEncounterScript(factory)
                     })
            {
                Assert.False(script.Enabled);
                Assert.IsNotAssignableFrom<PublicEventObjectiveCreditEntityScript>(script);
            }
        }

        [Fact]
        public void BladeWindCastsOnlyThunderCrossBeforePhaseTwoAndAddsStrikesAfter()
        {
            ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> proxy);
            proxy.SetProperty(nameof(IUnitEntity.Health), 100u);
            proxy.SetProperty(nameof(IUnitEntity.MaxHealth), 100u);
            proxy.SetProperty(nameof(IUnitEntity.InCombat), true);
            ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out _);
            IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out RecordingDispatchProxy<IFactory<ISpellParameters>> factoryProxy);
            factoryProxy.SetMethodReturnFactory(nameof(IFactory<ISpellParameters>.Resolve), () => parameters);

            var script = new BladeWindProvisionalEncounterScript(factory) { Enabled = true };
            script.OnLoad(owner);

            script.Update(15d);
            Assert.Equal([38868u], proxy.GetInvocations(nameof(IUnitEntity.CastSpell)).Select(c => (uint)c.Arguments[0]));

            proxy.SetProperty(nameof(IUnitEntity.Health), 60u);
            script.Update(6d);

            Assert.Contains(38836u, proxy.GetInvocations(nameof(IUnitEntity.CastSpell)).Select(c => (uint)c.Arguments[0]));
        }
    }
}
