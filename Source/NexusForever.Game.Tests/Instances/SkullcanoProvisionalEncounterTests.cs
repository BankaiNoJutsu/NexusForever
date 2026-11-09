using System.Reflection;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance;
using NexusForever.Script.Instance.Dungeon.Skullcano.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Provisional Skullcano boss scripts: inert by default and attached to the creature rows the repo already
    /// uses for the same bosses. Timings are estimates, so these tests pin the placeholder shape only.
    /// </summary>
    public class SkullcanoProvisionalEncounterTests
    {
        [Theory]
        [InlineData(typeof(TuggaProvisionalEncounterScript), 24493u, 24898u)]
        [InlineData(typeof(ThunderfootProvisionalEncounterScript), 24475u, 24893u)]
        [InlineData(typeof(OctogProvisionalEncounterScript), 24486u, 24894u)]
        [InlineData(typeof(MordechaiProvisionalEncounterScript), 24489u, 24895u)]
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

            Assert.False(new TuggaProvisionalEncounterScript(factory).Enabled);
            Assert.False(new ThunderfootProvisionalEncounterScript(factory).Enabled);
            Assert.False(new OctogProvisionalEncounterScript(factory).Enabled);
            Assert.False(new MordechaiProvisionalEncounterScript(factory).Enabled);
        }

        [Fact]
        public void EveryPlanIsASinglePhaseWithoutHealthThresholds()
        {
            // Mordechai's 33% and 10% are single-source and are deliberately not used.
            ProvisionalEncounterPlan[] plans =
            [
                SkullcanoProvisionalPlansAccessor.Tugga(), SkullcanoProvisionalPlansAccessor.Thunderfoot(),
                SkullcanoProvisionalPlansAccessor.Octog(), SkullcanoProvisionalPlansAccessor.Mordechai()
            ];

            Assert.All(plans, plan => Assert.Single(plan.Phases));
        }
    }

    /// <summary>
    /// Gruh'ar was added after the Jabbithole check: Kneecap on both rows, Fatal Shot only on the second (word 9 = 5) row.
    /// </summary>
    public class GruharProvisionalEncounterTests
    {
        [Theory]
        [InlineData(typeof(GruharProvisionalEncounterScript), 24490u)]
        [InlineData(typeof(GruharVeteranProvisionalEncounterScript), 24896u)]
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

            Assert.False(new GruharProvisionalEncounterScript(factory).Enabled);
            Assert.False(new GruharVeteranProvisionalEncounterScript(factory).Enabled);
        }

        [Fact]
        public void FatalShotIsOnlyInTheSecondRowPlan()
        {
            Type type = typeof(TuggaProvisionalEncounterScript).Assembly
                .GetType("NexusForever.Script.Instance.Dungeon.Skullcano.Script.SkullcanoProvisionalPlans");
            var low = (ProvisionalEncounterPlan)type.GetMethod("Gruhar").Invoke(null, [false]);
            var high = (ProvisionalEncounterPlan)type.GetMethod("Gruhar").Invoke(null, [true]);

            Assert.Equal([41901u], low.Phases[0].Casts.Select(c => c.SpellId));
            Assert.Equal([41901u, 63677u], high.Phases[0].Casts.Select(c => c.SpellId));
        }
    }

    internal static class SkullcanoProvisionalPlansAccessor
    {
        // The plans class is internal to the script assembly; reach it by reflection to keep it non-public.
        private static ProvisionalEncounterPlan Get(string name)
        {
            Type type = typeof(TuggaProvisionalEncounterScript).Assembly
                .GetType("NexusForever.Script.Instance.Dungeon.Skullcano.Script.SkullcanoProvisionalPlans");
            return (ProvisionalEncounterPlan)type.GetMethod(name).Invoke(null, null);
        }

        public static ProvisionalEncounterPlan Tugga() => Get("Tugga");
        public static ProvisionalEncounterPlan Thunderfoot() => Get("Thunderfoot");
        public static ProvisionalEncounterPlan Octog() => Get("Octog");
        public static ProvisionalEncounterPlan Mordechai() => Get("Mordechai");
    }
}
