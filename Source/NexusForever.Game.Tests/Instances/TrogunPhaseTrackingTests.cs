using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth.Script;
using NexusForever.Script.Template;
using NexusForever.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Forgemaster Trogun forge phase placeholder (INSTANCE_KELVORETH_SECONDSOURCE_PACKET_20261109.md,
    /// 50% forge phase seen in four independent clips, agent-read). Disabled by default with no gameplay effect;
    /// these tests pin the placeholder, not retail behaviour.
    /// </summary>
    public class TrogunPhaseTrackingTests
    {
        [Fact]
        public void TrackingIsDisabledByDefaultAndPhaseNeverChanges()
        {
            (ForgemasterTrogunEntityScript script, RecordingDispatchProxy<ICreatureEntity> proxy) = CreateScript();

            SetHealth(proxy, 10u);
            script.OnHealthChange(null, 90u, DamageType.Physical);

            Assert.False(script.PhaseTrackingEnabled);
            Assert.Equal(TrogunPhase.Opening, script.Phase);
        }

        [Fact]
        public void EnabledTrackingMovesToTheForgePhaseAtFiftyPercentOnce()
        {
            (ForgemasterTrogunEntityScript script, RecordingDispatchProxy<ICreatureEntity> proxy) = CreateScript();
            script.PhaseTrackingEnabled = true;

            SetHealth(proxy, 51u);
            script.OnHealthChange(null, 49u, DamageType.Physical);
            Assert.Equal(TrogunPhase.Opening, script.Phase);

            SetHealth(proxy, 50u);
            script.OnHealthChange(null, 1u, DamageType.Physical);
            Assert.Equal(TrogunPhase.ForgeRetreat, script.Phase);

            SetHealth(proxy, 90u);
            script.OnHealthChange(null, 40u, DamageType.Heal);
            Assert.Equal(TrogunPhase.ForgeRetreat, script.Phase);
        }

        [Fact]
        public void InterfaceDispatchReachesTheHandlersAndLeavingCombatResets()
        {
            (ForgemasterTrogunEntityScript script, RecordingDispatchProxy<ICreatureEntity> proxy) = CreateScript();
            script.PhaseTrackingEnabled = true;
            IUnitScript unitScript = script;

            SetHealth(proxy, 40u);
            unitScript.OnHealthChange(null, 60u, DamageType.Physical);
            Assert.Equal(TrogunPhase.ForgeRetreat, script.Phase);

            unitScript.OnLeaveCombat();
            Assert.Equal(TrogunPhase.Opening, script.Phase);

            SetHealth(proxy, 100u);
            unitScript.OnHealthChange(null, 60u, DamageType.Heal);
            Assert.Equal(TrogunPhase.Opening, script.Phase);
        }

        private static void SetHealth(RecordingDispatchProxy<ICreatureEntity> proxy, uint health)
        {
            proxy.SetProperty(nameof(IUnitEntity.Health), health);
        }

        private static (ForgemasterTrogunEntityScript, RecordingDispatchProxy<ICreatureEntity>) CreateScript()
        {
            ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> proxy);
            proxy.SetProperty(nameof(IUnitEntity.Health), 100u);
            proxy.SetProperty(nameof(IUnitEntity.MaxHealth), 100u);

            var script = new ForgemasterTrogunEntityScript(
                RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
                RecordingDispatchProxy<IGameTableManager>.Create(out _));
            script.OnLoad(owner);
            return (script, proxy);
        }
    }
}
