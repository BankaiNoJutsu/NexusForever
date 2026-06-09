using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;
using System.Reflection;
using System.Runtime.CompilerServices;
using NetworkIdentity = NexusForever.Network.World.Message.Model.Shared.Identity;

namespace NexusForever.Game.Tests.Housing;

public class ClientHousingCommunityRenameHandlerTests
{
    [Fact]
    public void HandleMessage_WhenPlayerIsNotLeader_SendsInvalidPermissions()
    {
        ClientHousingCommunityRenameHandler handler = CreateHandler();
        IWorldSession session = CreateSession(
            playerCharacterId: 100ul,
            communityLeaderId: 200ul,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<ICommunity> communityProxy);

        handler.HandleMessage(session, ReadRequest("New Community Name"));

        ServerHousingCommunityRenameResult result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingCommunityRenameResult>());
        Assert.Equal(HousingResult.InvalidPermissions, result.Result);
        Assert.Equal(0u, result.Reserved0);
        Assert.Equal(0u, result.Reserved1);
        Assert.Equal(0u, result.Reserved2);
        Assert.Empty(communityProxy.GetInvocations(nameof(ICommunity.RenameGuild)));
    }

    [Fact]
    public void HandleMessage_WhenGameFormulaTableMissing_SendsFailedWithoutMutation()
    {
        ClientHousingCommunityRenameHandler handler = CreateHandler(
            out RecordingDispatchProxy<ITextFilterManager> textFilterProxy,
            out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        textFilterProxy.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        gameTableProxy.SetProperty(nameof(IGameTableManager.GameFormula), null);

        IWorldSession session = CreateSession(
            playerCharacterId: 100ul,
            communityLeaderId: 100ul,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<ICommunity> communityProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IResidenceMapInstance> residenceMapProxy);

        handler.HandleMessage(session, ReadRequest("New Community Name"));

        ServerHousingCommunityRenameResult result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingCommunityRenameResult>());
        Assert.Equal(HousingResult.Failed, result.Result);
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CanAfford)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(communityProxy.GetInvocations(nameof(ICommunity.RenameGuild)));
        Assert.Empty(residenceMapProxy.GetInvocations(nameof(IResidenceMapInstance.RenameResidence)));
    }

    [Fact]
    public void HandleMessage_WhenFormulaBackedCreditsAffordable_RenamesAndDebitsCost()
    {
        ClientHousingCommunityRenameHandler handler = CreateHandler(
            out RecordingDispatchProxy<ITextFilterManager> textFilterProxy,
            out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
        textFilterProxy.SetMethodReturn(nameof(ITextFilterManager.IsTextValid), true);
        gameTableProxy.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry
        {
            Id       = 2395u,
            Dataint0 = 1234u
        }));

        IWorldSession session = CreateSession(
            playerCharacterId: 100ul,
            communityLeaderId: 100ul,
            out RecordingDispatchProxy<IWorldSession> sessionProxy,
            out RecordingDispatchProxy<ICommunity> communityProxy,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out RecordingDispatchProxy<IResidenceMapInstance> residenceMapProxy);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

        handler.HandleMessage(session, ReadRequest("New Community Name"));

        ServerHousingCommunityRenameResult result = Assert.Single(GetEncryptedMessages(sessionProxy).OfType<ServerHousingCommunityRenameResult>());
        Assert.Equal(HousingResult.Success, result.Result);

        RecordingDispatchProxy<ICurrencyManager>.Invocation subtract = Assert.Single(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(CurrencyType.Credits, subtract.Arguments[0]);
        Assert.Equal(1234ul, Convert.ToUInt64(subtract.Arguments[1]));

        RecordingDispatchProxy<ICommunity>.Invocation renameGuild = Assert.Single(communityProxy.GetInvocations(nameof(ICommunity.RenameGuild)));
        Assert.Equal("New Community Name", renameGuild.Arguments[0]);

        RecordingDispatchProxy<IResidenceMapInstance>.Invocation renameResidence = Assert.Single(residenceMapProxy.GetInvocations(nameof(IResidenceMapInstance.RenameResidence)));
        Assert.Equal("New Community Name", renameResidence.Arguments[1]);
    }

    private static ClientHousingCommunityRenameHandler CreateHandler()
    {
        return CreateHandler(out _, out _);
    }

    private static ClientHousingCommunityRenameHandler CreateHandler(
        out RecordingDispatchProxy<ITextFilterManager> textFilterProxy,
        out RecordingDispatchProxy<IGameTableManager> gameTableProxy)
    {
        ITextFilterManager textFilterManager = RecordingDispatchProxy<ITextFilterManager>.Create(out textFilterProxy);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out gameTableProxy);
        IRealmContext realmContext = RecordingDispatchProxy<IRealmContext>.Create(out _);
        return new ClientHousingCommunityRenameHandler(textFilterManager, gameTableManager, realmContext);
    }

    private static IWorldSession CreateSession(
        ulong playerCharacterId,
        ulong communityLeaderId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<ICommunity> communityProxy)
    {
        return CreateSession(
            playerCharacterId,
            communityLeaderId,
            out sessionProxy,
            out communityProxy,
            out _,
            out _);
    }

    private static IWorldSession CreateSession(
        ulong playerCharacterId,
        ulong communityLeaderId,
        out RecordingDispatchProxy<IWorldSession> sessionProxy,
        out RecordingDispatchProxy<ICommunity> communityProxy,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        out RecordingDispatchProxy<IResidenceMapInstance> residenceMapProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        IGuildManager guildManager = RecordingDispatchProxy<IGuildManager>.Create(out RecordingDispatchProxy<IGuildManager> guildManagerProxy);
        ICommunity community = RecordingDispatchProxy<ICommunity>.Create(out communityProxy);
        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        IResidence residence = RecordingDispatchProxy<IResidence>.Create(out RecordingDispatchProxy<IResidence> residenceProxy);
        IResidenceMapInstance map = RecordingDispatchProxy<IResidenceMapInstance>.Create(out residenceMapProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), playerCharacterId);
        playerProxy.SetProperty(nameof(IPlayer.GuildManager), guildManager);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        communityProxy.SetProperty(nameof(ICommunity.LeaderId), communityLeaderId);
        communityProxy.SetProperty(nameof(ICommunity.Identity), new Identity
        {
            RealmId = 7,
            Id      = 0x5566778899AABBCCul
        });
        residenceProxy.SetProperty(nameof(IResidence.Map), map);
        communityProxy.SetProperty(nameof(ICommunity.Residence), residence);
        guildManagerProxy.SetMethodReturn(nameof(IGuildManager.GetGuild), community);

        return session;
    }

    private static ClientHousingCommunityRename ReadRequest(string name)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            var targetGuild = new NetworkIdentity
            {
                RealmId = 7,
                Id      = 0x5566778899AABBCCul
            };

            targetGuild.Write(writer);
            writer.WriteStringWide(name);
            writer.Write(false);
            writer.FlushBits();
        }

        byte[] packetData = stream.ToArray();
        using var packetStream = new MemoryStream(packetData);
        using var reader = new GamePacketReader(packetStream);
        var request = new ClientHousingCommunityRename();
        request.Read(reader);
        return request;
    }

    private static IEnumerable<object> GetEncryptedMessages(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        return sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0]);
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }
}
