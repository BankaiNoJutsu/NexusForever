using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Text.Filter;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Session;
using NexusForever.Network.World.Entity.Model;
using NexusForever.Network.World.Message.Model.Entity;
using NexusForever.Network.World.Message.Model.Pet;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Pet;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Entity;

public partial class PathManagerTests
{
    [Fact]
    public void ScannerName_DeployUsesStoredProfileNameBeforeMapAdmission()
    {
        NameFixture f = CreateNameFixture();
        f.Map.SetMethodHandler(nameof(IBaseMap.CanEnter), _ =>
        {
            Assert.Equal("Vector", f.Bot.Model().Name);
            return true;
        });

        Assert.True(f.Path.TryDeployScientistScanbot(77));
        Assert.Equal("Vector", f.Bot.Model().Name);
        Assert.Empty(f.Names());
    }

    [Fact]
    public void ScannerName_PublicRenameWhilePendingChangesCreateModelWithoutZeroGuidUpdate()
    {
        NameFixture f = CreateNameFixture();
        Assert.True(f.Path.TryDeployScientistScanbot(77));

        f.Rename(77, "Renamed");

        Assert.Equal("Renamed", f.Pets.GetCustomisation(PetType.ScanBot, 77).Name);
        Assert.Equal("Renamed", f.Bot.Model().Name);
        Assert.Empty(f.Names());
        Assert.Equal("Renamed", Assert.Single(f.Messages<ServerPetCustomisation>()).PetCustomisation.PetName);
    }

