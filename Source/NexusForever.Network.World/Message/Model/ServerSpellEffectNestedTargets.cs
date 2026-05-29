using NexusForever.Network.Message;

namespace NexusForever.Network.World.Message.Model
{
    /// <summary>
    /// Spell broadcast follow-up for opcode <c>0x07F8</c>.
    /// Client reader <c>ServerSpellEffectNestedTargets_ReadPayload</c> @ <c>140095810</c> reads:
    /// <list type="number">
    /// <item><description><see cref="ServerUniqueId"/> — 32-bit spell-cast / server-unique id (same role as <c>CastingId</c> on <c>0x07FF</c>).</description></item>
    /// <item><description><see cref="Spell4EffectId"/> — 19-bit <c>Spell4Effects</c> row id.</description></item>
    /// <item><description><see cref="TargetId"/> — 32-bit entity guid for the unit that receives the following damage rows.</description></item>
    /// <item><description><see cref="DamageDescriptions"/> — 8-bit count, then count × <see cref="ServerSpellEffectDamage.DamageDescription"/> via <c>SpellDamageDescription_ReadPayload</c> @ <c>1400946c0</c>.</description></item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Prior placeholder model (pre-2026-05) used nested <c>UnknownStructure0</c> / <c>UnknownStructure1</c> types.
    /// Those layers flatten to the shared <see cref="ServerSpellEffectDamage.DamageDescription"/> shape used by
    /// <c>0x07F4</c> / <c>0x07F6</c>:
    /// <para>
    /// Header: <c>CastingId</c> → <see cref="ServerUniqueId"/>; <c>Spell4EffectId</c> unchanged;
    /// <c>CasterId</c> → <see cref="TargetId"/> (incorrect “caster” name; this packet has no separate caster guid).
    /// </para>
    /// <para>
    /// Each list element (<c>UnknownStructure0</c>): seven damage uint32s → <c>RawDamage</c> … <c>GlanceAmount</c>;
    /// <c>Unknown25</c> → <c>KilledTarget</c>; <c>Unknown26</c> → <c>CombatResult</c> (4-bit);
    /// <c>Unknown27</c> → <c>DamageType</c> (3-bit); <c>unknownStructure1</c> → <c>TrailingStructures</c>
    /// (8-bit count + rows via <c>SpellDamageTrailingRow_ReadPayload</c> @ <c>1400945e0</c>: same seven damage
    /// uint32 names as the parent row, then a 3-bit tail on each row; gameplay consumer past the reader remains blocked).
    /// </para>
    /// Compared to opcode <c>0x07F6</c> (<see cref="ServerSpellEffectDamage"/>), <c>0x07F8</c> drops the second
    /// entity guid (<c>UnitId</c> on <c>0x07F6</c>, typically caster/source) and emits multiple
    /// <see cref="ServerSpellEffectDamage.DamageDescription"/> rows for one <see cref="TargetId"/> instead of a single row.
    /// </remarks>
    [Message(GameMessageOpcode.ServerSpellEffectNestedTargets)]
    public class ServerSpellEffectNestedTargets : IWritable
    {
        /// <summary>Spell cast server-unique id (<c>ISpell.CastingId</c> / <see cref="ServerSpellGo.ServerUniqueId"/>).</summary>
        public uint ServerUniqueId { get; set; }

        /// <summary><c>Spell4Effects</c> table id for the effect producing these damage rows.</summary>
        public uint Spell4EffectId { get; set; }

        /// <summary>Entity guid of the target receiving every <see cref="DamageDescriptions"/> entry.</summary>
        public uint TargetId { get; set; }

        /// <summary>Per-hit or per-tick damage rows for <see cref="TargetId"/> (multi-row AoE / channel / nested effect).</summary>
        public List<ServerSpellEffectDamage.DamageDescription> DamageDescriptions { get; set; } = new();

        public void Write(GamePacketWriter writer)
        {
            writer.Write(ServerUniqueId);
            writer.Write(Spell4EffectId, 19);
            writer.Write(TargetId);

            writer.Write(DamageDescriptions.Count, 8u);
            DamageDescriptions.ForEach(u => u.Write(writer));
        }
    }
}
