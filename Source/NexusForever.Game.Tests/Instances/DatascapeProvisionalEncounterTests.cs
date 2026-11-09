using System.Reflection;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance;
using NexusForever.Script.Instance.Raid.Datascape.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Provisional Datascape boss scripts: inert by default and attached to the creature rows verified by
    /// name against Creature2. Timings are estimates, so these tests pin the placeholder shape only.
    /// </summary>
    public class DatascapeProvisionalEncounterTests
    {
        [Theory]
        [InlineData(typeof(SystemDaemonsProvisionalEncounterScript), new[] { 30495u, 30496u })]
        [InlineData(typeof(MaelstromAuthorityProvisionalEncounterScript), new[] { 30497u })]
        [InlineData(typeof(AvatusProvisionalEncounterScript), new[] { 30505u, 60057u, 60058u })]
        public void ScriptsAttachToTheDocumentedCreatureRows(Type scriptType, uint[] creatureIds)
        {
            ScriptFilterCreatureIdAttribute attribute = scriptType.GetCustomAttribute<ScriptFilterCreatureIdAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal(creatureIds, attribute.CreatureId);
        }

        [Fact]
        public void ScriptsAreDisabledByDefault()
        {
            IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _);

            Assert.False(new SystemDaemonsProvisionalEncounterScript(factory).Enabled);
            Assert.False(new MaelstromAuthorityProvisionalEncounterScript(factory).Enabled);
            Assert.False(new AvatusProvisionalEncounterScript(factory).Enabled);
        }

        [Fact]
        public void EveryPlanIsASinglePhaseWithoutHealthThresholds()
        {
            // The System Daemons 70% and 30% pillar phases are single-source and are deliberately not used.
            ProvisionalEncounterPlan[] plans =
            [
                DatascapeProvisionalPlansAccessor.SystemDaemons(), DatascapeProvisionalPlansAccessor.MaelstromAuthority(),
                DatascapeProvisionalPlansAccessor.Avatus()
            ];

            Assert.All(plans, plan => Assert.Single(plan.Phases));
            Assert.All(plans, plan => Assert.InRange(plan.Phases[0].Casts.Count, 1, 3));
        }
    }

    internal static class DatascapeProvisionalPlansAccessor
    {
        // The plans class is internal to the script assembly; reach it by reflection to keep it non-public.
        private static ProvisionalEncounterPlan Get(string name)
        {
            Type type = typeof(SystemDaemonsProvisionalEncounterScript).Assembly
                .GetType("NexusForever.Script.Instance.Raid.Datascape.Script.DatascapeProvisionalPlans");
            return (ProvisionalEncounterPlan)type.GetMethod(name).Invoke(null, null);
        }

        public static ProvisionalEncounterPlan SystemDaemons() => Get("SystemDaemons");
        public static ProvisionalEncounterPlan MaelstromAuthority() => Get("MaelstromAuthority");
        public static ProvisionalEncounterPlan Avatus() => Get("Avatus");
    }
}
