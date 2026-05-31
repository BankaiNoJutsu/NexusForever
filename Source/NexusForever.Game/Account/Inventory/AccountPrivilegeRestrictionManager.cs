using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Static.Account;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Inventory
{
    public static class AccountPrivilegeRestrictionManager
    {
        public static void SendRestrictionUpdate(IAccount account, AccountPrivilegeRestrictionType restrictionType, float durationDays, bool active)
        {
            if (account?.Session == null)
                return;

            account.Session.EnqueueMessageEncrypted(new ServerAccountPrivilegeRestrictionUpdate
            {
                RestrictionType = (uint)restrictionType,
                DurationDays    = active ? durationDays : 0f
            });
        }

        public static void SendStorePurchaseVelocityRestriction(IAccount account, float durationDays = 1f)
        {
            SendRestrictionUpdate(account, AccountPrivilegeRestrictionType.StorePurchaseVelocity, durationDays, active: true);
        }
    }
}
