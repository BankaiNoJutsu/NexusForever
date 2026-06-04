using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Static.Matching;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Matching;

namespace NexusForever.Game.Tests.Matching;

public class MatchingLookingForReplacementsValidationTests
{
    [Theory]
    [InlineData(Role.None)]
    [InlineData(Role.Tank)]
    [InlineData(Role.Healer)]
    [InlineData(Role.DPS)]
    [InlineData(Role.Tank | Role.Healer | Role.DPS)]
    public void IsValidReplacementRoleMask_AllowsMappedClientRoleBits(Role roles)
    {
        Assert.True(MatchingLookingForReplacementsValidation.IsValidReplacementRoleMask(roles));
    }

    [Theory]
    [InlineData((Role)0x08)]
    [InlineData((Role)0x10)]
    [InlineData(Role.Tank | (Role)0x08)]
    public void IsValidReplacementRoleMask_RejectsUnmappedRoleBits(Role roles)
    {
        Assert.False(MatchingLookingForReplacementsValidation.IsValidReplacementRoleMask(roles));
    }

    [Fact]
    public void InitiateLookingForReplacements_InProgressMatch_DoesNotEmitBlockedBackfillPackets()
    {
        IWorldSession session = CreateInProgressMatchSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out IMatchManager matchManager);
        var handler = new ClientMatchingMatchInitiateLookingForReplacementsHandler(
            NullLogger<ClientMatchingMatchInitiateLookingForReplacementsHandler>.Instance,
            matchManager);

        handler.HandleMessage(session, ReadInitiateLookingForReplacements(Role.Tank | Role.Healer));

        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    [Fact]
    public void StopLookingForReplacements_InProgressMatch_DoesNotEmitBlockedBackfillPackets()
    {
        IWorldSession session = CreateInProgressMatchSession(out RecordingDispatchProxy<IWorldSession> sessionProxy, out IMatchManager matchManager);
        var handler = new ClientMatchingStopLookingForReplacementsHandler(
            NullLogger<ClientMatchingStopLookingForReplacementsHandler>.Instance,
            matchManager);

        handler.HandleMessage(session, new ClientMatchingStopLookingForReplacements());

        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    private static IWorldSession CreateInProgressMatchSession(
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out IMatchManager matchManager)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IMatchCharacter matchCharacter = RecordingDispatchProxy<IMatchCharacter>.Create(out RecordingDispatchProxy<IMatchCharacter> matchCharacterProxy);
        IMatch match = RecordingDispatchProxy<IMatch>.Create(out RecordingDispatchProxy<IMatch> matchProxy);
        matchManager = RecordingDispatchProxy<IMatchManager>.Create(out RecordingDispatchProxy<IMatchManager> matchManagerProxy);

        var identity = new Identity
        {
            RealmId = 1,
            Id = 42
        };

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 42u);
        playerProxy.SetProperty(nameof(IPlayer.Identity), identity);
        matchProxy.SetProperty(nameof(IMatch.Guid), Guid.Parse("11111111-1111-1111-1111-111111111111"));
        matchProxy.SetProperty(nameof(IMatch.Status), MatchStatus.InProgress);
        matchCharacterProxy.SetProperty(nameof(IMatchCharacter.Match), match);
        matchManagerProxy.SetMethodReturn(nameof(IMatchManager.GetMatchCharacter), matchCharacter);

        return session;
    }

    private static ClientMatchingMatchInitiateLookingForReplacements ReadInitiateLookingForReplacements(Role roles)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
            writer.Write((uint)roles);

        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var packet = new ClientMatchingMatchInitiateLookingForReplacements();
        packet.Read(reader);
        return packet;
    }
}
