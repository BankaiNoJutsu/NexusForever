using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Static.Spell;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Prerequisite;

// Native evidence (Decomp/Analysis/PREREQUISITE_NATIVE_COMPARISONS_20261107.md):
//  - type 11 OtherPrerequisite (evaluator case 0xb): comparison 2 returns the NOT of the nested prerequisite, every other comparison returns the nested result. Retail uses comparison 1 (3,559
//    slots), 2 (322) and 0 (22).
//  - type 52 Path (evaluator case 0x34, FUN_14049fd50): the player's path equals Value; comparison 1 returns that, 2 its negation, any other comparison false. Retail: Equal 538, NotEqual 14.
//  - type 105 SpellEffectTypeOnUnit (evaluator case 0x69, inline; Decomp/Analysis/PREREQUISITE_105_SPELL_EFFECT_TYPE_20261110.md): the id compared with each active node is the slot's
//    ObjectId, not its Value; comparison 1 returns the match, 2 its negation, any other comparison false. Retail: 7 slots, ObjectId 0/3/31/83/100/112, Value always 0.
public class PrerequisiteNativeComparisonTests
{
    // Retail row 7405 is (Equal, ObjectId 83 Stealth, Value 0); row 37231 is three NotEqual slots with ObjectId 31, 100 and 112 (the disguise effects) and Value 0.
    [Theory]
    [InlineData(PrerequisiteComparison.Equal, SpellEffectType.Stealth, true)]
    [InlineData(PrerequisiteComparison.Equal, SpellEffectType.Disguise, false)]
    [InlineData(PrerequisiteComparison.NotEqual, SpellEffectType.Stealth, false)]
    [InlineData(PrerequisiteComparison.NotEqual, SpellEffectType.Disguise, true)]
    [InlineData(PrerequisiteComparison.NotEqual, SpellEffectType.MimicDisguise, true)]
    [InlineData(PrerequisiteComparison.NotEqual, SpellEffectType.DisguiseOutfit, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, SpellEffectType.Stealth, false)]
    [InlineData((PrerequisiteComparison)0, SpellEffectType.Stealth, false)]
    public void SpellEffectTypeOnUnitReadsTheEffectTypeFromObjectId(PrerequisiteComparison comparison, SpellEffectType objectId, bool expected)
    {
        // the unit only has an active Stealth effect
        var asked = new List<SpellEffectType>();
        bool result = PrerequisiteCheckSpellEffectTypeOnUnit.Evaluate(comparison, (uint)objectId, type =>
        {
            asked.Add(type);
            return type == SpellEffectType.Stealth;
        });

        Assert.Equal(expected, result);
        Assert.Equal([objectId], asked);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, true, true)]
    [InlineData(PrerequisiteComparison.Equal, false, false)]
    [InlineData(PrerequisiteComparison.NotEqual, true, false)]
    [InlineData(PrerequisiteComparison.NotEqual, false, true)]
    [InlineData((PrerequisiteComparison)0, true, true)]                    // 22 retail slots use comparison 0
    [InlineData((PrerequisiteComparison)0, false, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, true, true)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual, false, false)]
    public void OtherPrerequisiteNegatesOnlyForComparisonTwo(PrerequisiteComparison comparison, bool nested, bool expected)
    {
        IPrerequisiteManager manager = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var proxy);
        proxy.SetMethodHandler(nameof(IPrerequisiteManager.Meets), _ => nested);
        proxy.SetMethodHandler(nameof(IPrerequisiteManager.MeetsUnit), _ => nested);
        var check = new PrerequisiteCheckOtherPrerequisite(NullLogger<PrerequisiteCheckOtherPrerequisite>.Instance, manager);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);

        Assert.Equal(expected, check.Meets(player, comparison, 0u, 1234u, new PrerequisiteParameters()));
        Assert.Equal(expected, check.MeetsUnit(player, comparison, 0u, 1234u, new PrerequisiteParameters()));
    }

    // Native type 3 (FUN_14049c560) compares the class for equality or inequality and returns false for any other comparison; the server returned true.
    [Theory]
    [InlineData(PrerequisiteComparison.GreaterThan)]
    [InlineData(PrerequisiteComparison.LessThanOrEqual)]
    [InlineData((PrerequisiteComparison)0)]
    public void ClassNeverMeetsAnUnsupportedComparison(PrerequisiteComparison comparison)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.Class), Class.Warrior);

        var check = new PrerequisiteCheckClass(NullLogger<PrerequisiteCheckClass>.Instance);
        Assert.False(check.Meets(player, comparison, (uint)Class.Warrior, 0u, new PrerequisiteParameters()));
    }

    [Theory]
    [InlineData(PrerequisiteComparison.Equal, Path.Soldier, true)]
    [InlineData(PrerequisiteComparison.Equal, Path.Scientist, false)]
    [InlineData(PrerequisiteComparison.NotEqual, Path.Soldier, false)]
    [InlineData(PrerequisiteComparison.NotEqual, Path.Scientist, true)]
    [InlineData(PrerequisiteComparison.GreaterThan, Path.Soldier, false)]
    [InlineData(PrerequisiteComparison.LessThan, Path.Scientist, false)]
    [InlineData((PrerequisiteComparison)0, Path.Soldier, false)]
    public void PathComparesTheActivePathWithValue(PrerequisiteComparison comparison, Path value, bool expected)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IPathManager pathManager = RecordingDispatchProxy<IPathManager>.Create(out var pathProxy);
        pathProxy.SetMethodHandler(nameof(IPathManager.IsPathActive), args => (Path)args[0] == Path.Soldier);
        playerProxy.SetProperty(nameof(IPlayer.PathManager), pathManager);

        var check = new PrerequisiteCheckPath(NullLogger<PrerequisiteCheckPath>.Instance);

        Assert.Equal(expected, check.Meets(player, comparison, (uint)value, 0u, new PrerequisiteParameters()));
    }
}
