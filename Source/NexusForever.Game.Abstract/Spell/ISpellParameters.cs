using NexusForever.Network.World.Entity;

namespace NexusForever.Game.Abstract.Spell
{
    public interface ISpellParameters
    {
        ICharacterSpell CharacterSpell { get; set; }
        ISpellInfo SpellInfo { get; set; }
        ISpellInfo ParentSpellInfo { get; set; }
        ISpellInfo RootSpellInfo { get; set; }
        bool UserInitiatedSpellCast { get; set; }
        bool IgnoreGlobalCooldown { get; set; }
        bool CancelActiveTrade { get; set; }
        bool UseServiceTokenCost { get; set; }
        bool CaptureRuntimeEvidence { get; set; }
        bool EmitDiagnosticSpellBroadcasts { get; set; }
        bool UseCreatureOverrides { get; set; }
        bool SkipPrimaryTargetRangeValidation { get; set; }
        bool DeferActivateEffectObjectiveCredit { get; set; }
        bool WaitForClientSideInteractionResponse { get; set; }
        uint ClientSideInteractionDurationMs { get; set; }
        System.Action<ISpell, bool> FinishCallback { get; set; }
        uint ClientContextToken { get; set; }
        string ClientRequestSource { get; set; }
        uint PrimaryTargetId { get; set; }
        Position Position { get; set; }
        ushort TaxiNode { get; set; }
    }
}