    [Fact]
    public void ScannerName_PublicRenameUpdatesOwnerObserversAndLateCreateModel()
    {
        NameFixture f = CreateNameFixture();
        f.Activate();
        IGameSession observerSession = RecordingDispatchProxy<IGameSession>.Create(out var observerMessages);
        IPlayer observer = RecordingDispatchProxy<IPlayer>.Create(out var observerProxy);
        observerProxy.SetProperty(nameof(IPlayer.Guid), 501u);
        observerProxy.SetProperty(nameof(IPlayer.Session), observerSession);
        f.Bot.AddVisible(observer);

        f.Rename(77, "Véctor 星");

        ServerUnitNameChange update = Assert.Single(f.Names());
        Assert.Equal(9001u, update.UnitId);
        Assert.Equal("Véctor 星", update.Name);
        Assert.Same(update, Assert.Single(observerMessages.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted))).Arguments[0]);
        Assert.Equal("Véctor 星", f.Bot.Model().Name);
    }

    [Fact]
    public void ScannerName_RenamingAnotherUnlockedProfilePreservesActiveName()
    {
        NameFixture f = CreateNameFixture(otherProfile: true);
        f.Activate();

        f.Rename(78, "Other");

        Assert.Equal("Other", f.Pets.GetCustomisation(PetType.ScanBot, 78).Name);
        Assert.Equal("Vector", f.Bot.Model().Name);
        Assert.Empty(f.Names());
    }

    [Theory]
    [InlineData("locked")]
    [InlineData("filtered")]
    public void ScannerName_RejectedRequestPreservesProfileAndEntity(string refusal)
    {
        NameFixture f = CreateNameFixture();
        f.Activate();
        if (refusal == "filtered")
            f.Filter.SetMethodHandler(nameof(ITextFilterManager.IsTextValid), _ => false);

        Assert.Throws<InvalidPacketValueException>(() => f.Rename(refusal == "locked" ? 78u : 77u, "Rejected"));

        Assert.Equal("Vector", f.Pets.GetCustomisation(PetType.ScanBot, 77).Name);
        Assert.Equal("Vector", f.Bot.Model().Name);
        Assert.Empty(f.Names());
        Assert.Empty(f.Messages<ServerPetCustomisation>());
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("identity")]
    [InlineData("missing")]
    [InlineData("dismissed")]
    [InlineData("replacement")]
    public void ScannerName_StaleOrDismissingEntityIsNotRenamed(string stale)
    {
        NameFixture f = CreateNameFixture();
        f.Activate();
        switch (stale)
        {
            case "owner": f.Bot.SummonerGuid = 777u; break;
            case "identity": f.Bot.SetIdentity(9002u); break;
            case "missing": f.Map.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => null); break;
            case "dismissed": Assert.True(f.Path.DismissScientistScanbot()); break;
            case "replacement":
                var replacement = new NameScanner { SummonerGuid = f.Owner.Guid };
                replacement.Enter(f.Owner.Map);
                f.Map.SetMethodHandler(nameof(IBaseMap.GetEntity), _ => replacement);
                break;
        }

        f.Rename(77, "Accepted profile");

        Assert.Equal("Accepted profile", f.Pets.GetCustomisation(PetType.ScanBot, 77).Name);
        Assert.Equal("Vector", f.Bot.Model().Name);
        Assert.Empty(f.Names());
    }

    [Fact]
    public void ScannerName_RepeatingAcceptedNameDoesNotRepublishUnitUpdate()
    {
        NameFixture f = CreateNameFixture();
        f.Activate();
        f.Rename(77, "Renamed");
        f.Rename(77, "Renamed");

        Assert.Single(f.Names());
        Assert.Equal(2, f.Messages<ServerPetCustomisation>().Count);
    }

    [Fact]
    public void ScannerName_UnsummonedEntityKeepsItsNameAndRedeployUsesLatestAcceptedName()
    {
        NameFixture f = CreateNameFixture();
        f.Activate();
        Assert.True(f.Path.OnScientistScanbotUnsummoned(f.Bot));
        f.Bot.Leave();
        f.Rename(77, "Next deployment");
        Assert.Equal("Vector", f.Bot.Model().Name);
        Assert.Empty(f.Names());

        Assert.True(f.Path.TryDeployScientistScanbot(77));
        Assert.Equal("Next deployment", f.Bot.Model().Name);
    }

    [Fact]
    public void ScannerName_PacketKeepsNativeLayoutAndLegacyWriterRegistration()
    {
        var message = new ServerUnitNameChange { UnitId = 0x89abcdef, Name = "Véctor 星" };
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            message.Write(writer);
            writer.Write((byte)0xa5);
            writer.FlushBits();
        }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        Assert.Equal(0x89abcdefu, reader.ReadUInt());
        Assert.Equal("Véctor 星", reader.ReadWideString());
        Assert.Equal((byte)0xa5, reader.ReadByte());
        Assert.Throws<EndOfStreamException>(() => reader.ReadBit());
        var registry = new MessageManager(NullLogger<MessageManager>.Instance);
        registry.RegisterMessage(typeof(ServerUnitNameChange));
        registry.RegisterMessage(typeof(ServerEntityStatUInt32WideString));
        Assert.Equal(GameMessageOpcode.ServerEntityStatUInt32WideString, registry.GetOpcode(message));
        Assert.Equal(GameMessageOpcode.ServerEntityStatUInt32WideString,
            registry.GetOpcode(new ServerEntityStatUInt32WideString { Value = message.UnitId, Text = message.Name }));
        Assert.Equal((ushort)0x08cc, (ushort)registry.GetOpcode(message).Value);
    }

    private NameFixture CreateNameFixture(bool otherProfile = false)
    {
        BuildScanbotGameTableProvider();
        var scanner = new NameScanner();
        scanner.InitialiseRuntimeDependencies(null, null, null, () => configuredGameTableManager);
        PathManager path = CreateManager(Path.Scientist, 0, 1, out IPlayer owner, out var player,
            out var messages, out _, out _, entityFactory: CreateEntityFactory(scanner, out _));
        ConfigureScanbotPlayer(player, scanner, true, out var map);
        ITextFilterManager filter = RecordingDispatchProxy<ITextFilterManager>.Create(out var filterProxy);
        filterProxy.SetMethodHandler(nameof(ITextFilterManager.IsTextValid), _ => true);
        var model = new CharacterModel { Id = 42 };
        model.PetCustomisation.Add(new CharacterPetCustomisationModel { Id = 42, Type = (byte)PetType.ScanBot, ObjectId = 77, Name = "Vector" });
        if (otherProfile)
            model.PetCustomisation.Add(new CharacterPetCustomisationModel { Id = 42, Type = (byte)PetType.ScanBot, ObjectId = 78, Name = "Other profile" });
        var pets = new PetCustomisationManager(owner, model, configuredGameTableManager, filter);
        player.SetProperty(nameof(IPlayer.PetCustomisationManager), pets);
        scanner.AddVisible(owner);
        return new NameFixture(path, pets, scanner, owner, messages, map, filterProxy);
    }

    private sealed record NameFixture(PathManager Path, PetCustomisationManager Pets, NameScanner Bot, IPlayer Owner,
        RecordingDispatchProxy<IGameSession> MessagesProxy, RecordingDispatchProxy<IBaseMap> Map,
        RecordingDispatchProxy<ITextFilterManager> Filter)
    {
        public void Activate()
        {
            Assert.True(Path.TryDeployScientistScanbot(77));
            Bot.Enter(Owner.Map);
            Assert.True(Path.OnScientistScanbotSummoned(Bot));
        }

        public void Rename(uint profile, string name)
        {
            using var stream = new MemoryStream();
            using (var writer = new GamePacketWriter(stream))
            {
                writer.Write(PetType.ScanBot, 2u);
                writer.Write(profile);
                writer.WriteStringWide(name);
                writer.FlushBits();
            }
            using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
            var packet = new ClientPathScientistSetScannerName();
            packet.Read(reader);
            IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out var proxy);
            proxy.SetProperty(nameof(IWorldSession.Player), Owner);
            new ClientPathScientistSetScannerNameHandler().HandleMessage(session, packet);
        }

        public IReadOnlyList<T> Messages<T>() => GetEncryptedMessages<T>(MessagesProxy);
        public IReadOnlyList<ServerUnitNameChange> Names() => Messages<ServerUnitNameChange>();
    }

    private sealed class NameScanner() : ScannerUnitEntity(RecordingDispatchProxy<IMovementManager>.Create(out _))
    {
        public ScannerUnitEntityModel Model() => (ScannerUnitEntityModel)BuildEntityModel();
        public void SetIdentity(uint id) => Guid = id;
        public void Enter(IBaseMap map)
        {
            Guid = 9001;
            typeof(GridEntity).GetProperty(nameof(Map)).SetValue(this, map);
        }
        public void Leave()
        {
            Guid = 0;
            typeof(GridEntity).GetProperty(nameof(Map)).SetValue(this, null);
        }
    }
}
