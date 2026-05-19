using NexusForever.Cryptography;
using NexusForever.Database;
using NexusForever.Database.Auth;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Account.Inventory;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Static.RBAC;
using NexusForever.Shared.Configuration;
using NexusForever.WorldServer.Command.Context;
using NexusForever.WorldServer.Command.Convert;
using NexusForever.WorldServer.Command.Static;
using NexusForever.WorldServer.Network;

namespace NexusForever.WorldServer.Command.Handler
{
    [Command(Permission.Account, "A collection of commands to modify game accounts.", "acc", "account")]
    public class AccountCommandCategory : CommandCategory
    {
        [Command(Permission.AccountCreate, "Create a new account.", "create")]
        public void HandleAccountCreate(ICommandContext context,
            [Parameter("Email address for the new account", converter: typeof(StringLowerParameterConverter))]
            string email,
            [Parameter("Password for the new account")]
            string password,
            [Parameter("Role", ParameterFlags.Optional, typeof(EnumParameterConverter<Role>))]
            Role? role = null)
        {
            if (DatabaseManager.Instance.GetDatabase<AuthDatabase>().AccountExists(email))
            {
                context.SendMessage("Account with that username already exists. Please try another.");
                return;
            }

            role ??= (SharedConfiguration.Instance.Get<RealmConfig>().DefaultRole ?? Role.Player);

            (string salt, string verifier) = PasswordProvider.GenerateSaltAndVerifier(email, password);
            DatabaseManager.Instance.GetDatabase<AuthDatabase>().CreateAccount(email, salt, verifier, (uint)role);

            context.SendMessage($"Account {email} created successfully");
        }

        [Command(Permission.AccountDelete, "Delete an account.", "delete")]
        public void HandleAccountDelete(ICommandContext context,
            [Parameter("Email address of the account to delete")]
            string email)
        {
            if (DatabaseManager.Instance.GetDatabase<AuthDatabase>().DeleteAccount(email))
                context.SendMessage($"Account {email} successfully removed!");
            else
                context.SendMessage($"Cannot find account with Email: {email}");
        }

        [Command(Permission.Account, "Arm runtime evidence export for the next blocked pending-group transfer attempted by the invoker.", "capturenext", "capture", "evidencenext")]
        public void HandleAccountCaptureNext(ICommandContext context)
        {
            if (!TryGetInvokerSession(context, out IWorldSession session))
                return;

            session.ArmNextAccountRuntimeEvidenceCapture();
            context.SendMessage($"Next blocked pending-group transfer for this player will export a runtime evidence artifact under {AccountRuntimeEvidenceCollector.GetOutputDirectoryHint()}. Use !account couponblockers for the current coupon blocker snapshot.");
        }

        [Command(Permission.Account, "Export a coupon blocker report using the current enum-only mapping and unsupported-path notes.", "couponblockers", "couponreport")]
        public void HandleAccountCouponBlockers(ICommandContext context)
        {
            if (!TryGetInvokerPlayer(context, out IPlayer player))
                return;

            string outputPath = AccountRuntimeEvidenceCollector.ExportCouponBlockerReport(
                player,
                "manual-command",
                "Manual coupon blocker snapshot exported without mapping a coupon request packet or implementing coupon runtime policy.");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                context.SendError("Failed to export coupon blocker report. Check server logs for details.");
                return;
            }

            context.SendMessage($"Coupon blocker report exported to {outputPath}.");
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
