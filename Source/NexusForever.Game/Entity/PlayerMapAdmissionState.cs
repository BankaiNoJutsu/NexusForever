namespace NexusForever.Game.Entity;

/// <summary>Serializes admission, acknowledgement and cancellation for one player object.</summary>
internal sealed class PlayerMapAdmissionState
{
    public object SyncRoot { get; } = new();
    public ulong Generation { get; private set; }
    public bool Canceled { get; private set; }
    public bool Queued { get; private set; }
    public ulong? BootstrapGeneration { get; private set; }
    public bool HasUnconfirmedLocation { get; private set; }
    private bool ready;
    private bool entered;
    private bool deferred;

    public ulong Queue()
    {
        lock (SyncRoot)
        {
            if (Canceled)
                return Generation;
            Generation++;
            Queued = true;
            ready = entered = deferred = false;
            return Generation;
        }
    }

    public bool IsCurrent(ulong generation) => !Canceled && Generation == generation;

    public ulong? BeginBootstrap()
    {
        if (Canceled || !Queued || BootstrapGeneration.HasValue)
            return null;
        Queued = false;
        BootstrapGeneration = Generation;
        HasUnconfirmedLocation = true;
        return Generation;
    }

    public bool RequestEntry(bool mapAssigned)
    {
        if (Canceled || !mapAssigned || entered)
            return false;
        if (BootstrapGeneration == Generation)
        {
            deferred = true;
            return false;
        }
        if (!ready)
            return false;
        entered = true;
        ready = false;
        return true;
    }

    public bool FinishBootstrap(ulong generation, bool successful)
    {
        if (BootstrapGeneration != generation)
            return false;
        BootstrapGeneration = null;
        if (!successful || !IsCurrent(generation))
            return false;
        HasUnconfirmedLocation = false;
        ready = true;
        return deferred && RequestEntry(true);
    }

    public void AbortQueue()
    {
        Generation++;
        Queued = ready = deferred = false;
        entered = true;
    }

    public void Cancel()
    {
        Canceled = true;
        Generation++;
        Queued = ready = entered = deferred = false;
        // Keep the active bootstrap marker until its finally block releases it.
    }
}
