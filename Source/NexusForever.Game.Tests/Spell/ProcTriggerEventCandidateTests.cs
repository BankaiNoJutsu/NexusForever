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