using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Static.RBAC;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Command.Static;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Command.Handler
{
    [Command(Permission.Account, "A collection of commands to inspect and capture reward rotation packet evidence.", "reward", "rew")]
    public class RewardCommandCategory : CommandCategory
    {
        [Command(Permission.Account, "Arm runtime evidence export for the next reward rotation refresh request from the invoker.", "capturenext", "capture", "evidencenext")]
        public void HandleRewardCaptureNext(ICommandContext context)
        {
            if (!TryGetInvokerSession(context, out IWorldSession session))
                return;

            session.ArmNextRewardRotationEvidenceCapture();
            context.SendMessage($"Next reward rotation refresh request for this player will export a runtime evidence artifact under {RewardRotationRuntimeEvidenceCollector.GetOutputDirectoryHint()}.");
        }

        [Command(Permission.Account, "Export a reward rotation report that includes game-table-derived 0x07CD content-context ids for the supplied index.", "reportcontext", "context")]
        public void HandleRewardReportContext(ICommandContext context,
            [Parameter("Reward rotation index to report.", ParameterFlags.Optional)]
            uint rewardRotationIndex = 0u)
        {
            if (!TryGetInvokerPlayer(context, out IPlayer player))
                return;

            string outputPath = RewardRotationRuntimeEvidenceCollector.ExportReport(
                player,
                rewardRotationIndex,
                "manual-command-context",
                "Manual reward rotation report exported with game-table-derived content-context ids; schedule and entry-state rows remain empty.",
                new GameTableRewardRotationRefreshProvider());
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                context.SendError("Failed to export reward rotation report. Check server logs for details.");
                return;
            }

            context.SendMessage($"Reward rotation context report exported to {outputPath}.");
        }

        [Command(Permission.Account, "Export a reward rotation protocol/report snapshot for the supplied request index without generating speculative live rows.", "report", "shape", "inspect")]
        public void HandleRewardReport(ICommandContext context,
            [Parameter("Reward rotation index to report.", ParameterFlags.Optional)]
            uint rewardRotationIndex = 0u)
        {
            if (!TryGetInvokerPlayer(context, out IPlayer player))
                return;

            string outputPath = RewardRotationRuntimeEvidenceCollector.ExportReport(
                player,
                rewardRotationIndex,
                "manual-command",
                "Manual reward rotation report exported without generating speculative live schedule rows or entry-state transitions.");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                context.SendError("Failed to export reward rotation report. Check server logs for details.");
                return;
            }

            string supportSummary = RewardRotationRefreshBuilder.IsSupported(rewardRotationIndex)
                ? "supported placeholder response recorded"
                : $"unsupported request recorded (supported range 0-{RewardRotationRefreshBuilder.MaxSupportedRewardRotationIndex})";
            context.SendMessage($"Reward rotation report exported to {outputPath} ({supportSummary}).");
        }

        private static bool TryGetInvokerPlayer(ICommandContext context, out IPlayer player)
        {
            player = context.Invoker as IPlayer;
            if (player != null)
                return true;

            context.SendError("This command requires a player invoker.");
            return false;
        }

        private static bool TryGetInvokerSession(ICommandContext context, out IWorldSession session)
        {
            session = (context.Invoker as IPlayer)?.Session as IWorldSession;
            if (session != null)
                return true;

            context.SendError("This command requires a player invoker with an active world session.");
            return false;
        }
    }
}
