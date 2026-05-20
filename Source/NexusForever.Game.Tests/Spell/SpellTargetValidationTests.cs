namespace NexusForever.Game.Tests.Spell;

public class SpellTargetValidationTests
{
    [Theory]
    [InlineData(22u, 22u, 180f, false)]
    [InlineData(22u, 170u, 180f, true)]
    [InlineData(22u, 170u, 0f, false)]
    [InlineData(22u, 170u, 360f, false)]
    public void ShouldApplyPrimaryTargetAngle_SkipsSelfAnchoredTargets(uint casterGuid, uint targetGuid, float targetAngle, bool expected)
    {
        bool result = NexusForever.Game.Spell.Spell.ShouldApplyPrimaryTargetAngle(casterGuid, targetGuid, targetAngle);

        Assert.Equal(expected, result);
    }
}
