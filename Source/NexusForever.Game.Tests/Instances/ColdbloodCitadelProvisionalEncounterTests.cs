using System.Reflection;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance;
using NexusForever.Script.Instance.Dungeon.ColdbloodCitadel.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Provisional Coldblood Citadel boss scripts: inert by default and attached to the creature rows the repo
    /// already uses for the same bosses. The abilities rest on one developer clip and the timings are estimates,
    /// so these tests pin the placeholder shape only.
    /// </summary>
    public class ColdbloodCitadelProvisionalEncounterTests
    {
        [Theory]
        [InlineData(typeof(HailstoneGatecrasherProvisionalEncounterScript), 75508u)]
        [InlineData(typeof(UlfridProvisionalEncounterScript), 75474u)]
        [InlineData(typeof(KatlaProvisionalEncounterScript), 75473u)]
        [InlineData(typeof(GolagProvisionalEncounterScript), 75472u)]
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

            Assert.False(new HailstoneGatecrasherProvisionalEncounterScript(factory).Enabled);
            Assert.False(new UlfridProvisionalEncounterScript(factory).Enabled);
            Assert.False(new KatlaProvisionalEncounterScript(factory).Enabled);
            Assert.False(new GolagProvisionalEncounterScript(factory).Enabled);
        }

        [Fact]
        public void EveryPlanIsASinglePhaseWithoutHealthThresholds()
        {
            // The packet states no health thresholds, so none are used.
            ProvisionalEncounterPlan[] plans =
            [
                ColdbloodCitadelProvisionalPlansAccessor.HailstoneGatecrasher(), ColdbloodCitadelProvisionalPlansAccessor.Ulfrid(),
                ColdbloodCitadelProvisionalPlansAccessor.Katla(), ColdbloodCitadelProvisionalPlansAccessor.Golag()
            ];

            Assert.All(plans, plan => Assert.Single(plan.Phases));
            Assert.All(plans, plan => Assert.InRange(plan.Phases[0].Casts.Count, 1, 3));
        }
    }

    internal static class ColdbloodCitadelProvisionalPlansAccessor
    {
        // The plans class is internal to the script assembly; reach it by reflection to keep it non-public.
        private static ProvisionalEncounterPlan Get(string name)
        {
            Type type = typeof(HailstoneGatecrasherProvisionalEncounterScript).Assembly
                .GetType("NexusForever.Script.Instance.Dungeon.ColdbloodCitadel.Script.ColdbloodCitadelProvisionalPlans");
            return (ProvisionalEncounterPlan)type.GetMethod(name).Invoke(null, null);
        }

        public static ProvisionalEncounterPlan HailstoneGatecrasher() => Get("HailstoneGatecrasher");
        public static ProvisionalEncounterPlan Ulfrid() => Get("Ulfrid");
        public static ProvisionalEncounterPlan Katla() => Get("Katla");
        public static ProvisionalEncounterPlan Golag() => Get("Golag");
    }
}
