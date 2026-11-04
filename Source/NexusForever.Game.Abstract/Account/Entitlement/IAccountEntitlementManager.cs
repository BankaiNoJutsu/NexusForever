using NexusForever.Database.Auth;
using NexusForever.Game.Abstract.Entitlement;
using NexusForever.Game.Static.Entity;

namespace NexusForever.Game.Abstract.Account.Entitlement
{
    public interface IAccountEntitlementManager : IEntitlementManager<IAccountEntitlement>, IDatabaseAuth
    {
        bool CanIncreaseEntitlement(EntitlementType type, int amount) => false;
    }
}
