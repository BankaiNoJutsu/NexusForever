using NexusForever.Database.Character;

namespace NexusForever.Game.RealmBank
{
    // Only Stage crosses to the SQL worker. Actor settlement and publication
    // share the bank batch's original-owner checks and quarantine barrier.
    internal interface IBankSaveCompanion
    {
        bool CanExecuteWithoutItems => false;
        void Stage(CharacterContext context);
        void Adopt();
        bool ReleaseForOrdinarySave() => false;
        void Complete();
        void Abort();
        void Quarantine();
    }
}
