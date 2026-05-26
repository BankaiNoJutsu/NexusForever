using System.Text;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Loot;
using NexusForever.Game.Loot;
using NexusForever.Game.Static.RBAC;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Command.Static;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Command.Handler
{
    [Command(Permission.Item, "A collection of commands to inspect and capture loot packet evidence.", "loot")]
    public class LootCommandCategory : CommandCategory
    {
        private readonly IGlobalLootManager lootManager;

        public LootCommandCategory(
            IGlobalLootManager lootManager)
        {
            this.lootManager = lootManager;
        }

        [Command(Permission.ItemInfo, "Inspect active loot state for the selected owner or explicit owner unit id.", "inspect", "state")]
        public void HandleLootInspect(ICommandContext context,
            [Parameter("Owner unit id to inspect. If omitted, the selected target is used.", ParameterFlags.Optional)]
            uint? ownerUnitId)
        {
            if (!TryGetInvokerPlayer(context, out IPlayer player))
                return;

            ownerUnitId ??= context.Target?.Guid;
            if (ownerUnitId == null)
            {
                context.SendError("Supply an owner unit id or select a loot owner first.");
                return;
            }

            if (!lootManager.TryGetLootRuntimeSnapshot(player, ownerUnitId.Value, out LootRuntimeSnapshot snapshot))
            {
                context.SendError($"No active loot snapshot is available for owner unit {ownerUnitId.Value} and player {player.CharacterId}.");
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"Loot owner {snapshot.OwnerUnitId}: entityType {snapshot.LootEntityType}, looterType {snapshot.LooterType}, explosion {snapshot.Explosion}, expired {snapshot.HasExpired}.");
            builder.AppendLine($"Tracked looters: [{string.Join(", ", snapshot.TrackedLooterCharacterIds)}], viewer {snapshot.ViewerCharacterId}, viewerTracked {snapshot.ViewerIsTrackedLooter}.");
            builder.AppendLine($"Runtime ParentUnitId would serialize as {snapshot.ParentUnitIdRuntimeValue}. {snapshot.ParentUnitIdNotes}");
            builder.AppendLine($"Items: {snapshot.Items.Count}.");

            foreach (LootRuntimeSnapshotItem item in snapshot.Items)
            {
                builder.AppendLine(
                    $"  lootUnitId {item.LootUnitId}, type {item.Type}, itemId {item.ItemId}, amount {item.Amount}, delivered {item.Delivered}, canLootForViewer {item.ViewerCanLoot}, requiresRoll {item.RequiresRoll}, onlyMasterLootable {item.OnlyMasterLootable}, rollTime {item.RollTime}, winnerChar {item.WinnerCharacterId}, winnerGuid {item.WinnerGuid}, itemQuality2Id {item.ItemQuality2Id}, lootVisualEffectId {item.ItemQualityVisualEffectIdLoot}, eligible [{string.Join(", ", item.EligibleCharacterIds)}], masters [{string.Join(", ", item.MasterCharacterIds)}], masterCandidates [{string.Join(", ", item.MasterCandidateCharacterIds)}], masterListCount {item.MasterListCount}.");
            }

            builder.AppendLine($"Use !loot capturenext before the next kill, bag use, or forced loot notify to export packet-shape evidence under {LootRuntimeEvidenceCollector.GetOutputDirectoryHint()}.");
            context.SendMessage(builder.ToString());
        }

        [Command(Permission.ItemInfo, "Arm runtime evidence export for the next ServerLootNotify sent to the invoker.", "capturenext", "capture", "evidencenext")]
        public void HandleLootCaptureNext(ICommandContext context)
        {
            if (!TryGetInvokerSession(context, out IWorldSession session))
                return;

            session.ArmNextLootEvidenceCapture();
            context.SendMessage($"Next loot notify for this player will export a runtime evidence artifact under {LootRuntimeEvidenceCollector.GetOutputDirectoryHint()}.");
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
