using NexusForever.Game.Abstract.Account;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Storefront;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Account.Inventory
{
    public static class VirtualCurrencyPackageService
    {
        public static bool TryPurchase(IAccount account, IGameSession session, byte packageId, out StoreError error)
        {
            error = StoreError.GenericFail;
            if (account == null || session == null)
                return false;

            if (!VirtualCurrencyPackageCatalog.TryGetDefinition(packageId, out VirtualCurrencyPackageDefinition definition))
            {
                error = StoreError.InvalidOffer;
                return false;
            }

            account.CurrencyManager.CurrencyAddAmount(definition.CurrencyType, definition.GrantAmount);
            SendPurchaseResults(session, isSuccess: true, displayValue: 0u);
            return true;
        }

        public static void SendPurchaseResults(IGameSession session, bool isSuccess, uint displayValue)
        {
            session.EnqueueMessageEncrypted(new ServerStorePurchaseVirtualCurrencyPackageResult
            {
                Flag       = isSuccess,
                UInt5Value = displayValue
            });

            session.EnqueueMessageEncrypted(new ServerStoreCompleteOrderVirtualCurrencyPackageResult
            {
                Flag  = isSuccess,
                Value = displayValue
            });
        }
    }

    public sealed class VirtualCurrencyPackageDefinition
    {
        public byte PackageId { get; init; }
        public string Name { get; init; }
        public AccountCurrencyType CurrencyType { get; init; }
        public ulong GrantAmount { get; init; }
        public float DisplayPrice { get; init; }
    }

    public static class VirtualCurrencyPackageCatalog
    {
        private static readonly VirtualCurrencyPackageDefinition[] Definitions =
        [
            new()
            {
                PackageId    = 1,
                Name         = "NCoin Pack (1,000)",
                CurrencyType = AccountCurrencyType.NCoin,
                GrantAmount  = 1_000ul,
                DisplayPrice = 4.99f
            },
            new()
            {
                PackageId    = 2,
                Name         = "NCoin Pack (2,500)",
                CurrencyType = AccountCurrencyType.NCoin,
                GrantAmount  = 2_500ul,
                DisplayPrice = 9.99f
            },
            new()
            {
                PackageId    = 3,
                Name         = "Omnibit Pack (500)",
                CurrencyType = AccountCurrencyType.Omnibit,
                GrantAmount  = 500ul,
                DisplayPrice = 2.99f
            }
        ];

        public static IReadOnlyList<VirtualCurrencyPackageDefinition> All => Definitions;

        public static bool TryGetDefinition(byte packageId, out VirtualCurrencyPackageDefinition definition)
        {
            definition = Definitions.FirstOrDefault(d => d.PackageId == packageId);
            return definition != null;
        }

        public static IReadOnlyList<ServerStoreCategories.CurrencyPackage> BuildCatalogRows()
        {
            return Definitions
                .Select(d => new ServerStoreCategories.CurrencyPackage
                {
                    Id           = d.PackageId,
                    Name         = d.Name,
                    Count        = (uint)Math.Min(d.GrantAmount, uint.MaxValue),
                    Price        = d.DisplayPrice,
                    CurrencyType = d.CurrencyType
                })
                .ToList();
        }
    }
}
