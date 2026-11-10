using NexusForever.Game.Static.Spell;

namespace NexusForever.Game.Tests.Spell;

public class ProcTriggerEventCandidateTests
{
    [Theory]
    [InlineData(ProcTriggerEventCandidate.ReceiveDamageMelee, "receive-damage-melee")]
    [InlineData(ProcTriggerEventCandidate.ReceiveDamageRanged, "receive-damage-ranged")]
    [InlineData(ProcTriggerEventCandidate.ReceiveDamageMagic, "receive-damage-magic")]
    public void TryGetConservativeLabel_RecognizesSchoolSpecificReceiveDamageEvents(uint triggerEvent, string expectedLabel)
    {
        Assert.True(ProcTriggerEventCandidate.TryGetConservativeLabel(triggerEvent, out string label));
        Assert.True(ProcTriggerEventCandidate.IsConservativelySupported(triggerEvent));
        Assert.Equal(expectedLabel, label);
    }

    // Event 87: a class resource changed. Retail: Warrior Full Defense (Spell4 59159) "While above 500 Kinetic Energy gain a Defense"; the ON auras of the two Warrior
    // toggles (79790, 79753) cast the toggle's "Spell Remove" from it under the prerequisite "Kinetic Energy < 250".
    [Fact]
    public void ResourceChangedIsADispatchedTriggerEvent()
    {
        Assert.Equal(87u, ProcTriggerEventCandidate.ResourceChanged);
        Assert.True(ProcTriggerEventCandidate.TryGetConservativeLabel(ProcTriggerEventCandidate.ResourceChanged, out string label));
        Assert.Equal("resource-changed", label);
        Assert.True(ProcDispatchEvidenceBoundary.Describe(ProcTriggerEventCandidate.ResourceChanged, 9u).IsConservativelyDispatchSupported);
    }

    [Theory]
    [InlineData(SpellSchool.Melee, ProcTriggerEventCandidate.ReceiveDamageMelee)]
    [InlineData(SpellSchool.Ranged, ProcTriggerEventCandidate.ReceiveDamageRanged)]
    [InlineData(SpellSchool.Spell, ProcTriggerEventCandidate.ReceiveDamageMagic)]
    public void TryGetReceiveDamageSchoolTriggerEvent_MapsSupportedSchools(SpellSchool school, uint expectedTriggerEvent)
    {
        Assert.True(ProcTriggerEventCandidate.TryGetReceiveDamageSchoolTriggerEvent(school, out uint triggerEvent));
        Assert.Equal(expectedTriggerEvent, triggerEvent);
    }

    [Fact]
    public void TryGetReceiveDamageSchoolTriggerEvent_LeavesUnarmedOnGenericReceiveDamage()
    {
        Assert.False(ProcTriggerEventCandidate.TryGetReceiveDamageSchoolTriggerEvent(SpellSchool.Unarmed, out _));
    }
}