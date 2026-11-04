using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network.Message.Handler.Spell;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity.Player
{
    public class ClientRapidTransportHandler : IMessageHandler<IWorldSession, ClientRapidTransport>
    {
        private const uint RapidTransportSpellGameFormulaId = 0x051Bu;

        #region Dependency Injection

        private readonly ILogger<ClientRapidTransportHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientRapidTransportHandler(
            ILogger<ClientRapidTransportHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log = log;
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientRapidTransport rapidTransport)
        {
            IPlayer player = session.Player;
            if (player == null)
                return;

            log.LogTrace("Received rapid transport request from player {PlayerGuid}: TaxiNode={TaxiNode}, ContextToken={ContextToken}.",
                player.Guid, rapidTransport.TaxiNode, rapidTransport.ContextToken);

            if (gameTableManager.TaxiNode == null)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: taxi node table is unavailable.",
                    player.Guid);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            TaxiNodeEntry taxiNode = gameTableManager.TaxiNode.GetEntry(rapidTransport.TaxiNode);
            if (taxiNode == null)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: destination taxi node {TaxiNode} was not found.",
                    player.Guid, rapidTransport.TaxiNode);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            if (player.Level < taxiNode.AutoUnlockLevel)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: level {PlayerLevel} is below required {RequiredLevel} for taxi node {TaxiNode}.",
                    player.Guid, player.Level, taxiNode.AutoUnlockLevel, rapidTransport.TaxiNode);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            if (gameTableManager.WorldLocation2 == null)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: world location table is unavailable.",
                    player.Guid);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            WorldLocation2Entry worldLocation = gameTableManager.WorldLocation2.GetEntry(taxiNode.WorldLocation2Id);
            if (worldLocation == null)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: world location {WorldLocation2Id} for taxi node {TaxiNode} was not found.",
                    player.Guid, taxiNode.WorldLocation2Id, rapidTransport.TaxiNode);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            GameFormulaEntry formula = gameTableManager.GameFormula?.GetEntry(RapidTransportSpellGameFormulaId);
            if (formula == null || formula.Dataint0 == 0u)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: rapid transport spell formula {GameFormulaId} is unavailable or empty.",
                    player.Guid, RapidTransportSpellGameFormulaId);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            if (player.SpellManager.GetSpellCooldown(formula.Dataint0) > 0d)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: spell {Spell4Id} is on cooldown.",
                    player.Guid, formula.Dataint0);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, formula.Dataint0, CastResult.SpellCooldown);
                return;
            }

            TaxiRouteEntry route = ResolveRapidTransportRoute(player, rapidTransport.TaxiNode);
            if (route == null)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: no route found to destination taxi node {TaxiNode}.",
                    player.Guid, rapidTransport.TaxiNode);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, formula.Dataint0, CastResult.RapidTransportInvalid);
                return;
            }

            if (!player.CurrencyManager.CanAfford(CurrencyType.Credits, route.Price))
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: insufficient credits for route {RouteId} with price {Price}.",
                    player.Guid, route.Id, route.Price);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, formula.Dataint0, CastResult.CasterVitalCostMoney);
                return;
            }

            log.LogTrace("Attempting rapid transport for player {PlayerGuid}: route {RouteId}, destination taxi node {TaxiNode}, price {Price}, spell {Spell4Id}.",
                player.Guid, route.Id, rapidTransport.TaxiNode, route.Price, formula.Dataint0);
            var spellParameters = new RapidTransportSpellParameters(route.Price)
            {
                // Native cast-context construction indicates the client packet's secondary 32-bit field is a
                // generated cast-context token, not a wall-clock time value.
                // Keep server mutation keyed only by validated destination node and route.
                TaxiNode            = rapidTransport.TaxiNode,
                CancelActiveTrade   = true,
                ClientContextToken  = rapidTransport.ContextToken,
                HasClientCastRequest = true,
                ClientRequestSource = nameof(ClientRapidTransport)
            };

            ClientSpellEvidenceCaptureHelper.ApplyPendingCapture(session, spellParameters);
            if (ReferenceEquals(session.Player, player))
                player.CastSpell(formula.Dataint0, spellParameters);
        }

        private TaxiRouteEntry ResolveRapidTransportRoute(IPlayer player, ushort destinationNodeId)
        {
            if (player.Map?.Entry == null
                || gameTableManager.TaxiRoute == null
                || gameTableManager.TaxiNode == null
                || gameTableManager.WorldLocation2 == null)
                return null;

            uint worldId = player.Map.Entry.Id;
            Vector3 playerPosition = player.Position;

            return gameTableManager.TaxiRoute.Entries
                .Where(route => route != null && route.TaxiNodeIdDestination == destinationNodeId)
                .Select(route => new
                {
                    Route = route,
                    SourceNode = gameTableManager.TaxiNode.GetEntry(route.TaxiNodeIdSource)
                })
                .Where(x => x.SourceNode != null)
                .Select(x => new
                {
                    x.Route,
                    SourceLocation = gameTableManager.WorldLocation2.GetEntry(x.SourceNode.WorldLocation2Id)
                })
                .Where(x => x.SourceLocation != null && x.SourceLocation.WorldId == worldId)
                .OrderBy(x => Vector3.DistanceSquared(
                    playerPosition,
                    new Vector3(x.SourceLocation.Position0, x.SourceLocation.Position1, x.SourceLocation.Position2)))
                .Select(x => x.Route)
                .FirstOrDefault();
        }

        private void SendRapidTransportCastResult(IWorldSession session, uint contextToken, uint spell4Id, CastResult castResult)
        {
            log.LogDebug("Sending rapid transport cast result {CastResult} for player {PlayerGuid} and spell {Spell4Id}.",
                castResult, session.Player?.Guid, spell4Id);

            session.EnqueueMessageEncrypted(new ServerSpellCastResult
            {
                ContextToken = contextToken,
                Spell4Id     = spell4Id,
                CastResult   = castResult
            });
        }
    }
}
