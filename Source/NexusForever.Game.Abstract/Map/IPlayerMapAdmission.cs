namespace NexusForever.Game.Abstract.Map;

/// <summary>Optional lifecycle ownership for queued player map admissions.</summary>
public interface IPlayerMapAdmission
{
    object MapAdmissionSyncRoot { get; }
    ulong MapAdmissionGeneration { get; }
    ulong BeginMapAdmission();
    bool IsMapAdmissionCurrent(ulong generation);
    void CompleteMapAdmission(ulong generation, bool successful);
}
