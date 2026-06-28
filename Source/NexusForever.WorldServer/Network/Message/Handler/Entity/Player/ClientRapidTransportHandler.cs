using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Logging;
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
            log.LogTrace("Received rapid transport request from player {PlayerGuid}: TaxiNode={TaxiNode}, ContextToken={ContextToken}.",
                session.Player?.Guid, rapidTransport.TaxiNode, rapidTransport.ContextToken);

            if (gameTableManager.TaxiNode == null)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: taxi node table is unavailable.",
                    session.Player?.Guid);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            TaxiNodeEntry taxiNode = gameTableManager.TaxiNode.GetEntry(rapidTransport.TaxiNode);
            if (taxiNode == null)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: destination taxi node {TaxiNode} was not found.",
                    session.Player?.Guid, rapidTransport.TaxiNode);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            if (session.Player.Level < taxiNode.AutoUnlockLevel)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: level {PlayerLevel} is below required {RequiredLevel} for taxi node {TaxiNode}.",
                    session.Player?.Guid, session.Player.Level, taxiNode.AutoUnlockLevel, rapidTransport.TaxiNode);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            if (gameTableManager.WorldLocation2 == null)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: world location table is unavailable.",
                    session.Player?.Guid);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            WorldLocation2Entry worldLocation = gameTableManager.WorldLocation2.GetEntry(taxiNode.WorldLocation2Id);
            if (worldLocation == null)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: world location {WorldLocation2Id} for taxi node {TaxiNode} was not found.",
                    session.Player?.Guid, taxiNode.WorldLocation2Id, rapidTransport.TaxiNode);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            GameFormulaEntry formula = gameTableManager.GameFormula?.GetEntry(RapidTransportSpellGameFormulaId);
            if (formula == null || formula.Dataint0 == 0u)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: rapid transport spell formula {GameFormulaId} is unavailable or empty.",
                    session.Player?.Guid, RapidTransportSpellGameFormulaId);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            if (session.Player.SpellManager.GetSpellCooldown(formula.Dataint0) > 0d)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: spell {Spell4Id} is on cooldown.",
                    session.Player?.Guid, formula.Dataint0);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, formula.Dataint0, CastResult.SpellCooldown);
                return;
            }

            TaxiRouteEntry route = ResolveRapidTransportRoute(session, rapidTransport.TaxiNode);
            if (route == null)
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: no route found to destination taxi node {TaxiNode}.",
                    session.Player?.Guid, rapidTransport.TaxiNode);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, formula.Dataint0, CastResult.RapidTransportInvalid);
                return;
            }

            if (!session.Player.CurrencyManager.CanAfford(CurrencyType.Credits, route.Price))
            {
                log.LogTrace("Rapid transport rejected for player {PlayerGuid}: insufficient credits for route {RouteId} with price {Price}.",
                    session.Player?.Guid, route.Id, route.Price);
                SendRapidTransportCastResult(session, rapidTransport.ContextToken, formula.Dataint0, CastResult.CasterVitalCostMoney);
                return;
            }

            log.LogTrace("Rapid transport accepted for player {PlayerGuid}: route {RouteId}, destination taxi node {TaxiNode}, price {Price}, spell {Spell4Id}.",
                session.Player?.Guid, route.Id, rapidTransport.TaxiNode, route.Price, formula.Dataint0);
            session.Player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, route.Price);
            var spellParameters = new SpellParameters
            {
                // Native cast-context construction indicates the client packet's secondary 32-bit field is a
                // generated cast-context token, not a wall-clock time value.
                // Keep server mutation keyed only by validated destination node and route.
                TaxiNode            = rapidTransport.TaxiNode,
                CancelActiveTrade   = true,
                ClientContextToken  = rapidTransport.ContextToken,
                ClientRequestSource = nameof(ClientRapidTransport)
            };

            ClientSpellEvidenceCaptureHelper.ApplyPendingCapture(session, spellParameters);
            session.Player.CastSpell(formula.Dataint0, spellParameters);
        }

        private TaxiRouteEntry ResolveRapidTransportRoute(IWorldSession session, ushort destinationNodeId)
        {
            if (session.Player.Map?.Entry == null
                || gameTableManager.TaxiRoute == null
                || gameTableManager.TaxiNode == null
                || gameTableManager.WorldLocation2 == null)
                return null;

            uint worldId = session.Player.Map.Entry.Id;
            Vector3 playerPosition = session.Player.Position;

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
