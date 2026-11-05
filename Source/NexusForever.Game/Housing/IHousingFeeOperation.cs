using NexusForever.Database.Character;

namespace NexusForever.Game.Housing
{
    // The two coupled housing fees share the existing frozen Character actor;
    // each operation still supplies its own narrowly typed SQL authorization.
    internal interface IHousingFeeOperation
    {
        CharacterSaveReconciliation CreateReceipt(bool primary);
        void Stage(CharacterContext context);
        void PrepareEffects();
        void ApplyCommittedEffects();
    }
}
