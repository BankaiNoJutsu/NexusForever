using System.Reflection;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Script.Instance;
using NexusForever.Script.Instance.Raid.RedMoonTerror.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Provisional Star-Eater script: inert by default, attached to the creature rows verified by name against
    /// Creature2, and casting only the two tag-bound rows the official developer stream supports.
    /// Timings are estimates, so these tests pin the placeholder shape only.
    /// </summary>
    public class RedMoonTerrorProvisionalEncounterTests
    {
        [Fact]
        public void StarEaterAttachesToTheVerifiedCreatureRows()
        {
            ScriptFilterCreatureIdAttribute attribute = typeof(StarEaterProvisionalEncounterScript)
                .GetCustomAttribute<ScriptFilterCreatureIdAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal([72758u, 75658u], attribute.CreatureId);
        }

        [Fact]
        public void StarEaterIsDisabledByDefault()
        {
            IFactory<ISpellParameters> factory = RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _);

            Assert.False(new StarEaterProvisionalEncounterScript(factory).Enabled);
        }

        [Fact]
        public void StarEaterPlanCastsTheHookshotPullAndSupernovaInOnePhase()
        {
            Type type = typeof(StarEaterProvisionalEncounterScript).Assembly
                .GetType("NexusForever.Script.Instance.Raid.RedMoonTerror.Script.RedMoonTerrorProvisionalPlans");
            var plan = (ProvisionalEncounterPlan)type.GetMethod("StarEater").Invoke(null, null);

            Assert.Single(plan.Phases);
            Assert.Equal([85530u, 85713u], plan.Phases[0].Casts.Select(c => c.SpellId));
        }
    }
}
