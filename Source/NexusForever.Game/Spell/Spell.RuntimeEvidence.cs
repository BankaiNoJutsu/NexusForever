using NexusForever.Game.Spell.Effect;
using NexusForever.Game.Static.Spell;
using NexusForever.Network.World.Message.Model;

namespace NexusForever.Game.Spell
{
    public partial class Spell
    {
        private readonly List<PendingDiagnosticSpellBroadcast> pendingDiagnosticSpellBroadcasts = new();

        private sealed record PendingDiagnosticSpellBroadcast(uint TargetId, string Reason, uint BlockedSpell4EffectId, SpellEffectType BlockedEffectType);

        private void QueueDiagnosticSpellBroadcast(uint targetId, string reason, SpellEffectInterpretation effect)
        {
            if (!Parameters.EmitDiagnosticSpellBroadcasts)
                return;

            pendingDiagnosticSpellBroadcasts.Add(new PendingDiagnosticSpellBroadcast(
                targetId,
                reason,
                effect.Entry.Id,
                effect.Entry.EffectType));
        }

        private void SendDiagnosticSpellBroadcasts()
        {
            if (!Parameters.EmitDiagnosticSpellBroadcasts || pendingDiagnosticSpellBroadcasts.Count == 0)
                return;

            List<PendingDiagnosticSpellBroadcast> diagnostics = pendingDiagnosticSpellBroadcasts
                .GroupBy(diagnostic => diagnostic.TargetId)
                .Select(group => group.First())
                .ToList();

            pendingDiagnosticSpellBroadcasts.Clear();

            var message = new Server07FB
            {
                CastingId = CastingId,
                unknownStructure0 = diagnostics.Select(diagnostic => new Server07FB.UnknownStructure0
                {
                    CasterId = diagnostic.TargetId,
                    Unknown4 = 0,
                    Unknown5 = 0u
                }).ToList()
            };

            SpellRuntimeEvidenceCollector.RecordPacketEvent(
                this,
                nameof(Server07FB),
                string.Join(", ", diagnostics.Select(diagnostic => $"{diagnostic.TargetId}:{diagnostic.Reason}:{diagnostic.BlockedEffectType}")),
                entryCount: message.unknownStructure0.Count);

            log.Debug(
                "SpellDiagnostics diagnostic-broadcast packet={0} castingId={1} entries={2}",
                nameof(Server07FB),
                CastingId,
                message.unknownStructure0.Count);

            Caster.EnqueueToVisible(message, true);
        }
    }
}