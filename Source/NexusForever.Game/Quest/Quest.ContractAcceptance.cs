namespace NexusForever.Game.Quest
{
    public partial class Quest
    {
        private string contractAcceptanceId;
        internal string ContractAcceptanceId { get { lock (persistence.Gate) return contractAcceptanceId; } }

        internal static bool ValidContractAcceptanceId(string value)
            => value != null && Guid.TryParseExact(value, "N", out var id) && id != Guid.Empty && id.ToString("N") == value;

        // An identity is allocated at actual acceptance, never completion/retry.
        // Existing active NULL rows stay legacy; a duplicate active add must not
        // mint another generation. The same normal/reward snapshots own this
        // scalar alongside the Quest, including rollback and later actor intent.
        internal void BeginContractAcceptance(bool retainActive, bool supported)
        {
            lock (persistence.Gate)
            {
                if (retainActive) return;
                string next = supported && Info.IsContract() ? Guid.NewGuid().ToString("N") : null;
                if (next == contractAcceptanceId) return;
                contractAcceptanceId = next;
                persistence.Dirty((int)QuestSaveMask.ContractAcceptance);
            }
            player.RequestSave();
        }
    }
}
