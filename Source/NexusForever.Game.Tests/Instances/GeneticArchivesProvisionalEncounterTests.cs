using System.Reflection;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance;
using NexusForever.Script.Instance.Raid.GeneticArchives.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Provisional Genetic Archives boss scripts: inert by default and attached to the creature rows the repo
    /// already spawns or scripts for the same bosses. Timings are estimates, so these tests pin the placeholder shape only.
    /// </summary>
    public class GeneticArchivesProvisionalEncounterTests
    {
        [Theory]
        [InlineData(typeof(ExperimentX89ProvisionalEncounterScript), 49198u)]
        [InlineData(typeof(KuralakTheDefilerProvisionalEncounterScript), 52969u)]
        [InlineData(typeof(PhageMawProvisionalEncounterScript), 52974u)]
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

            Assert.False(new ExperimentX89ProvisionalEncounterScript(factory).Enabled);
            Assert.False(new KuralakTheDefilerProvisionalEncounterScript(factory).Enabled);
            Assert.False(new PhageMawProvisionalEncounterScript(factory).Enabled);
        }

        [Fact]
        public void OnlyKuralakHasAHealthPhaseAndItIsTheTwoClipEggPhase()
        {
            ProvisionalEncounterPlan x89 = GeneticArchivesProvisionalPlansAccessor.ExperimentX89();
            ProvisionalEncounterPlan kuralak = GeneticArchivesProvisionalPlansAccessor.Kuralak();
            ProvisionalEncounterPlan phageMaw = GeneticArchivesProvisionalPlansAccessor.PhageMaw();

            Assert.Single(x89.Phases);
            Assert.Single(phageMaw.Phases);
            Assert.Equal(2, kuralak.Phases.Count);
            Assert.Equal(0.72f, kuralak.Phases[1].HealthFraction);
            Assert.All(new[] { x89, kuralak, phageMaw }, plan => Assert.All(plan.Phases, phase => Assert.InRange(phase.Casts.Count, 1, 3)));
        }
    }

    internal static class GeneticArchivesProvisionalPlansAccessor
    {
        // The plans class is internal to the script assembly; reach it by reflection to keep it non-public.
        private static ProvisionalEncounterPlan Get(string name)
        {
            Type type = typeof(ExperimentX89ProvisionalEncounterScript).Assembly
                .GetType("NexusForever.Script.Instance.Raid.GeneticArchives.Script.GeneticArchivesProvisionalPlans");
            return (ProvisionalEncounterPlan)type.GetMethod(name).Invoke(null, null);
        }

        public static ProvisionalEncounterPlan ExperimentX89() => Get("ExperimentX89");
        public static ProvisionalEncounterPlan Kuralak() => Get("Kuralak");
        public static ProvisionalEncounterPlan PhageMaw() => Get("PhageMaw");
    }
}
