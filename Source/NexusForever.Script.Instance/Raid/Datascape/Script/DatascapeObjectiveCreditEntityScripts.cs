using NexusForever.Game.Abstract.Spell;
using NexusForever.GameTable;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.Datascape.Script
{
    /// <summary>
    /// WIP-guessed objective-credit hooks for Datascape rows promoted from
    /// LaughingWS/New-Zones-and-more SQL. The SQL gives branch-authored entity
    /// placement and placeholder HP notes; exact boss mechanics, spawn timing,
    /// and choreography remain blocked pending retail/client smoke proof.
    /// </summary>
    [ScriptFilterScriptName("OptimizedMemoryProbeED1EntityScript")]
    public class OptimizedMemoryProbeED1EntityScript : PublicEventObjectiveCreditEntityScript
    {
        public OptimizedMemoryProbeED1EntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatOptimizedMemoryProbeED1)
        {
        }
    }

    [ScriptFilterScriptName("OptimizedMemoryProbeP2ZEntityScript")]
    public class OptimizedMemoryProbeP2ZEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public OptimizedMemoryProbeP2ZEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatOptimizedMemoryProbeP2Z)
        {
        }
    }

    [ScriptFilterScriptName("NullSystemDaemonEntityScript")]
    public class NullSystemDaemonEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public NullSystemDaemonEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheSystemDaemons)
        {
        }
    }

    [ScriptFilterScriptName("BinarySystemDaemonEntityScript")]
    public class BinarySystemDaemonEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public BinarySystemDaemonEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheSystemDaemons)
        {
        }
    }

    [ScriptFilterScriptName("DatascapeAvatusEntityScript")]
    public class DatascapeAvatusEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public DatascapeAvatusEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatAvatus)
        {
        }
    }

    [ScriptFilterScriptName("FrostBoulderAvalancheFirstEntityScript")]
    public class FrostBoulderAvalancheFirstEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public FrostBoulderAvalancheFirstEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheFirstFrostBoulderAvalanche)
        {
        }
    }

    [ScriptFilterScriptName("FrostBoulderAvalancheSecondEntityScript")]
    public class FrostBoulderAvalancheSecondEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public FrostBoulderAvalancheSecondEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheSecondFrostBoulderAvalanche)
        {
        }
    }

    [ScriptFilterScriptName("FrostbringerWarlockEntityScript")]
    public class FrostbringerWarlockEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public FrostbringerWarlockEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheFrostbringerWarlock)
        {
        }
    }

    [ScriptFilterScriptName("BioEnhancedBroodmotherEntityScript")]
    public class BioEnhancedBroodmotherEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public BioEnhancedBroodmotherEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheBioEnhancedBroodmother)
        {
        }
    }

    [ScriptFilterScriptName("GloomclawEntityScript")]
    public class GloomclawEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public GloomclawEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatGloomclaw)
        {
        }
    }

    [ScriptFilterScriptName("HyperAcceleratedSkeledroidEntityScript")]
    public class HyperAcceleratedSkeledroidEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public HyperAcceleratedSkeledroidEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheHyperAcceleratedSkeledroid)
        {
        }
    }

    [ScriptFilterScriptName("AugmentedHeraldOfAvatusEntityScript")]
    public class AugmentedHeraldOfAvatusEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public AugmentedHeraldOfAvatusEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatTheAugmentedHeraldOfAvatus)
        {
        }
    }

    [ScriptFilterScriptName("WarmongerAgrathaEntityScript")]
    public class WarmongerAgrathaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public WarmongerAgrathaEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatWarmongerAgratha)
        {
        }
    }

    [ScriptFilterScriptName("WarmongerChunaEntityScript")]
    public class WarmongerChunaEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public WarmongerChunaEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatWarmongerChuna)
        {
        }
    }

    [ScriptFilterScriptName("WarmongerTalariiEntityScript")]
    public class WarmongerTalariiEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public WarmongerTalariiEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatWarmongerTalarii)
        {
        }
    }

    [ScriptFilterScriptName("GrandWarmongerTargreshEntityScript")]
    public class GrandWarmongerTargreshEntityScript : PublicEventObjectiveCreditEntityScript
    {
        public GrandWarmongerTargreshEntityScript(IFactory<ISpellParameters> spellParametersFactory, IGameTableManager gameTableManager)
            : base(spellParametersFactory, gameTableManager, (uint)PublicEventObjective.DefeatGrandWarmongerTargresh)
        {
        }
    }
}
