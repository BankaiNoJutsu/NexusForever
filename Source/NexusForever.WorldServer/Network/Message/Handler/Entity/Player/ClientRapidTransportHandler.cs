using System.Linq;
using System.Numerics;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity.Player
{
    public class ClientRapidTransportHandler : IMessageHandler<IWorldSession, ClientRapidTransport>
    {
        private const uint RapidTransportSpellGameFormulaId = 0x051Bu;

        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;

        public ClientRapidTransportHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientRapidTransport rapidTransport)
        {
            TaxiNodeEntry taxiNode = gameTableManager.TaxiNode.GetEntry(rapidTransport.TaxiNode);
            if (taxiNode == null)
            {
                SendRapidTransportCastResult(session, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            if (session.Player.Level < taxiNode.AutoUnlockLevel)
            {
                SendRapidTransportCastResult(session, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            WorldLocation2Entry worldLocation = gameTableManager.WorldLocation2.GetEntry(taxiNode.WorldLocation2Id);
            if (worldLocation == null)
            {
                SendRapidTransportCastResult(session, 0u, CastResult.RapidTransportInvalid);
                return;
            }

            GameFormulaEntry formula = gameTableManager.GameFormula.GetEntry(RapidTransportSpellGameFormulaId);
            if (formula == null || formula.Dataint0 == 0u)
                throw new InvalidPacketValueException();

            if (session.Player.SpellManager.GetSpellCooldown(formula.Dataint0) > 0d)
            {
                SendRapidTransportCastResult(session, formula.Dataint0, CastResult.SpellCooldown);
                return;
            }

            TaxiRouteEntry route = ResolveRapidTransportRoute(session, rapidTransport.TaxiNode);
            if (route == null)
            {
                SendRapidTransportCastResult(session, formula.Dataint0, CastResult.RapidTransportInvalid);
                return;
            }

            if (!session.Player.CurrencyManager.CanAfford(CurrencyType.Credits, route.Price))
            {
                SendRapidTransportCastResult(session, formula.Dataint0, CastResult.CasterVitalCostMoney);
                return;
            }

            session.Player.CurrencyManager.CurrencySubtractAmount(CurrencyType.Credits, route.Price);
            session.Player.CastSpell(formula.Dataint0, new SpellParameters
            {
                TaxiNode = rapidTransport.TaxiNode
            });
        }

        private TaxiRouteEntry ResolveRapidTransportRoute(IWorldSession session, ushort destinationNodeId)
        {
            if (session.Player.Map == null)
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

        private static void SendRapidTransportCastResult(IWorldSession session, uint spell4Id, CastResult castResult)
        {
            session.EnqueueMessageEncrypted(new ServerSpellCastResult
            {
                Spell4Id = spell4Id,
                CastResult = castResult
            });
        }
    }
}
