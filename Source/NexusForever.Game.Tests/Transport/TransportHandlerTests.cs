using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using NexusForever.Database.Auth.Model;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Entity;
using NexusForever.Game.Map;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.Packet;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Chat;
using NexusForever.Network.World.Message.Model.Info;
using NexusForever.Network.World.Message.Model.Pregame;
using NexusForever.Network.World.Message.Static;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;
using NexusForever.Shared.Game.Events;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity.Player;
using NexusForever.WorldServer.Network.Message.Handler.Entity.Vehicle;

namespace NexusForever.Game.Tests.Transport;

public partial class TransportHandlerTests
{
    private const uint RapidTransportSpellGameFormulaId = 0x051Bu;
    private const uint RapidTransportSpell4Id         = 82922u;

    [Fact]
    public void RapidTransport_RejectsUnknownDestinationNode()
    {
        ClientRapidTransportHandler handler = CreateRapidTransportHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out _,
            out _,
            out _,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiNode), CreateGameTable<TaxiNodeEntry>());
        tableProxy.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry
        {
            Id        = RapidTransportSpellGameFormulaId,
            Dataint0  = RapidTransportSpell4Id
        }));

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);

        handler.HandleMessage(session, BuildRapidTransport(destinationNodeId: 999));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        ServerSpellCastResult result = GetEncryptedMessages(session)
            .OfType<ServerSpellCastResult>()
            .Single();
        Assert.Equal(0u, result.Spell4Id);
        Assert.Equal(CastResult.RapidTransportInvalid, result.CastResult);
    }

    [Fact]
    public void RapidTransport_RejectsInsufficientCredits()
    {
        ClientRapidTransportHandler handler = CreateRapidTransportHandler(
            out _,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _,
            out _,
            out _);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), false);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildRapidTransport(destinationNodeId: 20));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        ServerSpellCastResult result = GetEncryptedMessages(session)
            .OfType<ServerSpellCastResult>()
            .Single();
        Assert.Equal(RapidTransportSpell4Id, result.Spell4Id);
        Assert.Equal(CastResult.CasterVitalCostMoney, result.CastResult);
    }

    [Fact]
    public void RapidTransport_RejectsWhenTaxiNodeTableUnavailable()
    {
        ClientRapidTransportHandler handler = CreateRapidTransportHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _,
            out _,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiNode), null);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildRapidTransport(destinationNodeId: 20, contextToken: 0x12345678u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        ServerSpellCastResult result = GetEncryptedMessages(session)
            .OfType<ServerSpellCastResult>()
            .Single();
        Assert.Equal(0x12345678u, result.ContextToken);
        Assert.Equal(0u, result.Spell4Id);
        Assert.Equal(CastResult.RapidTransportInvalid, result.CastResult);
    }

    [Fact]
    public void RapidTransport_RejectsWhenTaxiRouteTableUnavailable()
    {
        ClientRapidTransportHandler handler = CreateRapidTransportHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out _,
            out _,
            out _,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiRoute), null);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildRapidTransport(destinationNodeId: 20));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        ServerSpellCastResult result = GetEncryptedMessages(session)
            .OfType<ServerSpellCastResult>()
            .Single();
        Assert.Equal(RapidTransportSpell4Id, result.Spell4Id);
        Assert.Equal(CastResult.RapidTransportInvalid, result.CastResult);
    }

    [Fact]
    public void RapidTransport_RejectsWhenWorldLocationTableUnavailable()
    {
        ClientRapidTransportHandler handler = CreateRapidTransportHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _,
            out _,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), null);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildRapidTransport(destinationNodeId: 20));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        ServerSpellCastResult result = GetEncryptedMessages(session)
            .OfType<ServerSpellCastResult>()
            .Single();
        Assert.Equal(0u, result.Spell4Id);
        Assert.Equal(CastResult.RapidTransportInvalid, result.CastResult);
    }

    [Fact]
    public void RapidTransport_RejectsWhenSpellFormulaUnavailable()
    {
        ClientRapidTransportHandler handler = CreateRapidTransportHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _,
            out _,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.GameFormula), null);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildRapidTransport(destinationNodeId: 20));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        ServerSpellCastResult result = GetEncryptedMessages(session)
            .OfType<ServerSpellCastResult>()
            .Single();
        Assert.Equal(0u, result.Spell4Id);
        Assert.Equal(CastResult.RapidTransportInvalid, result.CastResult);
    }

    [Fact]
    public void RapidTransport_RejectsWhenSpellFormulaHasNoSpell()
    {
        ClientRapidTransportHandler handler = CreateRapidTransportHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _,
            out _,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry
        {
            Id       = RapidTransportSpellGameFormulaId,
            Dataint0 = 0u
        }));

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildRapidTransport(destinationNodeId: 20));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        ServerSpellCastResult result = GetEncryptedMessages(session)
            .OfType<ServerSpellCastResult>()
            .Single();
        Assert.Equal(0u, result.Spell4Id);
        Assert.Equal(CastResult.RapidTransportInvalid, result.CastResult);
    }

    [Theory]
    [InlineData(CastResult.Ok)]
    [InlineData(CastResult.SpellGlobalCooldown)]
    [InlineData(CastResult.RapidTransportInvalid)]
    [InlineData(CastResult.CasterVitalCostMoney)]
    public void RapidTransport_ChargesTheSelectedFareOnlyForAnAcceptedCast(CastResult expected)
    {
        ClientRapidTransportHandler handler = CreateRapidTransportHandler(
            out _,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out RecordingDispatchProxy<ISpellManager> spellManager,
            out _,
            out _);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), expected != CastResult.RapidTransportInvalid);
        spellManager.SetMethodReturn(nameof(ISpellManager.GetGlobalSpellCooldown), expected == CastResult.SpellGlobalCooldown ? 1d : 0d);
        ulong balance = 100ul;
        currencyProxy.SetMethodHandler(nameof(ICurrencyManager.CanAfford), args => balance >= (ulong)args[1]);
        currencyProxy.SetMethodHandler(nameof(ICurrencyManager.CurrencySubtractAmount), args =>
        {
            Assert.Equal(CurrencyType.Credits, args[0]);
            balance -= (ulong)args[1];
            return null;
        });
        playerProxy.SetMethodHandler(nameof(IPlayer.EnqueueToVisible), args =>
        {
            if (args[0] is ServerSpellStart)
                Assert.Equal(75ul, balance);
            return null;
        });

        var info = RecordingDispatchProxy<ISpellInfo>.Create(out var spellInfo);
        var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var spellBase);
        spellBase.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 12 });
        spellInfo.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellInfo.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = RapidTransportSpell4Id, Spell4BaseIdBaseSpell = 12, CastTime = 1000 });
        spellInfo.SetProperty(nameof(ISpellInfo.GlobalCooldown), new SpellCoolDownEntry { Id = 7, CooldownTime = 1000 });
        spellInfo.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());
        spellInfo.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>());
        var global = RecordingDispatchProxy<IGlobalSpellManager>.Create(out var globalSpellManager);
        globalSpellManager.SetProperty(nameof(IGlobalSpellManager.NextCastingId), 77u);
        var scripts = RecordingDispatchProxy<IScriptManager>.Create(out var scriptManager);
        scriptManager.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), RecordingDispatchProxy<IScriptCollection>.Create(out _));

        CastResult? castResult = null;
        playerProxy.SetMethodHandler(nameof(IUnitEntity.CastSpell), args =>
        {
            var parameters = Assert.IsAssignableFrom<ISpellParameters>(args[1]);
            parameters.SpellInfo = info;
            // Model the balance changing after the handler's quote check.
            if (expected == CastResult.CasterVitalCostMoney)
                balance = 24;
            using var spell = new Game.Spell.Spell(player, parameters, globalSpellManager: global, scriptManager: scripts);
            castResult = spell.Cast();
            return null;
        });

        handler.HandleMessage(session, BuildRapidTransport(destinationNodeId: 20, contextToken: 0xAABBCCDDu));

        RecordingDispatchProxy<IPlayer>.Invocation cast =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        Assert.Equal(RapidTransportSpell4Id, cast.Arguments[0]);
        ISpellParameters parameters = Assert.IsAssignableFrom<ISpellParameters>(cast.Arguments[1]);
        Assert.Equal((ushort)20, parameters.TaxiNode);
        Assert.Equal(0xAABBCCDDu, parameters.ClientContextToken);
        Assert.Equal(nameof(ClientRapidTransport), parameters.ClientRequestSource);

        var debits = currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount));
        if (expected == CastResult.Ok)
        {
            Assert.Equal(25ul, Assert.Single(debits).Arguments[1]);
            Assert.Equal(75ul, balance);
            Assert.Single(session.EncryptedMessages.OfType<ServerSpellCastPositionSync>());
            Assert.Empty(session.EncryptedMessages.OfType<ServerSpellCastResult>());
        }
        else
        {
            Assert.Empty(debits);
            Assert.Equal(expected == CastResult.CasterVitalCostMoney ? 24ul : 100ul, balance);
            Assert.Empty(session.EncryptedMessages.OfType<ServerSpellCastPositionSync>());
            var failure = Assert.Single(session.EncryptedMessages.OfType<ServerSpellCastResult>());
            Assert.Equal(expected, failure.CastResult);
            Assert.Equal(parameters.ClientContextToken, failure.ContextToken);
        }
        Assert.Equal(expected, castResult);
    }

    [Theory]
    [InlineData(false, false, false, null)]
    [InlineData(true, true, false, null)]
    [InlineData(true, false, false, null)]
    [InlineData(true, false, true, null)]
    [InlineData(true, false, false, "before-cast")]
    [InlineData(true, false, false, "after-cast")]
    public void RapidTransport_ValidatesWorldAtCastAndTransfer(bool worldExists, bool disabled, bool disableAfterCast,
        string queuedAdmission)
    {
        CreateRapidTransportHandler(out var tables, out _, out var wallet, out var spells, out _, out _);
        tables.SetProperty(nameof(IGameTableManager.World), worldExists
            ? CreateGameTable(new WorldEntry { Id = 5 }) : CreateGameTable<WorldEntry>());
        var disabledWorlds = RecordingDispatchProxy<IDisableManager>.Create(out var disables);
        disables.SetMethodHandler(nameof(IDisableManager.IsDisabled), args => disabled && (DisableType)args[0] == DisableType.World && (uint)args[1] == 5);
        var movement = RecordingDispatchProxy<IMovementManager>.Create(out var moves);
        var mapManager = RecordingDispatchProxy<IMapManager>.Create(out var maps);
        var player = new Player(movement, null, null, null, null,
            (IGameTableManager)(object)tables, (ICurrencyManager)(object)wallet, disableManager: disabledWorlds,
            mapManager: mapManager) { IsLoading = false };
        var session = new TransportTestSession { Player = player };
        typeof(Player).GetProperty(nameof(IPlayer.Session))!.SetValue(player, session);
        typeof(Player).GetProperty(nameof(IPlayer.Identity))!.SetValue(player, new Game.Abstract.Identity { RealmId = 1, Id = 42 });
        typeof(Player).GetProperty(nameof(IPlayer.SpellManager))!.SetValue(player, (ISpellManager)(object)spells);

        var info = RecordingDispatchProxy<ISpellInfo>.Create(out var spellInfo);
        var baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var spellBase);
        spellBase.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 12 });
        spellInfo.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        spellInfo.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = RapidTransportSpell4Id, Spell4BaseIdBaseSpell = 12, CastTime = 1000 });
        spellInfo.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>());
        spellInfo.SetProperty(nameof(ISpellInfo.Telegraphs), new List<TelegraphDamageEntry>());
        var scripts = RecordingDispatchProxy<IScriptManager>.Create(out var scriptManager);
        scriptManager.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), RecordingDispatchProxy<IScriptCollection>.Create(out _));
        var parameters = new Game.Spell.RapidTransportSpellParameters(25)
        {
            SpellInfo = info, TaxiNode = 20, HasClientCastRequest = true, ClientContextToken = 0xAABBCCDDu
        };
        using var spell = new Game.Spell.Spell(player, parameters,
            globalSpellManager: RecordingDispatchProxy<IGlobalSpellManager>.Create(out _), scriptManager: scripts);

        if (queuedAdmission == "before-cast")
            player.OnEnqueueAddToMap();
        bool available = worldExists && !disabled && queuedAdmission != "before-cast";
        Assert.Equal(available ? CastResult.Ok : CastResult.RapidTransportInvalid, spell.Cast());
        if (available)
        {
            disabled = disableAfterCast;
            var admission = (IPlayerMapAdmission)player;
            if (queuedAdmission == "after-cast")
                player.OnEnqueueAddToMap();
            ulong generation = admission.MapAdmissionGeneration;
            Game.Spell.SpellHandler.HandleEffectRapidTransport(spell, player, null);
            Assert.Equal(25ul, Assert.Single(wallet.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount))).Arguments[1]);
            Assert.Single(session.EncryptedMessages.OfType<ServerSpellCastPositionSync>());
            if (queuedAdmission == "after-cast")
            {
                Assert.Empty(maps.GetInvocations(nameof(IMapManager.AddToMap)));
                Assert.Empty(moves.GetInvocations(nameof(IMovementManager.SetRotation)));
                Assert.True(player.IsLoading);
                Assert.Equal(generation, admission.MapAdmissionGeneration);
            }
            else if (disableAfterCast)
            {
                Assert.Empty(maps.GetInvocations(nameof(IMapManager.AddToMap)));
                Assert.Empty(moves.GetInvocations(nameof(IMovementManager.SetRotation)));
                Assert.Single(session.EncryptedMessages.OfType<ServerChat>());
            }
            else
            {
                Game.Spell.SpellHandler.HandleEffectRapidTransport(spell, player, null);
                var queued = Assert.Single(maps.GetInvocations(nameof(IMapManager.AddToMap)));
                var destination = Assert.IsAssignableFrom<IMapPosition>(queued.Arguments[1]);
                Assert.Equal(5u, destination.Info.Entry.Id);
                Assert.Equal(new Vector3(10, 0, 0), destination.Position);
                Assert.Single(moves.GetInvocations(nameof(IMovementManager.SetRotation)));
            }
        }
        else
        {
            Assert.Empty(wallet.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Empty(maps.GetInvocations(nameof(IMapManager.AddToMap)));
            Assert.Empty(session.EncryptedMessages.OfType<ServerSpellCastPositionSync>());
            if (queuedAdmission == "before-cast")
            {
                Assert.True(player.IsLoading);
                Assert.Empty(session.EncryptedMessages.OfType<ServerSpellCastResult>());
                return;
            }
            var result = Assert.Single(session.EncryptedMessages.OfType<ServerSpellCastResult>());
            Assert.Equal(CastResult.RapidTransportInvalid, result.CastResult);
            Assert.Equal(parameters.ClientContextToken, result.ContextToken);
        }
    }

    [Fact]
    public void FlightPathPurchase_RejectsNonContiguousRouteChain()
    {
        ClientFlightPathPurchaseHandler handler = CreateFlightPathHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out _,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiRoute), CreateGameTable(
            new TaxiRouteEntry { Id = 1u, TaxiNodeIdSource = 10u, TaxiNodeIdDestination = 20u, Price = 5u },
            new TaxiRouteEntry { Id = 2u, TaxiNodeIdSource = 99u, TaxiNodeIdDestination = 30u, Price = 7u }));

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildFlightPathPurchase(1u, 2u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.QueueTaxiFlight)));
        RecordingDispatchProxy<IPlayer>.Invocation error =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Equal(GenericError.EmbarkNoSplineForTaxi, error.Arguments[0]);
    }

    [Fact]
    public void FlightPathPurchase_RejectsWhenTaxiRouteTableUnavailable()
    {
        ClientFlightPathPurchaseHandler handler = CreateFlightPathHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out _,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiRoute), null);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildFlightPathPurchase(1u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.QueueTaxiFlight)));
        RecordingDispatchProxy<IPlayer>.Invocation error =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Equal(GenericError.EmbarkNoSplineForTaxi, error.Arguments[0]);
    }

    [Fact]
    public void FlightPathPurchase_RejectsWhenTaxiNodeTableUnavailable()
    {
        ClientFlightPathPurchaseHandler handler = CreateFlightPathHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiNode), null);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildFlightPathPurchase(1u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.QueueTaxiFlight)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        RecordingDispatchProxy<IPlayer>.Invocation error =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Equal(GenericError.EmbarkNoSplineForTaxi, error.Arguments[0]);
    }

    [Fact]
    public void FlightPathPurchase_RejectsWhenWorldLocationTableUnavailable()
    {
        ClientFlightPathPurchaseHandler handler = CreateFlightPathHandler(
            out RecordingDispatchProxy<IGameTableManager> tableProxy,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _);
        tableProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), null);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildFlightPathPurchase(1u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.QueueTaxiFlight)));
        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        RecordingDispatchProxy<IPlayer>.Invocation error =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Equal(GenericError.EmbarkNoSplineForTaxi, error.Arguments[0]);
    }

    [Fact]
    public void FlightPathPurchase_RejectsInsufficientCredits()
    {
        ClientFlightPathPurchaseHandler handler = CreateFlightPathHandler(
            out _,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), false);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildFlightPathPurchase(1u, 2u));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.QueueTaxiFlight)));
        RecordingDispatchProxy<IPlayer>.Invocation error =
            Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Equal(GenericError.VendorNotEnoughCash, error.Arguments[0]);
    }

    [Fact]
    public void FlightPathPurchase_QueuesValidatedContiguousRouteAndFare()
    {
        ClientFlightPathPurchaseHandler handler = CreateFlightPathHandler(
            out _,
            out IPlayer player,
            out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
            out _);

        TransportTestSession session = CreateSession(player, out RecordingDispatchProxy<IPlayer> playerProxy);
        handler.HandleMessage(session, BuildFlightPathPurchase(1u, 2u));

        Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        var quote = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.QueueTaxiFlight)));
        Assert.Equal(7u, Assert.IsType<WorldEntry>(quote.Arguments[0]).Id);
        Assert.Equal(new Vector3(40, 50, 60), quote.Arguments[1]);
        Assert.Equal(30u, quote.Arguments[3]);
        Assert.Equal(12ul, quote.Arguments[4]);
    }

    [Theory]
    [InlineData("admit", false)]
    [InlineData("deny", false)]
    [InlineData("balance-changed", false)]
    [InlineData("rebound", false)]
    [InlineData("admit", true)]
    [InlineData("deny", true)]
    [InlineData("balance-changed", true)]
    [InlineData("instance-fault", true)]
    [InlineData("grid-fault", false)]
    [InlineData("grid-fault", true)]
    [InlineData("grid-balance-changed", false)]
    [InlineData("bootstrap-fault", false)]
    [InlineData("bootstrap-balance-changed", true)]
    public void FlightPathPurchase_SettlesOnlyOnDestinationAdmission(string outcome, bool instanced)
    {
        var handler = CreateFlightPathHandler(out var tables, out _, out var wallet, out _);
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        var session = new TransportTestSession { Player = player };
        var source = RecordingDispatchProxy<IBaseMap>.Create(out var sourceMap);
        sourceMap.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 5u });
        var kiosk = RecordingDispatchProxy<IWorldEntity>.Create(out var kioskProxy);
        kioskProxy.SetProperty(nameof(IWorldEntity.Map), source);
        kioskProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry { TaxiNodeId = 10u });
        var mapProperty = typeof(GridEntity).GetProperty(nameof(IPlayer.Map))!;
        sourceMap.SetMethodHandler(nameof(IBaseMap.EnqueueRemove), _ => { mapProperty.SetValue(player, null); return null; });
        ulong balance = 100;
        ulong? balanceAtDestination = null;
        ulong? balanceDuringBootstrap = null;
        bool discoveredDuringBootstrap = false;
        int destinationAttempts = 0;
        var destination = new TaxiTestDestination(() =>
        {
            balanceAtDestination = balance;
            if (outcome == "instance-fault" && ++destinationAttempts == 1)
                throw new InvalidOperationException("fixture instance admission failure");
            return outcome == "deny" ? GenericError.InstanceLimitExceeded : null;
        }, failGrid: outcome == "grid-fault", onBootstrap: () =>
        {
            balanceDuringBootstrap = balance;
            discoveredDuringBootstrap = session.EncryptedMessages.OfType<ServerEntitySelectAuxUInt14>().Any();
            if (outcome == "bootstrap-fault") throw new InvalidOperationException("fixture bootstrap failure");
            if (outcome == "bootstrap-balance-changed") balance = 11;
        });
        var factory = RecordingDispatchProxy<IMapFactory>.Create(out var maps);
        IMap destinationRoute = instanced ? new TaxiTestInstancedMap(destination) : destination;
        maps.SetMethodReturn(nameof(IMapFactory.CreateMap), destinationRoute);
        var mapManager = new MapManager(factory);
        var movement = RecordingDispatchProxy<IMovementManager>.Create(out _);
        typeof(Player).GetProperty(nameof(IPlayer.Session))!.SetValue(player, session);
        typeof(Player).GetProperty(nameof(IPlayer.Identity))!.SetValue(player, new Game.Abstract.Identity { RealmId = 1, Id = 42 });
        mapProperty.SetValue(player, source);
        typeof(GridEntity).GetProperty(nameof(IPlayer.Guid))!.SetValue(player, 42u);
        typeof(WorldEntity).GetProperty(nameof(IPlayer.MovementManager))!.SetValue(player, movement);
        SetField(typeof(GridEntity), "visibleEntities", new Dictionary<uint, IGridEntity> { [10] = kiosk });
        SetField(typeof(Player), "gameTableManager", (IGameTableManager)(object)tables);
        SetField(typeof(Player), "mapManager", mapManager);
        SetField(typeof(Player), "taxiStateLock", new object());
        SetField(typeof(Player), "taxiNodes", new HashSet<ushort>());
        SetField(typeof(Player), "unsavedTaxiNodes", new HashSet<ushort>());
        SetField(typeof(Player), "<CurrencyManager>k__BackingField", (ICurrencyManager)(object)wallet);
        wallet.SetMethodHandler(nameof(ICurrencyManager.CanAfford), args => balance >= (ulong)args[1]);
        wallet.SetMethodHandler(nameof(ICurrencyManager.CurrencySubtractAmount), args => { balance -= (ulong)args[1]; return null; });

        handler.HandleMessage(session, BuildFlightPathPurchase(1u, 2u));
        ulong balanceBeforeAdmission = balance;
        bool discoveredBeforeAdmission = session.EncryptedMessages.OfType<ServerEntitySelectAuxUInt14>().Any();

        if (outcome == "balance-changed")
            balance = 11;
        if (outcome == "rebound")
            session.Player = RecordingDispatchProxy<IPlayer>.Create(out _);
        mapManager.Update(0);
        ulong balanceBeforeGrid = balance;
        bool discoveredBeforeGrid = session.EncryptedMessages.OfType<ServerEntitySelectAuxUInt14>().Any();
        if (outcome == "grid-balance-changed") balance = 11;
        mapManager.Update(0);

        if (outcome == "admit")
        {
            Assert.Equal(88ul, balance);
            Assert.Single(wallet.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Equal(30u, Assert.Single(session.EncryptedMessages.OfType<ServerEntitySelectAuxUInt14>()).Value);
            Assert.Equal(new Vector3(40, 50, 60), Assert.Single(destination.AddedPositions));
            Assert.Empty(session.EncryptedMessages.OfType<ServerGenericError>());
        }
        else if (outcome is "bootstrap-fault" or "bootstrap-balance-changed")
        {
            Assert.Equal(outcome == "bootstrap-fault" ? 100ul : 11ul, balance);
            Assert.Empty(wallet.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Empty(session.EncryptedMessages.OfType<ServerEntitySelectAuxUInt14>());
            Assert.Equal(1, session.DisconnectRequests);
            Assert.False(player.IsLoading);
        }
        else if (outcome is "grid-fault" or "grid-balance-changed")
        {
            Assert.Equal(outcome == "grid-fault" ? 100ul : 11ul, balance);
            Assert.Empty(wallet.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Empty(session.EncryptedMessages.OfType<ServerEntitySelectAuxUInt14>());
            Assert.Empty(destination.AddedPositions);
            Assert.Equal(ForceKickReason.WorldDisconnect, Assert.Single(session.EncryptedMessages.OfType<ServerForceKick>()).Reason);
        }
        else
        {
            Assert.Equal(outcome == "balance-changed" ? 11ul : 100ul, balance);
            Assert.Empty(wallet.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Empty(session.EncryptedMessages.OfType<ServerEntitySelectAuxUInt14>());
            Assert.Empty(destination.AddedPositions);
            var errors = session.EncryptedMessages.OfType<ServerGenericError>();
            if (outcome == "rebound")
                Assert.Empty(errors);
            else
                Assert.Equal(outcome switch
                {
                    "deny" => GenericError.InstanceLimitExceeded,
                    "instance-fault" => GenericError.InstanceInvalidDestination,
                    _ => GenericError.VendorNotEnoughCash
                }, Assert.Single(errors).Error);
            Assert.True(player.CanTeleport());
        }
        Assert.Equal(100ul, balanceBeforeAdmission);
        Assert.False(discoveredBeforeAdmission);
        Assert.Equal(outcome == "balance-changed" ? 11ul : 100ul, balanceBeforeGrid);
        Assert.False(discoveredBeforeGrid);
        if (outcome != "rebound")
            Assert.Equal(outcome == "balance-changed" ? 11ul : 100ul, balanceAtDestination);
        if (balanceDuringBootstrap.HasValue)
        {
            Assert.Equal(100ul, balanceDuringBootstrap);
            Assert.False(discoveredDuringBootstrap);
        }

        if (outcome == "instance-fault")
        {
            Assert.Equal(player.Guid, player.ControlGuid);
            handler.HandleMessage(session, BuildFlightPathPurchase(1u, 2u));
            mapManager.Update(0);
            mapManager.Update(0);
            Assert.Equal(88ul, balance);
            Assert.Single(wallet.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
            Assert.Equal(30u, Assert.Single(session.EncryptedMessages.OfType<ServerEntitySelectAuxUInt14>()).Value);
            Assert.Single(destination.AddedPositions);
            Assert.Single(session.EncryptedMessages.OfType<ServerGenericError>());
        }

        void SetField(Type type, string name, object value)
            => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(player, value);
    }

    private sealed class TaxiTestDestination : MapInstance
    {
        private readonly Func<GenericError?> canEnter;
        private readonly bool failGrid;
        private readonly Action onBootstrap;
        public List<Vector3> AddedPositions { get; } = [];

        public TaxiTestDestination(Func<GenericError?> canEnter, bool failGrid, Action onBootstrap)
            : base(null, RecordingDispatchProxy<IPublicEventManager>.Create(out _), sharedConfiguration: CreateConfiguration())
        {
            this.canEnter = canEnter;
            this.failGrid = failGrid;
            this.onBootstrap = onBootstrap;
            var mapLock = RecordingDispatchProxy<IMapLock>.Create(out var mapLockProxy);
            mapLockProxy.SetProperty(nameof(IMapLock.InstanceId), Guid.NewGuid());
            typeof(MapInstance).GetProperty(nameof(MapLock))!.SetValue(this, mapLock);
            Initialise(new WorldEntry { Id = 7 });
        }

        public override void Initialise(WorldEntry entry) => typeof(BaseMap).GetProperty(nameof(Entry))!.SetValue(this, entry);
        public override GenericError? CanEnter(IPlayer player, IMapPosition position) => canEnter();
        protected override void SpawnGrid(uint gridX, uint gridZ)
        {
            if (failGrid) throw new InvalidOperationException("fixture destination grid failure");
        }
        protected override void AddEntity(IGridEntity entity, Vector3 vector)
        {
            var state = (PlayerMapAdmissionState)typeof(Player).GetField("mapAdmissionState", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(entity)!;
            Assert.NotNull(state.BeginBootstrap());
            typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(entity, this);
            onBootstrap();
            AddedPositions.Add(vector);
        }
        protected override IMapPosition GetPlayerReturnLocation(IPlayer player) => null;

        private static ISharedConfiguration CreateConfiguration()
        {
            var configuration = RecordingDispatchProxy<ISharedConfiguration>.Create(out var proxy);
            proxy.SetMethodReturn(nameof(ISharedConfiguration.Get), new MapConfig());
            return configuration;
        }
    }

    private sealed class TaxiTestInstancedMap : InstancedMap<IMapInstance>
    {
        private readonly TaxiTestDestination instance;

        public TaxiTestInstancedMap(TaxiTestDestination instance)
            : base(CreateRouterServices())
        {
            this.instance = instance;
        }

        protected override IMapLock GetMapLock(IPlayer player) => instance.MapLock;
        protected override IMapInstance CreateInstance(IPlayer player, IMapLock lockForInstance) => instance;

        private static IMapManager CreateRouterServices()
        {
            IMapManager manager = RecordingDispatchProxy<IMapManager>.Create(out var proxy);
            proxy.SetMethodReturn(nameof(IMapManager.CanCreateInstance), true);
            return manager;
        }
    }

    [Theory]
    [InlineData("remote-source")]
    [InlineData("missing-source")]
    [InlineData("missing-intermediate")]
    [InlineData("foreign-source")]
    [InlineData("foreign-intermediate")]
    [InlineData("unknown-node-faction")]
    [InlineData("missing-world")]
    [InlineData("oversized-world")]
    [InlineData("nonfinite-position")]
    [InlineData("pending-teleport")]
    public void FlightPathPurchase_RejectsInvalidOriginLegsOrDestinationBeforeCharging(string scenario)
    {
        var handler = CreateFlightPathHandler(out var tables, out var player, out var currency, out var pp);
        var source = new TaxiNodeEntry { Id = 10 };
        var intermediate = new TaxiNodeEntry { Id = 20 };
        var destination = new TaxiNodeEntry { Id = 30, WorldLocation2Id = 3003 };
        switch (scenario)
        {
            case "remote-source": pp.SetMethodReturn(nameof(IPlayer.IsNearTaxiNode), false); break;
            case "missing-source": source.Id = 11; break;
            case "missing-intermediate": intermediate.Id = 21; break;
            case "foreign-source": source.TaxiNodeFactionEnum = 2; break;
            case "foreign-intermediate": intermediate.TaxiNodeFactionEnum = 2; break;
            case "unknown-node-faction": intermediate.TaxiNodeFactionEnum = 99; break;
            case "missing-world": tables.SetProperty(nameof(IGameTableManager.World), null); break;
            case "oversized-world":
            case "nonfinite-position":
                tables.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(new WorldLocation2Entry
                {
                    Id = 3003, WorldId = scenario == "oversized-world" ? 65536u : 7u,
                    Position0 = scenario == "nonfinite-position" ? float.NaN : 0f
                })); break;
            case "pending-teleport": pp.SetMethodReturn(nameof(IPlayer.CanTeleport), false); break;
        }
        tables.SetProperty(nameof(IGameTableManager.TaxiNode), CreateGameTable(source, intermediate, destination));
        handler.HandleMessage(CreateSession(player, out _), BuildFlightPathPurchase(1, 2));
        Assert.Empty(currency.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(pp.GetInvocations(nameof(IPlayer.QueueTaxiFlight)));
        Assert.Empty(pp.GetInvocations(nameof(IPlayer.DiscoverTaxiNode)));
    }

    [Fact]
    public void FlightPathPurchase_RejectsOversizedRouteListWithoutCharge()
    {
        var handler = CreateFlightPathHandler(out _, out var player, out var currency, out var pp);
        handler.HandleMessage(CreateSession(player, out _), BuildFlightPathPurchase(Enumerable.Repeat(1u, 31).ToArray()));
        Assert.Empty(currency.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Empty(pp.GetInvocations(nameof(IPlayer.QueueTaxiFlight)));
    }

    [Fact]
    public void FlightPathPurchase_SumsFaresWithoutUIntOverflow()
    {
        var handler = CreateFlightPathHandler(out var tables, out var player, out var currency, out var pp);
        // Each hop is 2^32 (a float formula of 4294967295 rounds to 4294967296), so the total exceeds what a uint can hold.
        tables.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry { Id = 952u, Datafloat0 = 4294967295f }));
        handler.HandleMessage(CreateSession(player, out _), BuildFlightPathPurchase(1, 2));
        Assert.Empty(currency.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        Assert.Equal(2ul * 4294967296ul, Assert.Single(pp.GetInvocations(nameof(IPlayer.QueueTaxiFlight))).Arguments[4]);
    }

    // The client never reads TaxiRoute.Price on its fare path: per hop trunc((float)T * F * (float)T), T the destination node's ContentTier, F = GameFormula 952
    // Datafloat0 (132.75 in the table and as the compiled-in fallback). Observed fares in dated retail footage: Mistymurk Camp to Illium (tier 1) 1s32c, Illium to Hycrest
    // (tier 3) 11s94c, two such hops 23s88c, Bloodfire Village (tier 2) 5s31c.
    [Theory]
    [InlineData(1u, 1u, 132ul + 132ul)]
    [InlineData(1u, 3u, 132ul + 1194ul)]
    [InlineData(2u, 2u, 531ul + 531ul)]
    [InlineData(3u, 3u, 1194ul + 1194ul)]
    [InlineData(0u, 0u, 0ul)]
    public void FlightPathPurchase_ChargesTheRetailFareFormulaPerDestinationTier(uint tierOfFirstHop, uint tierOfSecondHop, ulong expected)
    {
        var handler = CreateFlightPathHandler(out var tables, out var player, out _, out var pp);
        tables.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry { Id = 952u, Datafloat0 = 132.75f }));
        tables.SetProperty(nameof(IGameTableManager.TaxiNode), CreateGameTable(
            new TaxiNodeEntry { Id = 10u },
            new TaxiNodeEntry { Id = 20u, ContentTier = tierOfFirstHop },
            new TaxiNodeEntry { Id = 30u, WorldLocation2Id = 3003u, ContentTier = tierOfSecondHop }));

        handler.HandleMessage(CreateSession(player, out _), BuildFlightPathPurchase(1u, 2u));

        Assert.Equal(expected, Assert.Single(pp.GetInvocations(nameof(IPlayer.QueueTaxiFlight))).Arguments[4]);
    }

    [Fact]
    public void FlightPathPurchase_UsesTheCompiledInFormulaWhenTheTableRowIsMissing()
    {
        var handler = CreateFlightPathHandler(out var tables, out var player, out _, out var pp);
        tables.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable<GameFormulaEntry>());
        tables.SetProperty(nameof(IGameTableManager.TaxiNode), CreateGameTable(
            new TaxiNodeEntry { Id = 10u },
            new TaxiNodeEntry { Id = 20u, ContentTier = 1u },
            new TaxiNodeEntry { Id = 30u, WorldLocation2Id = 3003u, ContentTier = 3u }));

        handler.HandleMessage(CreateSession(player, out _), BuildFlightPathPurchase(1u, 2u));

        Assert.Equal(132ul + 1194ul, Assert.Single(pp.GetInvocations(nameof(IPlayer.QueueTaxiFlight))).Arguments[4]);
    }

    [Fact]
    public void VehicleEmbark_LogsDecodedRequestWithoutMutatingVehicleState()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 77u);
        TransportTestSession session = CreateSession(player, out _);
        var handler = new ClientVehicleEmbarkHandler(NullLogger<ClientVehicleEmbarkHandler>.Instance);

        handler.HandleMessage(session, BuildVehicleEmbark(0x11111111u, unknown1: 3u, unknown2: 0u));

        Assert.Empty(session.EncryptedMessages);
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.Dismount)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.SendGenericError)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.SetControl)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IWorldEntity.SetPlatform)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IWorldEntity.AddPlatformPassenger)));
    }

    [Fact]
    public void VehicleDisembark_IgnoresRequestWhenPlayerIsNotPlatformed()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.PlatformGuid), null);
        TransportTestSession session = CreateSession(player, out _);
        var handler = new ClientVehicleDisembarkHandler();

        handler.HandleMessage(session, new ClientVehicleDisembark());

        Assert.Empty(session.EncryptedMessages);
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.Dismount)));
    }

    [Fact]
    public void VehicleDisembark_DelegatesMountedPlayerToDismount()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.PlatformGuid), 0x22222222u);
        TransportTestSession session = CreateSession(player, out _);
        var handler = new ClientVehicleDisembarkHandler();

        handler.HandleMessage(session, new ClientVehicleDisembark());

        Assert.Empty(session.EncryptedMessages);
        Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.Dismount)));
    }

    private static ClientRapidTransportHandler CreateRapidTransportHandler(
        out RecordingDispatchProxy<IGameTableManager> tableProxy,
        out IPlayer player,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        out RecordingDispatchProxy<ISpellManager> spellProxy,
        out RecordingDispatchProxy<IBaseMap> mapProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry
        {
            Id       = RapidTransportSpellGameFormulaId,
            Dataint0 = RapidTransportSpell4Id
        }));
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiNode), CreateGameTable(
            new TaxiNodeEntry { Id = 10u, AutoUnlockLevel = 1u, WorldLocation2Id = 1001u },
            new TaxiNodeEntry { Id = 20u, AutoUnlockLevel = 1u, WorldLocation2Id = 1002u }));
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiRoute), CreateGameTable(
            new TaxiRouteEntry { Id = 1u, TaxiNodeIdSource = 10u, TaxiNodeIdDestination = 20u, Price = 25u },
            new TaxiRouteEntry { Id = 2u, TaxiNodeIdSource = 99u, TaxiNodeIdDestination = 20u, Price = 99u }));
        tableProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(
            new WorldLocation2Entry
            {
                Id        = 1001u,
                WorldId   = 5u,
                Position0 = 0f,
                Position1 = 0f,
                Position2 = 0f
            },
            new WorldLocation2Entry
            {
                Id        = 1002u,
                WorldId   = 5u,
                Position0 = 10f,
                Position1 = 0f,
                Position2 = 0f
            }));

        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out spellProxy);
        spellProxy.SetMethodReturn(nameof(ISpellManager.GetSpellCooldown), 0d);

        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 5u });

        player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Level), 50u);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);

        return new ClientRapidTransportHandler(NullLogger<ClientRapidTransportHandler>.Instance, gameTableManager);
    }

    private static ClientFlightPathPurchaseHandler CreateFlightPathHandler(
        out RecordingDispatchProxy<IGameTableManager> tableProxy,
        out IPlayer player,
        out RecordingDispatchProxy<ICurrencyManager> currencyProxy,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiRoute), CreateGameTable(
            new TaxiRouteEntry { Id = 1u, TaxiNodeIdSource = 10u, TaxiNodeIdDestination = 20u, Price = 5u },
            new TaxiRouteEntry { Id = 2u, TaxiNodeIdSource = 20u, TaxiNodeIdDestination = 30u, Price = 7u }));
        // Retail fare per hop is trunc(T * F * T) with T the destination node's ContentTier and F = GameFormula 952 Datafloat0 (TAXI_FARE_NATIVE_READ_20261108.md);
        // tier 1 and F = 6 keep this fixture's two hops at 6 + 6 = 12.
        tableProxy.SetProperty(nameof(IGameTableManager.TaxiNode), CreateGameTable(
            new TaxiNodeEntry { Id = 10u },
            new TaxiNodeEntry { Id = 20u, ContentTier = 1u },
            new TaxiNodeEntry { Id = 30u, WorldLocation2Id = 3003u, ContentTier = 1u }));
        tableProxy.SetProperty(nameof(IGameTableManager.GameFormula), CreateGameTable(new GameFormulaEntry { Id = 952u, Datafloat0 = 6f }));
        tableProxy.SetProperty(nameof(IGameTableManager.World), CreateGameTable(new WorldEntry { Id = 7 }));
        tableProxy.SetProperty(nameof(IGameTableManager.WorldLocation2), CreateGameTable(
            new WorldLocation2Entry
            {
                Id        = 3003u,
                WorldId   = 7u,
                Position0 = 40f,
                Position1 = 50f,
                Position2 = 60f
            }));

        ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out currencyProxy);
        currencyProxy.SetMethodReturn(nameof(ICurrencyManager.CanAfford), true);

        player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanTeleport), true);
        playerProxy.SetMethodReturn(nameof(IPlayer.IsNearTaxiNode), true);
        playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);

        return new ClientFlightPathPurchaseHandler(NullLogger<ClientFlightPathPurchaseHandler>.Instance, gameTableManager);
    }

    private static TransportTestSession CreateSession(
        IPlayer player,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        playerProxy = (RecordingDispatchProxy<IPlayer>)(object)player;
        return new TransportTestSession { Player = player };
    }

    private static ClientRapidTransport BuildRapidTransport(ushort destinationNodeId, uint contextToken = 0u)
    {
        using var stream = new MemoryStream();
        using (var writer = new GamePacketWriter(stream))
        {
            writer.Write(destinationNodeId, 14u);
            writer.Write(contextToken);
            writer.FlushBits();
        }
        using var reader = new GamePacketReader(new MemoryStream(stream.ToArray()));
        var message = new ClientRapidTransport();
        message.Read(reader);
        return message;
    }

    private static ClientFlightPathPurchase BuildFlightPathPurchase(params uint[] routeIds)
    {
        var message = new ClientFlightPathPurchase();
        message.RouteIds.AddRange(routeIds);
        return message;
    }

    private static ClientVehicleEmbark BuildVehicleEmbark(uint vehicleUnitId, uint unknown1, uint unknown2)
    {
        var message = new ClientVehicleEmbark();
        SetPrivateProperty(message, nameof(ClientVehicleEmbark.VehicleUnitId), vehicleUnitId);
        SetPrivateProperty(message, nameof(ClientVehicleEmbark.Unknown1), unknown1);
        SetPrivateProperty(message, nameof(ClientVehicleEmbark.Unknown2), unknown2);
        return message;
    }

    private static IEnumerable<object> GetEncryptedMessages(TransportTestSession session)
    {
        return session.EncryptedMessages;
    }

    private sealed class TransportTestSession : IWorldSession
    {
        public IAccount Account => null;
        public IPlayer Player { get; set; }
        public bool HasSentCharacterListPackets { get; set; }
        public bool HasSentPregameAccountPackets { get; set; }
        public List<CharacterModel> Characters { get; } = [];
        public bool? IsQueued { get; set; }
        public bool CanProcessIncomingPackets { get; set; }
        public bool CanProcessOutgoingPackets { get; set; }
        public string Id { get; private set; } = "transport-handler-test";
        public EventQueue Events { get; } = new();
        public SocketHeartbeat Heartbeat { get; } = new();
        public List<IWritable> EncryptedMessages { get; } = [];
        public int DisconnectRequests { get; private set; }

        public void EnqueueMessage(IWritable message)
        {
        }

        public void EnqueueMessageEncrypted(IWritable message)
        {
            EncryptedMessages.Add(message);
        }

        public void EnqueueMessageEncrypted(uint opcode, string hex)
        {
        }

        public void HandlePacket(ClientGamePacket packet)
        {
        }

        public void FlushPackets()
        {
        }

        public bool CanDispose() => false;

        public void ForceDisconnect()
        {
            DisconnectRequests++;
        }

        public void OnAccept(Socket newSocket)
        {
        }

        public void UpdateId(string id) => Id = id;

        public void Update(double lastTick)
        {
        }

        public void Initialise(AccountModel account)
        {
        }

        public void SetEncryptionKey(byte[] sessionKey)
        {
        }

        public void ArmNextClientSpellEvidenceCapture(bool emitDiagnosticSpellBroadcasts = false)
        {
        }

        public bool TryConsumeNextClientSpellEvidenceCapture(out bool emitDiagnosticSpellBroadcasts)
        {
            emitDiagnosticSpellBroadcasts = false;
            return false;
        }

        public void ArmNextLootEvidenceCapture()
        {
        }

        public bool TryConsumeNextLootEvidenceCapture() => false;

        public void ArmNextAccountRuntimeEvidenceCapture()
        {
        }

        public bool TryConsumeNextAccountRuntimeEvidenceCapture() => false;

        public void ArmNextRewardRotationEvidenceCapture()
        {
        }

        public bool TryConsumeNextRewardRotationEvidenceCapture() => false;
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
        FieldInfo idField = typeof(T).GetField("Id")!;
        return (uint)idField.GetValue(entry)!;
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

    private static void SetPrivateProperty<T>(object instance, string propertyName, T value)
    {
        PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        property.SetValue(instance, value);
    }
}
