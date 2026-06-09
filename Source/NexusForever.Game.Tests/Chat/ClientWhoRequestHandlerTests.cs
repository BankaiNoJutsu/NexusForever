using System.Reflection;
using System.Runtime.CompilerServices;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Reputation;
using NexusForever.Game.Static.Who;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Who;
using NexusForever.Network.World.Message.Model.Who.Parameter;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Chat;
using PlayerClass = NexusForever.Game.Static.Entity.Class;
using PlayerPath = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Chat;

public class ClientWhoRequestHandlerTests
{
    [Fact]
    public void HandleMessage_WithNoParametersReturnsOtherPlayersInCurrentZone()
    {
        IPlayer requester = CreatePlayer(1ul, "Requester", 10u, 12u, Race.Human, PlayerClass.Warrior, PlayerPath.Soldier);
        IPlayer nearby   = CreatePlayer(2ul, "Nearby", 10u, 20u, Race.Aurin, PlayerClass.Esper, PlayerPath.Explorer);
        IPlayer elsewhere = CreatePlayer(3ul, "Elsewhere", 20u, 20u, Race.Granok, PlayerClass.Engineer, PlayerPath.Settler);
        IWorldSession session = CreateSession(requester, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        var handler = new ClientWhoRequestHandler(
            CreatePlayerManager(requester, elsewhere, nearby),
            CreateRealmContext("Test Realm"));

        handler.HandleMessage(session, new ClientWhoRequest());

        ServerWhoResponse response = GetWhoResponse(sessionProxy);
        ServerWhoResponse.WhoPlayer player = Assert.Single(response.Players);
        Assert.Equal(WhoResult.OK, response.Result);
        Assert.Equal("Nearby", player.Name);
        Assert.Equal("Test Realm", player.Realm);
        Assert.Equal(20u, player.Level);
        Assert.Equal(Race.Aurin, player.Race);
        Assert.Equal(PlayerClass.Esper, player.Class);
        Assert.Equal(PlayerPath.Explorer, player.Path);
        Assert.Equal(10u, player.Zone);
    }

    [Fact]
    public void HandleMessage_WithPlayerNameParameterSearchesAcrossZones()
    {
        IPlayer requester = CreatePlayer(1ul, "Requester", 10u, 12u, Race.Human, PlayerClass.Warrior, PlayerPath.Soldier);
        IPlayer alpha    = CreatePlayer(2ul, "Alpha", 20u, 20u, Race.Aurin, PlayerClass.Esper, PlayerPath.Explorer);
        IPlayer alphonse = CreatePlayer(3ul, "Alphonse", 30u, 20u, Race.Mechari, PlayerClass.Medic, PlayerPath.Scientist);
        IPlayer beta     = CreatePlayer(4ul, "Beta", 20u, 20u, Race.Granok, PlayerClass.Engineer, PlayerPath.Settler);
        IWorldSession session = CreateSession(requester, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        var handler = new ClientWhoRequestHandler(
            CreatePlayerManager(requester, beta, alphonse, alpha),
            CreateRealmContext("Test Realm"));

        handler.HandleMessage(session, CreateRequest(CreatePlayerNameParameter("alp")));

        ServerWhoResponse response = GetWhoResponse(sessionProxy);
        Assert.Equal(["Alpha", "Alphonse"], response.Players.Select(player => player.Name));
    }

    [Fact]
    public void HandleMessage_WithLevelParameterTreatsTopLevelAsExclusive()
    {
        IPlayer requester = CreatePlayer(1ul, "Requester", 10u, 9u, Race.Human, PlayerClass.Warrior, PlayerPath.Soldier);
        IPlayer inside    = CreatePlayer(2ul, "Inside", 20u, 19u, Race.Aurin, PlayerClass.Esper, PlayerPath.Explorer);
        IPlayer atTop     = CreatePlayer(3ul, "AtTop", 20u, 20u, Race.Mechari, PlayerClass.Medic, PlayerPath.Scientist);
        IWorldSession session = CreateSession(requester, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        var handler = new ClientWhoRequestHandler(
            CreatePlayerManager(requester, atTop, inside),
            CreateRealmContext("Test Realm"));

        handler.HandleMessage(session, CreateRequest(CreateLevelParameter(10u, 20u)));

        ServerWhoResponse response = GetWhoResponse(sessionProxy);
        ServerWhoResponse.WhoPlayer player = Assert.Single(response.Players);
        Assert.Equal("Inside", player.Name);
    }

    [Fact]
    public void HandleMessage_WithParameterGroupCountsUsesCumulativeOffsets()
    {
        IPlayer requester = CreatePlayer(1ul, "Requester", 10u, 12u, Race.Human, PlayerClass.Warrior, PlayerPath.Soldier);
        IPlayer aurinEsper = CreatePlayer(2ul, "AurinEsper", 20u, 20u, Race.Aurin, PlayerClass.Esper, PlayerPath.Explorer);
        IPlayer granokSettler = CreatePlayer(3ul, "GranokSettler", 30u, 20u, Race.Granok, PlayerClass.Engineer, PlayerPath.Settler);
        IPlayer remoteName = CreatePlayer(4ul, "RemoteName", 40u, 20u, Race.Mechari, PlayerClass.Medic, PlayerPath.Scientist);
        IPlayer aurinMedic = CreatePlayer(5ul, "AurinMedic", 20u, 20u, Race.Aurin, PlayerClass.Medic, PlayerPath.Explorer);
        IWorldSession session = CreateSession(requester, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        var handler = new ClientWhoRequestHandler(
            CreatePlayerManager(requester, remoteName, aurinMedic, granokSettler, aurinEsper),
            CreateRealmContext("Test Realm"));
        ClientWhoRequest request = CreateRequest(
            CreateRaceParameter(Race.Aurin),
            CreateClassParameter(PlayerClass.Esper),
            CreateRaceParameter(Race.Granok),
            CreatePathParameter(PlayerPath.Settler),
            CreatePlayerNameParameter("remote"));
        request.ParameterGroupCounts.Clear();
        request.ParameterGroupCounts.AddRange([2, 4]);

        handler.HandleMessage(session, request);

        ServerWhoResponse response = GetWhoResponse(sessionProxy);
        Assert.Equal(["AurinEsper", "GranokSettler", "RemoteName"], response.Players.Select(player => player.Name));
    }

    [Fact]
    public void HandleMessage_WithComboPathCanMatchSoldier()
    {
        IPlayer requester = CreatePlayer(1ul, "Requester", 10u, 12u, Race.Human, PlayerClass.Warrior, PlayerPath.Explorer);
        IPlayer soldier   = CreatePlayer(2ul, "SoldierPath", 20u, 20u, Race.Aurin, PlayerClass.Esper, PlayerPath.Soldier);
        IPlayer settler   = CreatePlayer(3ul, "SettlerPath", 20u, 20u, Race.Granok, PlayerClass.Engineer, PlayerPath.Settler);
        IWorldSession session = CreateSession(requester, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        var handler = new ClientWhoRequestHandler(
            CreatePlayerManager(requester, settler, soldier),
            CreateRealmContext("Test Realm"));

        handler.HandleMessage(session, CreateRequest(CreateComboParameter("soldier", Race.None, PlayerPath.Soldier, PlayerClass.None, 0u)));

        ServerWhoResponse response = GetWhoResponse(sessionProxy);
        ServerWhoResponse.WhoPlayer player = Assert.Single(response.Players);
        Assert.Equal("SoldierPath", player.Name);
    }

    [Fact]
    public void HandleMessage_WithComboRequiresAllProvidedFields()
    {
        IPlayer requester = CreatePlayer(1ul, "Requester", 10u, 12u, Race.Human, PlayerClass.Warrior, PlayerPath.Explorer);
        IPlayer match     = CreatePlayer(2ul, "MatchingEsper", 40u, 20u, Race.Aurin, PlayerClass.Esper, PlayerPath.Soldier);
        IPlayer wrongRace  = CreatePlayer(3ul, "MatchingRace", 40u, 20u, Race.Human, PlayerClass.Esper, PlayerPath.Soldier);
        IPlayer wrongPath  = CreatePlayer(4ul, "MatchingPath", 40u, 20u, Race.Aurin, PlayerClass.Esper, PlayerPath.Explorer);
        IPlayer wrongClass = CreatePlayer(5ul, "MatchingClass", 40u, 20u, Race.Aurin, PlayerClass.Engineer, PlayerPath.Soldier);
        IPlayer wrongZone  = CreatePlayer(6ul, "MatchingZone", 50u, 20u, Race.Aurin, PlayerClass.Esper, PlayerPath.Soldier);
        IPlayer wrongName  = CreatePlayer(7ul, "OtherName", 40u, 20u, Race.Aurin, PlayerClass.Esper, PlayerPath.Soldier);
        IWorldSession session = CreateSession(requester, out RecordingDispatchProxy<IWorldSession> sessionProxy);

        var handler = new ClientWhoRequestHandler(
            CreatePlayerManager(requester, wrongName, wrongZone, wrongClass, wrongPath, wrongRace, match),
            CreateRealmContext("Test Realm"));

        handler.HandleMessage(session, CreateRequest(CreateComboParameter("matching", Race.Aurin, PlayerPath.Soldier, PlayerClass.Esper, 40u)));

        ServerWhoResponse response = GetWhoResponse(sessionProxy);
        ServerWhoResponse.WhoPlayer player = Assert.Single(response.Players);
        Assert.Equal("MatchingEsper", player.Name);
    }

    private static IWorldSession CreateSession(IPlayer player, out RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out sessionProxy);
        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        return session;
    }

    private static IPlayerManager CreatePlayerManager(params IPlayer[] players)
    {
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out RecordingDispatchProxy<IPlayerManager> proxy);
        proxy.SetMethodHandler("GetEnumerator", _ => players.AsEnumerable().GetEnumerator());
        return playerManager;
    }

    private static IRealmContext CreateRealmContext(string realmName)
    {
        IRealmContext realmContext = RecordingDispatchProxy<IRealmContext>.Create(out RecordingDispatchProxy<IRealmContext> proxy);
        proxy.SetProperty(nameof(IRealmContext.RealmName), realmName);
        return realmContext;
    }

    private static IPlayer CreatePlayer(
        ulong characterId,
        string name,
        uint zoneId,
        uint level,
        Race race,
        PlayerClass playerClass,
        PlayerPath path)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> proxy);
        proxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        proxy.SetProperty(nameof(IPlayer.Name), name);
        proxy.SetProperty(nameof(IWorldEntity.Zone), new WorldZoneEntry { Id = zoneId });
        proxy.SetProperty(nameof(IWorldEntity.Level), level);
        proxy.SetProperty(nameof(IPlayer.Race), race);
        proxy.SetProperty(nameof(IPlayer.Class), playerClass);
        proxy.SetProperty(nameof(IPlayer.Path), path);
        proxy.SetProperty(nameof(IWorldEntity.Faction1), Faction.Exile);
        proxy.SetProperty(nameof(IPlayer.Sex), Sex.Female);
        proxy.SetProperty(nameof(IPlayer.ClientGroupAssociation), 0x8000000000000000ul | characterId);
        return player;
    }

    private static ClientWhoRequest CreateRequest(params WhoParameter[] parameters)
    {
        var request = new ClientWhoRequest();
        request.Parameters.AddRange(parameters);
        request.ParameterGroupCounts.Add(parameters.Length);
        return request;
    }

    private static WhoParameter CreateLevelParameter(uint bottomLevel, uint topLevel)
    {
        var data = (WhoParameterLevel)RuntimeHelpers.GetUninitializedObject(typeof(WhoParameterLevel));
        SetProperty(data, nameof(WhoParameterLevel.BottomLevel), bottomLevel);
        SetProperty(data, nameof(WhoParameterLevel.TopLevel), topLevel);
        return CreateParameter(WhoParameterType.Level, data);
    }

    private static WhoParameter CreateRaceParameter(Race race)
    {
        var data = (WhoParameterRace)RuntimeHelpers.GetUninitializedObject(typeof(WhoParameterRace));
        SetProperty(data, nameof(WhoParameterRace.RaceId), race);
        return CreateParameter(WhoParameterType.Race, data);
    }

    private static WhoParameter CreateClassParameter(PlayerClass playerClass)
    {
        var data = (WhoParameterClass)RuntimeHelpers.GetUninitializedObject(typeof(WhoParameterClass));
        SetProperty(data, nameof(WhoParameterClass.ClassId), playerClass);
        return CreateParameter(WhoParameterType.Class, data);
    }

    private static WhoParameter CreatePathParameter(PlayerPath path)
    {
        var data = (WhoParameterPath)RuntimeHelpers.GetUninitializedObject(typeof(WhoParameterPath));
        SetProperty(data, nameof(WhoParameterPath.PathId), path);
        return CreateParameter(WhoParameterType.Path, data);
    }

    private static WhoParameter CreatePlayerNameParameter(string name)
    {
        var data = (WhoParameterPlayer)RuntimeHelpers.GetUninitializedObject(typeof(WhoParameterPlayer));
        SetProperty(data, nameof(WhoParameterPlayer.PlayerName), name);
        return CreateParameter(WhoParameterType.Player, data);
    }

    private static WhoParameter CreateComboParameter(
        string searchText,
        Race race,
        PlayerPath path,
        PlayerClass playerClass,
        uint worldZoneId)
    {
        var data = (WhoParameterCombo)RuntimeHelpers.GetUninitializedObject(typeof(WhoParameterCombo));
        SetProperty(data, nameof(WhoParameterCombo.SearchString), searchText);
        SetProperty(data, nameof(WhoParameterCombo.RaceId), race);
        SetProperty(data, nameof(WhoParameterCombo.PathId), path);
        SetProperty(data, nameof(WhoParameterCombo.ClassId), playerClass);
        SetProperty(data, nameof(WhoParameterCombo.WorldZoneId), worldZoneId);
        return CreateParameter(WhoParameterType.Combo, data);
    }

    private static WhoParameter CreateParameter(WhoParameterType type, IWhoParameterData data)
    {
        var parameter = (WhoParameter)RuntimeHelpers.GetUninitializedObject(typeof(WhoParameter));
        SetProperty(parameter, nameof(WhoParameter.Type), type);
        SetProperty(parameter, nameof(WhoParameter.Data), data);
        return parameter;
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(target, value);
    }

    private static ServerWhoResponse GetWhoResponse(RecordingDispatchProxy<IWorldSession> sessionProxy)
    {
        RecordingDispatchProxy<IWorldSession>.Invocation invocation =
            Assert.Single(sessionProxy.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        return Assert.IsType<ServerWhoResponse>(invocation.Arguments[0]);
    }
}
