using System;

namespace NexusForever.Database.Auth
{
    public sealed class AuthCommitOutcomeUnknownException(Exception inner)
        : InvalidOperationException("The Auth save outcome could not be confirmed for live adoption.", inner);
}
