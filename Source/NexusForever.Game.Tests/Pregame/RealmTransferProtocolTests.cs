using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database;
using NexusForever.Database.Auth.Model;
using NexusForever.Game;
using NexusForever.Game.Abstract.Server;
using NexusForever.Game.Static.Pregame;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Network;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Character;
using NexusForever.WorldServer.Network.Message.Handler.Misc;

namespace NexusForever.Game.Tests.Pregame;

[Collection(LegacyServiceProviderCollection.Name)]
public class RealmTransferProtocolTests
{
    [Fact]
    public void ClientGetRealmTransferDestinationsHandler_ReturnsEmptyCompatibilityList()
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        var handler = new ClientGetRealmTransferDestinationsHandler(
            NullLogger<ClientGetRealmTransferDestinationsHandler>.Instance);

        handler.HandleMessage(session, new ClientGetRealmTransferDestinations());

        ServerTransferDestinationRealmList response = sessionProxy
            .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
            .Select(invocation => invocation.Arguments[0])
            .OfType<ServerTransferDestinationRealmList>()
            .Single();
        Assert.Empty(response.Realms);
    }

    [Fact]
    public void ServerTransferDestinationRealmList_WriteSerializesDestinationRows()
    {
        var message = new ServerTransferDestinationRealmList
        {
            Realms =
            [
                new ServerTransferDestinationRealmList.RealmTransferInfo
                {
                    RealmId = 1001u,
                    RealmName = "Destination",
                    RealmNoteStringId = 2002u,
                    Flags = RealmFlag.FactionRestricted,
                    Type = RealmType.PVP,
                    Status = RealmStatus.Up,
                    Population = RealmPopulation.High,
                    Unused1 = 3003u,
                    Unused2 = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(),
                    AccountRealmInfo = new RealmInfo.AccountRealmData
                    {
                        RealmId = 44,
                        CharacterCount = 2u,
                        LastPlayedCharacter = "Last",
                        LastPlayedTime = 0x1122334455667788ul
                    },
                    Unused3 = 55,
                    Unused4 = 66,
                    Unused5 = 77,
                    Unused6 = 88,
                    IsFree = true
                }
            ]
        };

        byte[] packetData = WritePacket(message);

        using var reader = new GamePacketReader(new MemoryStream(packetData));
        Assert.Equal(1u, reader.ReadUInt());
        Assert.Equal(1001u, reader.ReadUInt());
        Assert.Equal("Destination", reader.ReadWideString());
        Assert.Equal(2002u, reader.ReadUInt());
        Assert.Equal(RealmFlag.FactionRestricted, reader.ReadEnum<RealmFlag>(32u));
        Assert.Equal(RealmType.PVP, reader.ReadEnum<RealmType>(2u));
        Assert.Equal(RealmStatus.Up, reader.ReadEnum<RealmStatus>(3u));
        Assert.Equal(RealmPopulation.High, reader.ReadEnum<RealmPopulation>(3u));
        Assert.Equal(3003u, reader.ReadUInt());
        Assert.Equal(Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(), reader.ReadBytes(16u));
        Assert.Equal((ushort)44, reader.ReadUShort(14u));
        Assert.Equal(2u, reader.ReadUInt());
        Assert.Equal("Last", reader.ReadWideString());
        Assert.Equal(0x1122334455667788ul, reader.ReadULong());
        Assert.Equal((ushort)55, reader.ReadUShort());
        Assert.Equal((ushort)66, reader.ReadUShort());
        Assert.Equal((ushort)77, reader.ReadUShort());
        Assert.Equal((ushort)88, reader.ReadUShort());
        Assert.True(reader.ReadBit());
    }

    [Fact]
    public void ClientSelectRealmHandler_IgnoresCurrentRealmSelection()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = CreateRealmProvider(1);
        LegacyServiceProvider.Provider = provider;

        try
        {
            IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
            IServerManager serverManager = CreateServerManager(CreateServer(1, isOnline: false));
            IDatabaseManager databaseManager = RecordingDispatchProxy<IDatabaseManager>.Create(out _);
            var handler = new ClientSelectRealmHandler(serverManager, databaseManager);

            handler.HandleMessage(session, CreateSelectRealm(1u));

            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void ClientSelectRealmHandler_OfflineRealmSendsServerDownTransferResult()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        using ServiceProvider provider = CreateRealmProvider(1);
        LegacyServiceProvider.Provider = provider;

        try
        {
            IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
            IServerManager serverManager = CreateServerManager(CreateServer(2, isOnline: false));
            IDatabaseManager databaseManager = RecordingDispatchProxy<IDatabaseManager>.Create(out _);
            var handler = new ClientSelectRealmHandler(serverManager, databaseManager);

            handler.HandleMessage(session, CreateSelectRealm(2u));

            ServerRealmTransferResult result = sessionProxy
                .GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))
                .Select(invocation => invocation.Arguments[0])
                .OfType<ServerRealmTransferResult>()
                .Single();
            Assert.Equal(CharacterModifyResult.RealmTransferFailed_ServerDown, result.Result);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static byte[] WritePacket(ServerTransferDestinationRealmList message)
    {
        using var stream = new MemoryStream();
        using var writer = new GamePacketWriter(stream);
        message.Write(writer);
        writer.FlushBits();
        return stream.ToArray();
    }

    private static ServiceProvider CreateRealmProvider(ushort realmId)
    {
        var realmContext = (RealmContext)RuntimeHelpers.GetUninitializedObject(typeof(RealmContext));
        SetAutoProperty(realmContext, nameof(RealmContext.RealmId), realmId);
        return new ServiceCollection()
            .AddSingleton(realmContext)
            .BuildServiceProvider();
    }

    private static IServerManager CreateServerManager(params IServerInfo[] servers)
    {
        IServerManager serverManager = RecordingDispatchProxy<IServerManager>.Create(out RecordingDispatchProxy<IServerManager> serverManagerProxy);
        serverManagerProxy.SetProperty(nameof(IServerManager.Servers), servers.ToImmutableList());
        serverManagerProxy.SetProperty(nameof(IServerManager.ServerMessages), ImmutableList<IServerMessageInfo>.Empty);
        return serverManager;
    }

    private static IServerInfo CreateServer(byte realmId, bool isOnline)
    {
        IServerInfo server = RecordingDispatchProxy<IServerInfo>.Create(out RecordingDispatchProxy<IServerInfo> serverProxy);
        serverProxy.SetProperty(nameof(IServerInfo.Model), new ServerModel
        {
            Id   = realmId,
            Name = $"Realm {realmId}",
            Port = 24000,
            Type = (byte)RealmType.PVE
        });
        serverProxy.SetProperty(nameof(IServerInfo.IsOnline), isOnline);
        return server;
    }

    private static ClientSelectRealm CreateSelectRealm(uint realmId)
    {
        var message = (ClientSelectRealm)RuntimeHelpers.GetUninitializedObject(typeof(ClientSelectRealm));
        SetAutoProperty(message, nameof(ClientSelectRealm.RealmId), realmId);
        return message;
    }

    private static void SetAutoProperty<T>(object instance, string propertyName, T value)
    {
        instance.GetType().GetProperty(propertyName)!.SetValue(instance, value);
    }
}
