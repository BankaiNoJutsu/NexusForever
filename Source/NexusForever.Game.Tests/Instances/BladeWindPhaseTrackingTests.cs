using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Script.Instance.Dungeon.StormtalonsLair.Script;
using NexusForever.Script.Template;
using NexusForever.Shared;
using Xunit;

namespace NexusForever.Game.Tests.Instances
{
    /// <summary>
    /// Blade-Wind the Invoker phase tracking placeholder (INSTANCE_STORMTALON_BLADEWIND_PACKET_20261109.md).
    /// The 60% phase 2 threshold is single-source (one 2015 guide), so tracking is disabled by default
    /// and has no gameplay effect; these tests pin that behaviour, not retail behaviour.
    /// </summary>
    public class BladeWindPhaseTrackingTests
    {
        [Fact]
        public void TrackingIsDisabledByDefaultAndPhaseNeverChanges()
        {
            (BladeWindTheInvokerVeteranEntityScript script, RecordingDispatchProxy<ICreatureEntity> proxy) = CreateScript();

            SetHealth(proxy, 10u);
            script.OnHealthChange(null, 90u, DamageType.Physical);

            Assert.False(script.PhaseTrackingEnabled);
            Assert.Equal(BladeWindPhase.One, script.Phase);
        }

        [Fact]
        public void EnabledTrackingMovesToPhaseTwoAtTheSingleSourceThresholdOnce()
        {
            (BladeWindTheInvokerVeteranEntityScript script, RecordingDispatchProxy<ICreatureEntity> proxy) = CreateScript();
            script.PhaseTrackingEnabled = true;

            SetHealth(proxy, 61u);
            script.OnHealthChange(null, 39u, DamageType.Physical);
            Assert.Equal(BladeWindPhase.One, script.Phase);

            SetHealth(proxy, 60u);
            script.OnHealthChange(null, 1u, DamageType.Physical);
            Assert.Equal(BladeWindPhase.Two, script.Phase);

            SetHealth(proxy, 95u);
            script.OnHealthChange(null, 35u, DamageType.Heal);
            Assert.Equal(BladeWindPhase.Two, script.Phase);
        }

        [Fact]
        public void LeavingCombatResetsToPhaseOne()
        {
            (BladeWindTheInvokerVeteranEntityScript script, RecordingDispatchProxy<ICreatureEntity> proxy) = CreateScript();
            script.PhaseTrackingEnabled = true;

            SetHealth(proxy, 50u);
            script.OnHealthChange(null, 50u, DamageType.Physical);
            Assert.Equal(BladeWindPhase.Two, script.Phase);

            script.OnLeaveCombat();
            Assert.Equal(BladeWindPhase.One, script.Phase);

            SetHealth(proxy, 100u);
            script.OnHealthChange(null, 50u, DamageType.Heal);
            Assert.Equal(BladeWindPhase.One, script.Phase);
        }

        [Fact]
        public void InterfaceDispatchReachesThePhaseHandlers()
        {
            // The game invokes unit scripts through IUnitScript, so the handlers must be reachable that way.
            (BladeWindTheInvokerVeteranEntityScript script, RecordingDispatchProxy<ICreatureEntity> proxy) = CreateScript();
            script.PhaseTrackingEnabled = true;
            IUnitScript unitScript = script;

            SetHealth(proxy, 40u);
            unitScript.OnHealthChange(null, 60u, DamageType.Physical);
            Assert.Equal(BladeWindPhase.Two, script.Phase);

            unitScript.OnLeaveCombat();
            Assert.Equal(BladeWindPhase.One, script.Phase);
        }

        private static void SetHealth(RecordingDispatchProxy<ICreatureEntity> proxy, uint health)
        {
            proxy.SetProperty(nameof(IUnitEntity.Health), health);
        }

        private static (BladeWindTheInvokerVeteranEntityScript, RecordingDispatchProxy<ICreatureEntity>) CreateScript()
        {
            ICreatureEntity owner = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> proxy);
            proxy.SetProperty(nameof(IUnitEntity.Health), 100u);
            proxy.SetProperty(nameof(IUnitEntity.MaxHealth), 100u);

            var script = new BladeWindTheInvokerVeteranEntityScript(
                RecordingDispatchProxy<IFactory<ISpellParameters>>.Create(out _),
                RecordingDispatchProxy<IGameTableManager>.Create(out _));
            script.OnLoad(owner);
            return (script, proxy);
        }
    }
}
