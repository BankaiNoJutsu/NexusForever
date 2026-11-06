using System.Runtime.CompilerServices;
using System.Threading;

namespace NexusForever.Game.Entity;

public partial class Player
{
    // PlayerSaveMask has the default int backing type. Actor mutations and
    // Character settlement callbacks can update different bits concurrently.
    // Atomic bit operations preserve the other owners without adding lock order.
    private void MarkPlayerSave(PlayerSaveMask mask)
        => Interlocked.Or(ref Unsafe.As<PlayerSaveMask, int>(ref saveMask), (int)mask);

    private void ClearPlayerSave(PlayerSaveMask mask)
        => Interlocked.And(ref Unsafe.As<PlayerSaveMask, int>(ref saveMask), ~(int)mask);
}
