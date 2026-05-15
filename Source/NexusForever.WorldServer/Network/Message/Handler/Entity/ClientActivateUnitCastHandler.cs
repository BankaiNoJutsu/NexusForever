using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Spell;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Entity
{
    public class ClientActivateUnitCastHandler : IMessageHandler<IWorldSession, ClientActivateUnitCast>
    {
        #region Dependency Injection

        private readonly IPrerequisiteManager prerequisiteManager;

        public ClientActivateUnitCastHandler(
            IPrerequisiteManager prerequisiteManager)
        {
            this.prerequisiteManager = prerequisiteManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientActivateUnitCast activateUnitCast)
        {
            IWorldEntity entity = session.Player.GetVisible<IWorldEntity>(activateUnitCast.ActivateUnitId);
            if (entity == null)
                throw new InvalidPacketValueException();

            if (ActivationInteractionGuards.TryRejectBusyTarget(session, entity))
                return;

            if (ActivationInteractionGuards.TryRejectOutOfRangeTarget(session, entity))
                return;

            if (!TryResolveActivateSpell(entity, session.Player, out uint spell4Id))
            {
                SendSpellCastResult(session, GetFallbackActivateSpellId(entity), CastResult.NoValidActivateSpell);
                entity.OnActivateFail(session.Player);
                return;
            }

            session.Player.CastSpell(spell4Id, new SpellParameters
            {
                PrimaryTargetId        = entity.Guid,
                UserInitiatedSpellCast = false
            });

            entity.OnActivateCast(session.Player);
            entity.OnActivateSuccess(session.Player);
        }

        private bool TryResolveActivateSpell(IWorldEntity entity, IPlayer player, out uint spell4Id)
        {
            spell4Id = 0u;

            uint[] spellIds =
            [
                entity.CreatureEntry?.Spell4IdActivate00 ?? 0u,
                entity.CreatureEntry?.Spell4IdActivate01 ?? 0u,
                entity.CreatureEntry?.Spell4IdActivate02 ?? 0u,
                entity.CreatureEntry?.Spell4IdActivate03 ?? 0u
            ];

            uint[] prerequisiteIds =
            [
                entity.CreatureEntry?.PrerequisiteIdActivateSpell00 ?? 0u,
                entity.CreatureEntry?.PrerequisiteIdActivateSpell01 ?? 0u,
                entity.CreatureEntry?.PrerequisiteIdActivateSpell02 ?? 0u,
                entity.CreatureEntry?.PrerequisiteIdActivateSpell03 ?? 0u
            ];

            for (int index = 0; index < spellIds.Length; index++)
            {
                uint candidateSpellId = spellIds[index];
                if (candidateSpellId == 0u)
                    continue;

                uint prerequisiteId = prerequisiteIds[index];
                if (prerequisiteId != 0u && !prerequisiteManager.Meets(player, prerequisiteId))
                    continue;

                spell4Id = candidateSpellId;
                return true;
            }

            return false;
        }

        private static uint GetFallbackActivateSpellId(IWorldEntity entity)
        {
            return entity.CreatureEntry?.Spell4IdActivate00
                ?? entity.CreatureEntry?.Spell4IdActivate01
                ?? entity.CreatureEntry?.Spell4IdActivate02
                ?? entity.CreatureEntry?.Spell4IdActivate03
                ?? 0u;
        }

        private static void SendSpellCastResult(IWorldSession session, uint spell4Id, CastResult castResult)
        {
            session.EnqueueMessageEncrypted(new ServerSpellCastResult
            {
                Spell4Id   = spell4Id,
                CastResult = castResult
            });
        }
    }
}
