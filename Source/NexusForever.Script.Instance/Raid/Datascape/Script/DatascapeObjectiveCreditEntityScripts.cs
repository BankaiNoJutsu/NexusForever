using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.Datascape.Script
{
    /// <summary>
    /// WIP-guessed objective-credit hooks for Datascape rows promoted from
    /// LaughingWS/New-Zones-and-more SQL. The SQL gives branch-authored entity
    /// placement and placeholder HP notes; exact boss mechanics, spawn timing,
    /// and choreography remain blocked pending retail/client smoke proof.
    /// </summary>
    public class OptimizedMemoryProbeED1EntityScript : PublicEventObjectiveCreditEntityScript
    {
        public OptimizedMemoryProbeED1EntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatOptimizedMemoryProbeED1)
        {
        }
    }

    public class OptimizedMemoryProbeP2ZEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public OptimizedMemoryProbeP2ZEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatOptimizedMemoryProbeP2Z)
        {
        }
    }

    public class NullSystemDaemonEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public NullSystemDaemonEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheSystemDaemons)
        {
        }
    }

    public class BinarySystemDaemonEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public BinarySystemDaemonEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheSystemDaemons)
        {
        }
    }

    public class DatascapeAvatusEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public DatascapeAvatusEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatAvatus)
        {
        }
    }

    public class FrostBoulderAvalancheFirstEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public FrostBoulderAvalancheFirstEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheFirstFrostBoulderAvalanche)
        {
        }
    }

    public class FrostBoulderAvalancheSecondEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public FrostBoulderAvalancheSecondEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheSecondFrostBoulderAvalanche)
        {
        }
    }

    public class FrostbringerWarlockEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public FrostbringerWarlockEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheFrostbringerWarlock)
        {
        }
    }

    public class BioEnhancedBroodmotherEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public BioEnhancedBroodmotherEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheBioEnhancedBroodmother)
        {
        }
    }

    public class GloomclawEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public GloomclawEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatGloomclaw)
        {
        }
    }

    public class HyperAcceleratedSkeledroidEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public HyperAcceleratedSkeledroidEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheHyperAcceleratedSkeledroid)
        {
        }
    }

    public class AugmentedHeraldOfAvatusEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public AugmentedHeraldOfAvatusEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheAugmentedHeraldOfAvatus)
        {
        }
    }

    public class WarmongerAgrathaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public WarmongerAgrathaEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatWarmongerAgratha)
        {
        }
    }

    public class WarmongerChunaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public WarmongerChunaEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatWarmongerChuna)
        {
        }
    }

    public class WarmongerTalariiEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public WarmongerTalariiEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatWarmongerTalarii)
        {
        }
    }

    public class GrandWarmongerTargreshEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public GrandWarmongerTargreshEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatGrandWarmongerTargresh)
        {
        }
    }
}
