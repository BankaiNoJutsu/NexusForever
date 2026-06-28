using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Dungeon.UltimateProtogames.Script
{
    /// <summary>
    /// Mapped from build 16042 PublicEventObjective 2926 / TargetGroup 10657
    /// and the paired timed child objective 2941, whose Creature2 member is
    /// Mondo's Monstrosity 62575.
    /// </summary>
    [ScriptFilterCreatureId(62575u)]
    public class MondosMonstrosityEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MondosMonstrosityEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(
                spellParametersFactory,
                gameTableManager,
                (uint)PublicEventObjective.MonstrosityMassacre,
                (uint)PublicEventObjective.QuickReflexes)
        {
        }
    }

    /// <summary>
    /// Mapped from build 16042 PublicEventObjective 2920, whose Jabbithole
    /// public-event creature row maps Mondo's Crate to Creature2 62549.
    /// </summary>
    [ScriptFilterCreatureId(62549u)]
    public class MondosCrateEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MondosCrateEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.MondosCrate)
        {
        }
    }

    /// <summary>
    /// Mapped from build 16042 PublicEventObjective 4561 / TargetGroup 12390,
    /// whose Creature2 member is Ruffles 65794.
    /// </summary>
    [ScriptFilterCreatureId(65794u)]
    public class RufflesEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public RufflesEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.HuntRuffles)
        {
        }
    }

    /// <summary>
    /// Mapped from build 16042 PublicEventObjective 2862/4442 / TargetGroup
    /// 12263, whose Creature2 member is Gilded Fowl 63055.
    /// </summary>
    [ScriptFilterCreatureId(63055u)]
    public class GildedFowlEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private ICreatureEntity entity;
        private bool credited;

        public void OnLoad(ICreatureEntity owner)
        {
            entity = owner;
        }

        public void OnDeath()
        {
            if (credited)
                return;

            credited = true;
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.GildedFowl, 1);
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.GildedFowl2, 100);
        }
    }

    /// <summary>
    /// Mapped from build 16042 PublicEventObjective 2675, whose client
    /// Creature2 row is Hut-Hut 61417.
    /// </summary>
    [ScriptFilterCreatureId(61417u)]
    public class HutHutEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public HutHutEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatHutHut)
        {
        }
    }

    /// <summary>
    /// Mapped from build 16042 PublicEventObjective 4657 / TargetGroup 12528,
    /// whose Creature2 member is Deputy 68949.
    /// </summary>
    [ScriptFilterCreatureId(68949u)]
    public class DeputyEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public DeputyEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.Deputy)
        {
        }
    }

    /// <summary>
    /// Mapped from build 16042 TargetGroup 12474, whose Warden member is
    /// Creature2 62324. The aggregate Prototentiary objective also needs the
    /// Cage Console leg; stealth and timed bonus semantics remain blocked.
    /// </summary>
    [ScriptFilterCreatureId(62324u)]
    public class WardenEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public WardenEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.SneakThroughThePrototentiary)
        {
        }
    }

    /// <summary>
    /// Mapped from build 16042 PublicEventObjective 4692 / TargetGroup 12577,
    /// whose Creature2 member is Misplaced Mammoth 63312.
    /// </summary>
    [ScriptFilterCreatureId(63312u)]
    public class MisplacedMammothEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public MisplacedMammothEntityScript(
            IFactory<ISpellParameters> spellParametersFactory,
            IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.QuickReflexes2)
        {
        }
    }

    /// <summary>
    /// Mapped from build 16042 PublicEventObjective 2676/2868/2869/2872 /
    /// TargetGroup 12671, whose Creature2 members are Busted Red Tank 62542,
    /// Wrecked Blue Tank 62543, and Malfunctioning Yellow Tank 62546.
    /// Objectives 2676 and 2872 are timed dynamic-max all-three cleanup rows;
    /// exact random route and timer semantics remain blocked.
    /// </summary>
    [ScriptFilterCreatureId(62542u, 62543u, 62546u)]
    public class MalfunctioningTankEntityScript : IUnitScript, IOwnedScript<ICreatureEntity>
    {
        private ICreatureEntity entity;
        private bool credited;

        public void OnLoad(ICreatureEntity owner)
        {
            entity = owner;
        }

        public void OnDeath()
        {
            if (credited)
                return;

            credited = true;
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.DestructODerby, 1);
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.TankTrample, 1);
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.CanCrusher, 1);
            entity.Map.PublicEventManager.UpdateObjective(PublicEventObjective.GoingGreen, 1);
        }
    }
}
