using System.Reflection;
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
    /// Darkwitch Gurka was added after the Jabbithole check: Deadly Defilement and Blinding Dark, with the row set chosen per
    /// Creature2 row (word 9 = 1 lower set, word 9 = 5 veteran set; that pairing is a guess). Timings are estimates.
    /// </summary>
    public class GurkaProvisionalEncounterTests
    {
        [Theory]
        [InlineData(typeof(GurkaProvisionalEncounterScript), 33049u)]
        [InlineData(typeof(GurkaVeteranProvisionalEncounterScript), 33050u)]
        public void ScriptsAttachToTheVerifiedCreatureRows(Type scriptType, uint creatureId)
        {
            ScriptFilterCreatureIdAttribute attribute = scriptType.GetCustomAttribute<ScriptFilterCreatureIdAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal([creatureId], attribute.CreatureId);
        }

        [Fact]
        public void ScriptsAreDisabledByDefault()
        {
            IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _);

            Assert.False(new GurkaProvisionalEncounterScript(factory).Enabled);
            Assert.False(new GurkaVeteranProvisionalEncounterScript(factory).Enabled);
        }

        [Fact]
        public void EachRowSetCastsItsOwnDeadlyDefilementAndBlindingDark()
        {
            Type type = typeof(GurkaProvisionalEncounterScript).Assembly
                .GetType("NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script.KelVorethProvisionalPlans");
            var low = (ProvisionalEncounterPlan)type.GetMethod("Gurka").Invoke(null, [false]);
            var high = (ProvisionalEncounterPlan)type.GetMethod("Gurka").Invoke(null, [true]);

            Assert.Equal([44304u, 44315u], low.Phases[0].Casts.Select(c => c.SpellId));
            Assert.Equal([63510u, 63508u], high.Phases[0].Casts.Select(c => c.SpellId));
        }
    }
}
