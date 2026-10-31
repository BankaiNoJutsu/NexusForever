using NexusForever.Database.Auth;

namespace NexusForever.Game.Abstract.Account.Currency
{
    /// <summary>Optional transactional capability; existing currency implementations retain their APIs.</summary>
    public interface IAccountCurrencyMutationScopes
    {
        IAccountCurrencyMutationScope BeginMutationScope();
    }

    public interface IAccountCurrencyMutationScope : IDisposable
    {
        void Enlist(AuthContext context);
        void Complete();
    }

    public interface IAccountCurrencySourceScopes
    {
        IAccountCurrencyMutationScope BeginSourceConsumption(string sourceKey, ulong inventoryId, bool mayBeUnpersisted = false);
    }
}
