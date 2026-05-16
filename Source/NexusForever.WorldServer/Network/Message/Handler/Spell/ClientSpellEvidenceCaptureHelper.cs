using NexusForever.Game.Abstract.Spell;

namespace NexusForever.WorldServer.Network.Message.Handler.Spell
{
    internal static class ClientSpellEvidenceCaptureHelper
    {
        internal static void ApplyPendingCapture(IWorldSession session, ISpellParameters spellParameters)
        {
            if (session == null || spellParameters == null)
                return;

            if (!session.TryConsumeNextClientSpellEvidenceCapture(out bool emitDiagnosticSpellBroadcasts))
                return;

            spellParameters.CaptureRuntimeEvidence = true;
            spellParameters.EmitDiagnosticSpellBroadcasts = emitDiagnosticSpellBroadcasts;
        }
    }
}