using System.Reflection;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance;
using NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Provisional Sanctuary of the Swordmaiden boss scripts: inert by default and attached to the creature rows
    /// the repo already uses for the same bosses. Timings are estimates, so these tests pin the placeholder shape only.
    /// </summary>
    public class SanctuaryProvisionalEncounterTests
    {
        [Theory]
        [InlineData(typeof(OnduProvisionalEncounterScript), 28720u, 28721u)]
        [InlineData(typeof(SkashProvisionalEncounterScript), 28728u, 28727u)]
        [InlineData(typeof(RaynaProvisionalEncounterScript), 28733u, 28732u)]
        [InlineData(typeof(SeleneProvisionalEncounterScript), 28736u, 28735u)]
        public void ScriptsAttachToTheDocumentedCreatureRows(Type scriptType, uint firstId, uint secondId)
        {
            ScriptFilterCreatureIdAttribute attribute = scriptType.GetCustomAttribute<ScriptFilterCreatureIdAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal([firstId, secondId], attribute.CreatureId);
        }

        [Fact]
        public void ScriptsAreDisabledByDefault()
        {
            IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _);

            Assert.False(new OnduProvisionalEncounterScript(factory).Enabled);
            Assert.False(new SkashProvisionalEncounterScript(factory).Enabled);
            Assert.False(new RaynaProvisionalEncounterScript(factory).Enabled);
            Assert.False(new SeleneProvisionalEncounterScript(factory).Enabled);
        }

        [Fact]
        public void EveryPlanIsASinglePhaseWithoutHealthThresholds()
        {
            // No clip states a health threshold for any Sanctuary boss, so none is used.
            ProvisionalEncounterPlan[] plans =
            [
                SanctuaryProvisionalPlansAccessor.Ondu(), SanctuaryProvisionalPlansAccessor.Skash(),
                SanctuaryProvisionalPlansAccessor.Rayna(), SanctuaryProvisionalPlansAccessor.Selene()
            ];

            Assert.All(plans, plan => Assert.Single(plan.Phases));
            Assert.All(plans, plan => Assert.InRange(plan.Phases[0].Casts.Count, 1, 3));
        }

        [Fact]
        public void PlansCastTheDocumentedSpellRows()
        {
            Assert.Equal([43253u, 42526u], SpellIds(SanctuaryProvisionalPlansAccessor.Ondu()));
            Assert.Equal([42135u], SpellIds(SanctuaryProvisionalPlansAccessor.Skash()));
            Assert.Equal([42171u, 42418u, 42264u], SpellIds(SanctuaryProvisionalPlansAccessor.Rayna()));
            Assert.Equal([42880u, 42757u, 42386u], SpellIds(SanctuaryProvisionalPlansAccessor.Selene()));
        }

        private static uint[] SpellIds(ProvisionalEncounterPlan plan)
        {
            return plan.Phases[0].Casts.Select(c => c.SpellId).ToArray();
        }
    }

    internal static class SanctuaryProvisionalPlansAccessor
    {
        // The plans class is internal to the script assembly; reach it by reflection to keep it non-public.
        private static ProvisionalEncounterPlan Get(string name)
        {
            Type type = typeof(OnduProvisionalEncounterScript).Assembly
                .GetType("NexusForever.Script.Instance.Dungeon.SanctuaryOfTheSwordmaiden.Script.SanctuaryProvisionalPlans");
            return (ProvisionalEncounterPlan)type.GetMethod(name).Invoke(null, null);
        }

        public static ProvisionalEncounterPlan Ondu() => Get("Ondu");
        public static ProvisionalEncounterPlan Skash() => Get("Skash");
        public static ProvisionalEncounterPlan Rayna() => Get("Rayna");
        public static ProvisionalEncounterPlan Selene() => Get("Selene");
    }
}
