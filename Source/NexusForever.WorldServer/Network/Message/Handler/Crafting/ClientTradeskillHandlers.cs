using Microsoft.Extensions.Logging;
using NexusForever.Game.Static.Crafting;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Crafting
{
    public class ClientTradeskillLearnHandler : IMessageHandler<IWorldSession, ClientTradeskillLearn>
    {
        private readonly ILogger<ClientTradeskillLearnHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientTradeskillLearnHandler(
            ILogger<ClientTradeskillLearnHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientTradeskillLearn learn)
        {
            TradeskillRequestHelper.ValidateTradeskill(gameTableManager, learn.ToLearnTradeskillId);
            TradeskillRequestHelper.ValidateTradeskill(gameTableManager, learn.ToDropTradeskillId, true);

            if (session.Player == null || !session.Player.LearnTradeskill(learn.ToLearnTradeskillId, learn.ToDropTradeskillId))
            {
                log.LogDebug("Rejected tradeskill learn request from player {PlayerGuid}: learn {LearnTradeskillId}, drop {DropTradeskillId}.",
                    session.Player?.Guid, learn.ToLearnTradeskillId, learn.ToDropTradeskillId);
                session.Player?.SendGenericError(GenericError.Params);
                return;
            }

            log.LogDebug("Completed tradeskill learn request from player {PlayerGuid}: learn {LearnTradeskillId}, drop {DropTradeskillId}.",
                session.Player?.Guid, learn.ToLearnTradeskillId, learn.ToDropTradeskillId);
        }
    }

    public class ClientTradeskillPickTalentHandler : IMessageHandler<IWorldSession, ClientTradeskillPickTalent>
    {
        private readonly ILogger<ClientTradeskillPickTalentHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientTradeskillPickTalentHandler(
            ILogger<ClientTradeskillPickTalentHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientTradeskillPickTalent pickTalent)
        {
            TradeskillRequestHelper.ValidateTradeskill(gameTableManager, pickTalent.TradeskillId);
            if (pickTalent.Tier >= TradeskillRequestHelper.MaxTalentTiers)
                throw new InvalidPacketValueException();

            TradeskillRequestHelper.ValidateBonusForTradeskill(gameTableManager, pickTalent.TradeskillId, pickTalent.TradeskillBonusId);

            if (session.Player == null || !session.Player.PickTradeskillTalent(pickTalent.TradeskillId, pickTalent.Tier, pickTalent.TradeskillBonusId))
            {
                log.LogDebug("Rejected tradeskill pick-talent request from player {PlayerGuid}: tradeskill {TradeskillId}, tier {Tier}, bonus {TradeskillBonusId}.",
                    session.Player?.Guid, pickTalent.TradeskillId, pickTalent.Tier, pickTalent.TradeskillBonusId);
                session.Player?.SendGenericError(GenericError.Params);
                return;
            }

            log.LogDebug("Completed tradeskill pick-talent request from player {PlayerGuid}: tradeskill {TradeskillId}, tier {Tier}, bonus {TradeskillBonusId}.",
                session.Player?.Guid, pickTalent.TradeskillId, pickTalent.Tier, pickTalent.TradeskillBonusId);
        }
    }

    public class ClientTradeskillResetTalentsHandler : IMessageHandler<IWorldSession, ClientTradeskillResetTalents>
    {
        private readonly ILogger<ClientTradeskillResetTalentsHandler> log;
        private readonly IGameTableManager gameTableManager;

        public ClientTradeskillResetTalentsHandler(
            ILogger<ClientTradeskillResetTalentsHandler> log,
            IGameTableManager gameTableManager)
        {
            this.log              = log;
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientTradeskillResetTalents resetTalents)
        {
            TradeskillRequestHelper.ValidateTradeskill(gameTableManager, resetTalents.TradeskillId);

            if (session.Player == null || !session.Player.ResetTradeskillTalents(resetTalents.TradeskillId))
            {
                log.LogDebug("Rejected tradeskill reset-talents request from player {PlayerGuid}: tradeskill {TradeskillId}.",
                    session.Player?.Guid, resetTalents.TradeskillId);
                session.Player?.SendGenericError(GenericError.Params);
                session.EnqueueMessageEncrypted(new ServerTradeskillRelearnCooldown());
                return;
            }

            log.LogDebug("Completed tradeskill reset-talents request from player {PlayerGuid}: tradeskill {TradeskillId}.",
                session.Player.Guid, resetTalents.TradeskillId);
        }
    }

    internal static class TradeskillRequestHelper
    {
        public const uint MaxTalentTiers = 10u;

        public static void ValidateTradeskill(IGameTableManager gameTableManager, TradeskillType tradeskillId, bool allowNone = false)
        {
            uint id = (uint)tradeskillId;
            if (allowNone && id == 0u)
                return;

            if (gameTableManager.Tradeskill.GetEntry(id) == null)
                throw new InvalidPacketValueException();
        }

        public static void ValidateBonusForTradeskill(IGameTableManager gameTableManager, TradeskillType tradeskillId, uint tradeskillBonusId)
        {
            TradeskillBonusEntry bonus = gameTableManager.TradeskillBonus.GetEntry(tradeskillBonusId);
            if (bonus == null)
                throw new InvalidPacketValueException();

            TradeskillTalentTierEntry tier = gameTableManager.TradeskillTalentTier.GetEntry(bonus.TradeSkillTierId);
            if (tier == null || tier.TradeSkillId != (uint)tradeskillId || !TierContainsBonus(tier, tradeskillBonusId))
                throw new InvalidPacketValueException();
        }

        private static bool TierContainsBonus(TradeskillTalentTierEntry tier, uint tradeskillBonusId)
        {
            return tier.TradeSkillBonusId00 == tradeskillBonusId
                || tier.TradeSkillBonusId01 == tradeskillBonusId
                || tier.TradeSkillBonusId02 == tradeskillBonusId
                || tier.TradeSkillBonusId03 == tradeskillBonusId
                || tier.TradeSkillBonusId04 == tradeskillBonusId;
        }
    }
}
