using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Spell;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientSpellCastWithServiceTokenHandler : IMessageHandler<IWorldSession, ClientSpellCastWithServiceToken>
    {
        #region Dependency Injection

        private readonly IGameTableManager gameTableManager;
        private readonly IGlobalSpellManager globalSpellManager;

        public ClientSpellCastWithServiceTokenHandler(
            IGameTableManager gameTableManager,
            IGlobalSpellManager globalSpellManager)
        {
            this.gameTableManager   = gameTableManager;
            this.globalSpellManager = globalSpellManager;
        }

        #endregion

        public void HandleMessage(IWorldSession session, ClientSpellCastWithServiceToken serviceTokenCast)
        {
            ISpellInfo spellInfo = GetSpellInfo(serviceTokenCast.Spell4Id);
            if (spellInfo == null)
                throw new InvalidPacketValueException();

            if (!spellInfo.HasServiceTokenCost || spellInfo.ServiceTokenCostEntry == null)
            {
                SendSpellCastResult(session, serviceTokenCast.Spell4Id, CastResult.ServiceTokensInsufficentFunds);
                return;
            }

            var spellParameters = new SpellParameters
            {
                SpellInfo              = spellInfo,
                UserInitiatedSpellCast = true,
                UseServiceTokenCost    = true,
                ClientContextToken     = serviceTokenCast.ContextToken,
                ClientRequestSource    = nameof(ClientSpellCastWithServiceToken)
            };

            ClientSpellEvidenceCaptureHelper.ApplyPendingCapture(session, spellParameters);
            session.Player.CastSpell(spellParameters);
        }

        private ISpellInfo GetSpellInfo(uint spell4Id)
        {
            Spell4Entry spell4Entry = gameTableManager.Spell4.GetEntry(spell4Id);
            if (spell4Entry == null)
                return null;

            ISpellBaseInfo spellBaseInfo = globalSpellManager.GetSpellBaseInfo(spell4Entry.Spell4BaseIdBaseSpell);
            return spellBaseInfo?.GetSpellInfo((byte)spell4Entry.TierIndex);
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
