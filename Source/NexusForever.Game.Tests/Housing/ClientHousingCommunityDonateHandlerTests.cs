using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;

namespace NexusForever.Game.Tests.Housing;

public class ClientHousingCommunityDonateHandlerTests
{
    private const ushort RealmId = 7;
    private const ulong PlayerCharacterId = 1001ul;
    private const ulong PlayerResidenceId = 2002ul;
    private const ulong CommunityResidenceId = 3003ul;
    private const ulong DecorId = 4004ul;

    [Fact]
    public void DonateHandler_CannotDonateFlag_SendsResultAndDoesNotMutateResidences()
    {
        var handler = new ClientHousingCommunityDonateHandler();

        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IResidenceMapInstance map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out _);
        IResidenceManager residenceManager = RecordingDispatchProxy<IResidenceManager>.Create(out RecordingDispatchProxy<IResidenceManager> residenceManagerProxy);
        IGuildManager guildManager = RecordingDispatchProxy<IGuildManager>.Create(out RecordingDispatchProxy<IGuildManager> guildManagerProxy);
        ICommunity community = RecordingDispatchProxy<ICommunity>.Create(out RecordingDispatchProxy<ICommunity> communityProxy);
        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out RecordingDispatchProxy<IResidence> residenceProxy);
        IResidence communityResidence = RecordingDispatchProxy<IResidence>.Create(out RecordingDispatchProxy<IResidence> communityResidenceProxy);
        IDecor decor = RecordingDispatchProxy<IDecor>.Create(out RecordingDispatchProxy<IDecor> decorProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Identity), new NexusForever.Game.Abstract.Identity { RealmId = RealmId, Id = PlayerCharacterId });
        playerProxy.SetProperty(nameof(IPlayer.Name), "Donor");
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.ResidenceManager), residenceManager);
        playerProxy.SetProperty(nameof(IPlayer.GuildManager), guildManager);
        residenceManagerProxy.SetProperty(nameof(IResidenceManager.Residence), residence);

        residenceProxy.SetProperty(nameof(IResidence.Id), PlayerResidenceId);
        residenceProxy.SetMethodReturn(nameof(IResidence.GetDecor), decor);
        communityResidenceProxy.SetProperty(nameof(IResidence.Id), CommunityResidenceId);
        communityProxy.SetProperty(nameof(ICommunity.Residence), communityResidence);
        guildManagerProxy.SetMethodReturn(nameof(IGuildManager.GetGuild), community);

        decorProxy.SetProperty(nameof(IDecor.DecorId), DecorId);
        decorProxy.SetProperty(nameof(IDecor.Type), DecorType.Crate);
        decorProxy.SetProperty(nameof(IDecor.Entry), new HousingDecorInfoEntry { Flags = 0x8u });

        handler.HandleMessage(session, CreateDonateRequest(DecorId));

        ServerHousingResult result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingResult>());
        Assert.Equal(RealmId, result.RealmId);
        Assert.Equal(PlayerResidenceId, result.ResidenceId);
        Assert.Equal("Donor", result.PlayerName);
        Assert.Equal(HousingResult.Decor_CannotDonate, result.Result);
        Assert.Empty(GetEncryptedMessages(sessionProxy).OfType<ServerHousingCommunityDonateUpdate>());
        Assert.Empty(communityResidenceProxy.GetInvocations(nameof(IResidence.DecorCopy)));
        Assert.Empty(residenceProxy.GetInvocations(nameof(IResidence.DecorRemove)));
        Assert.Empty(decorProxy.GetInvocations(nameof(IDecor.EnqueueDelete)));
    }

    private static ClientHousingCommunityDonate CreateDonateRequest(ulong decorId)
    {
        var packet = new ClientHousingCommunityDonate();
        var decorInfo = new DecorInfo();
        SetPacketProperty(decorInfo, nameof(DecorInfo.DecorId), decorId);
        packet.Decor.Add(decorInfo);
        return packet;
    }

    private static void SetPacketProperty(object packet, string propertyName, object value)
    {
        var property = packet.GetType().GetProperty(propertyName);
        property?.GetSetMethod(true)?.Invoke(packet, [value]);
    }

    private static IEnumerable<object> GetEncryptedMessages<TSession>(RecordingDispatchProxy<TSession> sessionProxy)
        where TSession : class
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }
}
