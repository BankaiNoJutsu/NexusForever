using NexusForever.Game.Static.Matching;
using NexusForever.WorldServer.Network.Message.Handler.Matching;

namespace NexusForever.Game.Tests.Matching;

public class ClientMatchingMatchInitiateLookingForReplacementsHandlerTests
{
    [Theory]
    [InlineData(Role.None)]
    [InlineData(Role.Tank)]
    [InlineData(Role.Healer)]
    [InlineData(Role.DPS)]
    [InlineData(Role.Tank | Role.Healer | Role.DPS)]
    public void IsValidReplacementRoleMask_AllowsMappedClientRoleBits(Role roles)
    {
        Assert.True(ClientMatchingMatchInitiateLookingForReplacementsHandler.IsValidReplacementRoleMask(roles));
    }

    [Theory]
    [InlineData((Role)0x08)]
    [InlineData((Role)0x10)]
    [InlineData(Role.Tank | (Role)0x08)]
    public void IsValidReplacementRoleMask_RejectsUnmappedRoleBits(Role roles)
    {
        Assert.False(ClientMatchingMatchInitiateLookingForReplacementsHandler.IsValidReplacementRoleMask(roles));
    }
}
