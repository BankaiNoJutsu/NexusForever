using System.Reflection;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance;
using NexusForever.Script.Instance.Dungeon.ProtogamesAcademy.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Provisional Protogames Academy boss script: inert by default and attached to the creature row the repo
    /// already uses for Wrathbone. Timings are estimates, so these tests pin the placeholder shape only.
    /// </summary>
    public class ProtogamesAcademyProvisionalEncounterTests
    {
        [Theory]
        [InlineData(typeof(WrathboneProvisionalEncounterScript), 67944u)]
        public void ScriptsAttachToTheDocumentedCreatureRows(Type scriptType, uint creatureId)
        {
            ScriptFilterCreatureIdAttribute attribute = scriptType.GetCustomAttribute<ScriptFilterCreatureIdAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal([creatureId], attribute.CreatureId);
        }

        [Fact]
        public void ScriptsAreDisabledByDefault()
        {
            IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _);

            Assert.False(new WrathboneProvisionalEncounterScript(factory).Enabled);
        }

        [Fact]
        public void WrathbonePlanIsASinglePhaseWithoutHealthThresholds()
        {
            // No clip states a health threshold for Wrathbone, so the 66% and 33% catalog rows are not used.
            ProvisionalEncounterPlan plan = ProtogamesAcademyProvisionalPlansAccessor.Wrathbone();

            Assert.Single(plan.Phases);
            Assert.Null(plan.Phases[0].HealthFraction);
            Assert.Equal([77086u], plan.Phases[0].Casts.Select(c => c.SpellId).ToArray());
        }
    }

    internal static class ProtogamesAcademyProvisionalPlansAccessor
    {
        // The plans class is internal to the script assembly; reach it by reflection to keep it non-public.
        private static ProvisionalEncounterPlan Get(string name)
        {
            Type type = typeof(WrathboneProvisionalEncounterScript).Assembly
                .GetType("NexusForever.Script.Instance.Dungeon.ProtogamesAcademy.Script.ProtogamesAcademyProvisionalPlans");
            return (ProvisionalEncounterPlan)type.GetMethod(name).Invoke(null, null);
        }

        public static ProvisionalEncounterPlan Wrathbone() => Get("Wrathbone");
    }
}
