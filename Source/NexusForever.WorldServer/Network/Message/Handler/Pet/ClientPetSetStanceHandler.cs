using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Static.Pet;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Pet;

namespace NexusForever.WorldServer.Network.Message.Handler.Pet
{
    public class ClientPetSetStanceHandler : IMessageHandler<IWorldSession, ClientPetSetStance>
    {
        private static readonly uint[] EngineerCombatBotCreatureIds =
        [
            42682u,
            59845u,
            42683u,
            59846u,
            42684u,
            59847u,
            42685u,
            59848u
        ];

        private readonly ILogger<ClientPetSetStanceHandler> log;

        public ClientPetSetStanceHandler(ILogger<ClientPetSetStanceHandler> log)
        {
            this.log = log;
        }

        public void HandleMessage(IWorldSession session, ClientPetSetStance petSetStance)
        {
            if (session.Player?.Map == null)
                return;

            if (!Enum.IsDefined(petSetStance.Stance))
                throw new InvalidPacketValueException();

            if (petSetStance.PetUnitId == 0u)
            {
                IReadOnlyCollection<IWorldEntity> engineerBots = GetActiveEngineerCombatBots(session.Player);
                if (engineerBots.Count != 0)
                {
                    session.EnqueueMessageEncrypted(new ServerPetStanceChanged
                    {
                        PetUnitId = 0u,
                        Stance    = petSetStance.Stance
                    });

                    foreach (IWorldEntity engineerBot in engineerBots)
                    {
                        ApplySummonCommandStance(engineerBot, petSetStance.Stance);
                        session.EnqueueMessageEncrypted(new ServerPetStanceChanged
                        {
                            PetUnitId = engineerBot.Guid,
                            Stance    = petSetStance.Stance
                        });
                    }

                    log.LogDebug("ClientPetSetStance: player={Player} allEngineerBots count={Count} stance={Stance}",
                        session.Player.Guid, engineerBots.Count, petSetStance.Stance);
                    return;
                }
            }

            uint? petUnitId = petSetStance.PetUnitId == 0u
                ? session.Player.VanityPetGuid
                : petSetStance.PetUnitId;

            if (!petUnitId.HasValue)
            {
                log.LogDebug("ClientPetSetStance: player={Player} has no active pet for stance={Stance}.",
                    session.Player.Guid, petSetStance.Stance);
                return;
            }

            IWorldEntity entity = session.Player.Map.GetEntity<IWorldEntity>(petUnitId.Value);
            if (entity is IPetEntity pet)
            {
                if (pet.OwnerGuid != session.Player.Guid)
                {
                    log.LogDebug("ClientPetSetStance: pet {PetUnitId} not owned by player {Player}.",
                        petUnitId.Value, session.Player.Guid);
                    return;
                }

                pet.Stance = petSetStance.Stance;
                log.LogDebug("ClientPetSetStance: player={Player} petUnitId={PetUnitId} stance={Stance}",
                    session.Player.Guid, petUnitId.Value, petSetStance.Stance);
                return;
            }

            if (entity == null || entity.SummonerGuid != session.Player.Guid)
            {
                log.LogDebug("ClientPetSetStance: pet {PetUnitId} not found or not owned by player {Player}.",
                    petUnitId.Value, session.Player.Guid);
                return;
            }

            ApplySummonCommandStance(entity, petSetStance.Stance);
            session.EnqueueMessageEncrypted(new ServerPetStanceChanged
            {
                PetUnitId = petUnitId.Value,
                Stance    = petSetStance.Stance
            });

            log.LogDebug("ClientPetSetStance: player={Player} summonedPetUnitId={PetUnitId} stance={Stance}",
                session.Player.Guid, petUnitId.Value, petSetStance.Stance);
        }

        private static IReadOnlyCollection<IWorldEntity> GetActiveEngineerCombatBots(IPlayer player)
        {
            IEntitySummonFactory summonFactory = player?.SummonFactory;
            if (summonFactory == null)
                return [];

            var engineerBots = new List<IWorldEntity>();
            foreach (uint creatureId in EngineerCombatBotCreatureIds)
                engineerBots.AddRange(summonFactory.GetSummonCreatures(creatureId));

            return engineerBots;
        }

        private static void ApplySummonCommandStance(IWorldEntity entity, PetStance stance)
        {
            entity.SummonCommandStance = stance;
            entity.SummonCommandFollowRequested = stance != PetStance.Stay;

            if (entity is not IUnitEntity unit)
                return;

            if (stance is not (PetStance.Passive or PetStance.Stay))
                return;

            unit.SetTarget((IWorldEntity)null);
            unit.ThreatManager?.ClearThreatList();
            unit.MovementManager?.Finalise();
        }
    }
}
