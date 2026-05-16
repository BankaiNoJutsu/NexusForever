using NexusForever.Game.Abstract.Spell;
using NexusForever.Network.World.Entity;

namespace NexusForever.Game.Spell
{
    public class SpellParameters : ISpellParameters
    {
        public ICharacterSpell CharacterSpell { get; set; }
        public ISpellInfo SpellInfo { get; set; }
        public ISpellInfo ParentSpellInfo { get; set; }
        public ISpellInfo RootSpellInfo { get; set; }
        public bool UserInitiatedSpellCast { get; set; }
        public bool IgnoreGlobalCooldown { get; set; }
        public bool CancelActiveTrade { get; set; }
        public bool UseServiceTokenCost { get; set; }
        public bool CaptureRuntimeEvidence { get; set; }
        public bool EmitDiagnosticSpellBroadcasts { get; set; }
        public uint ClientContextToken { get; set; }
        public string ClientRequestSource { get; set; }
        public uint PrimaryTargetId { get; set; }
        public Position Position { get; set; }
        public ushort TaxiNode { get; set; }
    }
}
