using NexusForever.Database.Character;

namespace NexusForever.Game.Housing
{
    // Coupled housing fees and ordinary guild creation reuse the existing frozen
    // Character actor. Each supplies its own narrowly typed SQL authorization;
    // the historical housing names do not confer shared-row ownership.
    internal interface IHousingFeeOperation
    {
        CharacterSaveReconciliation CreateReceipt(bool primary);
        void Stage(CharacterContext context);
        void PrepareEffects();
        void ApplyCommittedEffects();
        void BeforePrimarySave() { }
        void AfterPrimaryRollback() { }
        void PrepareOrdinaryEffects() { }
        bool HasPendingOrdinaryEffects => false;
    }
}
