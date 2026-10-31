using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.GameTable;

namespace NexusForever.Game.Quest.Reward
{
    // Interprets the effects of a selected choice, not permission to claim it.
    // AccountItem compensation must stay on the account wallet path; it is
    // never an Item2 alias or an implicit entitlement grant.
    internal sealed class ContractRewardAwardCatalog
    {
        private readonly Dictionary<(uint Reward, uint Choice), ContractRewardAward> awards = [];
        internal ContractRewardCatalog Catalog { get; }
        internal IReadOnlyList<ContractRewardAward> Awards { get; }

        internal ContractRewardAwardCatalog(IGameTableManager tables) : this(new ContractRewardCatalog(tables)) { }

        internal ContractRewardAwardCatalog(ContractRewardCatalog catalog)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            foreach (var track in Catalog.Tracks)
            foreach (var slot in track.Slots)
            foreach (var reward in slot.Candidates)
            {
                ContractCharacterCurrencyAward currency = null;
                if (reward.CurrencyTypeId != 0)
                {
                    var definition = Catalog.GetCurrencySource(reward.CurrencyTypeId);
                    if (definition?.Id != reward.CurrencyTypeId || !Enum.IsDefined((CurrencyType)definition.Id))
                        throw Invalid("Contract currency has no supported Character owner.");
                    currency = new((CurrencyType)definition.Id, reward.CurrencyAmount, definition.CapAmount);
                }
                foreach (var choice in reward.Choices)
                {
                    ContractItemAward item = null;
                    ContractAccountCurrencyAward account = null;
                    if (choice.Type == 0)
                        item = new(choice.Id, choice.Count, (reward.Flags & 2u) != 0);
                    else
                    {
                        var source = Catalog.GetAccountSource(choice.Id);
                        // Only the mapped pure currency grant is supported here.
                        // A mixed/new effect must not be silently discarded when
                        // the compensation is handed from Character to Auth.
                        if (source?.Id != choice.Id || source.Flags != 0 || source.Item2Id != 0 || source.EntitlementId != 0
                            || source.InstantEventEnum != 0 || source.GenericUnlockSetId != 0 || source.PrerequisiteId != 0
                            || source.AccountItemCooldownGroupId != 0 || source.Creature2DisplayGroupIdGacha != 0
                            || source.AccountCurrencyEnum == 0 || source.AccountCurrencyAmount == 0)
                            throw Invalid("Contract AccountItem is not a supported pure account currency grant.");
                        if (source.CurrencyDefinitionId != source.AccountCurrencyEnum || !Enum.IsDefined((AccountCurrencyType)source.AccountCurrencyEnum))
                            throw Invalid("Contract AccountItem currency has no supported Auth owner.");
                        ulong amount;
                        try { amount = checked(source.AccountCurrencyAmount * choice.Count); }
                        catch (OverflowException error) { throw new InvalidDataException("Contract account currency quantity overflows.", error); }
                        account = new(source.Id, (AccountCurrencyType)source.AccountCurrencyEnum, amount);
                    }
                    awards.Add((reward.Id, choice.Index), new(track.Id, track.Type, slot.Ordinal, slot.RequiredPoints,
                        reward.Id, choice.Index, choice.Type, choice.Id, choice.Count, item, currency, account));
                }
            }
            Awards = Array.AsReadOnly(awards.Values.OrderBy(row => row.RewardRowId).ThenBy(row => row.Choice).ToArray());
        }

        internal ContractRewardAward GetAward(uint rewardRowId, uint choice) => awards.GetValueOrDefault((rewardRowId, choice));
        private static InvalidDataException Invalid(string message) => new(message);
    }

    internal sealed record ContractItemAward(uint ItemId, uint Count, bool ForceSoulbind);
    internal sealed record ContractCharacterCurrencyAward(CurrencyType Type, ulong Amount, ulong CapAmount);
    internal sealed record ContractAccountCurrencyAward(uint AccountItemId, AccountCurrencyType Type, ulong Amount);
    internal sealed record ContractRewardAward(uint TrackId, uint TrackType, byte Ordinal, uint RequiredPoints,
        uint RewardRowId, uint Choice, uint ChoiceType, uint ChoiceId, uint ChoiceCount,
        ContractItemAward Item, ContractCharacterCurrencyAward CharacterCurrency, ContractAccountCurrencyAward AccountCurrency);
}
