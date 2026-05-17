using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Abilities;
using NexusForever.GameTable;
using NexusForever.Network.Message;
using NexusForever.Network.World.Message.Model.Abilities;
using NexusForever.Network.World.Message.Static;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    public class ClientRespecAmpsHandler : IMessageHandler<IWorldSession, ClientRespecAmps>
    {
        private readonly IGameTableManager gameTableManager;

        public ClientRespecAmpsHandler(
            IGameTableManager gameTableManager)
        {
            this.gameTableManager = gameTableManager;
        }

        public void HandleMessage(IWorldSession session, ClientRespecAmps requestAmpReset)
        {
            LimitedActionSetResult validationResult = ValidateRequest(session, requestAmpReset);
            if (validationResult != LimitedActionSetResult.Ok)
            {
                SendAmpRespecResult(session, requestAmpReset.SpecIndex, validationResult);
                return;
            }

            IActionSet actionSet = session.Player.SpellManager.GetActionSet(requestAmpReset.SpecIndex);
            actionSet.RemoveAmp(requestAmpReset.RespecType, requestAmpReset.Value);
            SendAmpRespecResult(session, requestAmpReset.SpecIndex, LimitedActionSetResult.Ok);
            session.EnqueueMessageEncrypted(actionSet.BuildServerAmpList());
        }

        private LimitedActionSetResult ValidateRequest(IWorldSession session, ClientRespecAmps requestAmpReset)
        {
            if (session.Player == null)
                return LimitedActionSetResult.InvalidUnit;

            if (requestAmpReset.SpecIndex >= ActionSet.MaxActionSets)
                return LimitedActionSetResult.InvalidSpecIndex;

            if (!session.Player.IsAlive)
                return LimitedActionSetResult.PlayerIsDead;

            if (session.Player.InCombat)
                return LimitedActionSetResult.InCombat;

            IActionSet actionSet = session.Player.SpellManager.GetActionSet(requestAmpReset.SpecIndex);

            return requestAmpReset.RespecType switch
            {
                AmpRespecType.Full when requestAmpReset.Value == 0u => LimitedActionSetResult.Ok,
                AmpRespecType.Full => LimitedActionSetResult.EldanAugmentationInvalidId,
                AmpRespecType.Section => gameTableManager.EldanAugmentationCategory.GetEntry(requestAmpReset.Value) == null
                    ? LimitedActionSetResult.EldanAugmentationInvalidCategoryId
                    : LimitedActionSetResult.Ok,
                AmpRespecType.Single => gameTableManager.EldanAugmentation.GetEntry(requestAmpReset.Value) == null
                    || actionSet.GetAmp((ushort)requestAmpReset.Value) == null
                        ? LimitedActionSetResult.EldanAugmentationInvalidId
                        : LimitedActionSetResult.Ok,
                _ => LimitedActionSetResult.EldanAugmentationInvalidId
            };
        }

        private static void SendAmpRespecResult(IWorldSession session, ushort specIndex, LimitedActionSetResult result)
        {
            session.EnqueueMessageEncrypted(new ServerAmpRespecResult
            {
                Results =
                {
                    new ServerAmpRespecResult.AmpResult
                    {
                        SpecIndex = specIndex,
                        Result    = result
                    }
                }
            });
        }
    }
}
