using NexusForever.Game.Abstract.Spell;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;

namespace NexusForever.Script.Instance.Raid.Datascape.Script
{
    /// <summary>
    /// PROVISIONAL boss behaviour for the Datascape raid, disabled by default and never retail parity.
    /// Spell ids come from the Spell4 internal description tags (`[DS] e385`, `e390`, `e395`, `e399`, see
    /// INSTANCE_BOSS_ABILITY_CATALOG_20261109.md); creature ids were checked by name against Creature2
    /// (30495 Null System Daemon, 30496 Binary System Daemon, 30497 Maelstrom Authority, 30504 and 60068
    /// Mnemesis, 30505 / 60057 / 60058 Avatus). Abilities come from INSTANCE_DATASCAPE_PACKET_20261109.md,
    /// which is agent-read: only System Daemons has two narrated sources, every other boss is raid-callout
    /// vocabulary, so those plans are thinner and weaker. Delays and intervals are ESTIMATES; no health
    /// thresholds are used (the System Daemons 70% and 30% pillar phases are single-source).
    ///
    /// Skipped bosses:
    ///  - Gloomclaw (e393): every catalog row is an add or hazard cast (Ravager, Strain Parasite, Asura,
    ///    Claw Skurge); the packet reports ravagers, eggs, orbs and rupture as vocabulary only, with no
    ///    boss-cast row bound.
    ///  - Limbo Infomatrix (e392): an escape event against Keepers of Sands, no boss skeleton, rows are
    ///    Void Desert hazards.
    ///  - Avatus attention (e394): the Volatility Lattice escape event, not a boss kill; no boss clip read.
    ///  - Abstract Augmentation Algorithm (e1888): its four catalog rows are Escher Room beams and auras;
    ///    the clips only give interrupt and "Fusion" call-outs, so no row matches a reported ability.
    ///  - Elemental Guardians other than Mnemesis: Aileron's Twirl row is the Life/Water variant (the
    ///    Earth pairing uses a different row) and creature-id filters cannot tell the pair, and the Earth
    ///    and Water abilities have no `e395` rows.
    ///
    /// Not modelled: System Daemons Power Surge (no e385 base row), Data Drain, System Restore (a reaction
    /// to a health gap above 5%, not periodic), the 70%/30% pillar phases, ad waves, the firewall meter,
    /// modules and Upload/Download rings; Maelstrom Authority weather stations, cyclones and the weather
    /// cycle; the Mnemesis partner boss and its pair-specific mechanics; Avatus lasers, grid, portals,
    /// holograms and the phase list; Gloomclaw and the wing minibosses entirely.
    /// </summary>
    internal static class DatascapeProvisionalPlans
    {
        public const uint SystemDaemonsPurge        = 44099u; // [DS] e385 Eldan Sentinels - Purge - Base
        public const uint SystemDaemonsMemoryWipe   = 43454u; // [DS] e385 Eldan Sentinels - Memory Wipe - Base
        public const uint MaelstromLightningRod     = 44437u; // [DS] e390 Air Boss - Lightning Rod - Base
        public const uint AvatusSummonHoloHands     = 44860u; // [DS] e399 Avatus - Summon Holo Hands - Base n
        public const uint AvatusSummonHoloCannons   = 44876u; // [DS] e399 Avatus - Summon Holo Cannons - Base n

        public static ProvisionalEncounterPlan SystemDaemons() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(SystemDaemonsPurge, 20d, 45d),
                new ProvisionalCast(SystemDaemonsMemoryWipe, 42d, 60d, targetCurrentTarget: true)
            ], "Purge, Disconnect and Memory Wipe as named in two narrated guides (packet section 1); the 60 s disconnect cadence is single-source and every interval is ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan MaelstromAuthority() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(MaelstromLightningRod, 15d, 30d)
            ], "Lightning rods called out in two 2014 clips (comms only, not narrated); the row binding is by tag name and the interval is ESTIMATED")
        ]);

        public static ProvisionalEncounterPlan Avatus() => new(
        [
            new ProvisionalPhase(null,
            [
                new ProvisionalCast(AvatusSummonHoloHands, 15d, 60d),
                new ProvisionalCast(AvatusSummonHoloCannons, 40d, 60d)
            ], "Hands and cannons are the dominant call-out words across seven clips (comms only, no phase order); intervals are ESTIMATED")
        ]);
    }

    /// <summary>
    /// Provisional System Daemons casts; see <see cref="DatascapeProvisionalPlans"/>. The two daemons are
    /// fought together and the packet does not attribute abilities to one of them, so each runs this plan.
    /// </summary>
    [ScriptFilterCreatureId(30495u, 30496u)]
    public class SystemDaemonsProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public SystemDaemonsProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, DatascapeProvisionalPlans.SystemDaemons())
        {
        }
    }

    /// <summary>
    /// Provisional Maelstrom Authority casts; see <see cref="DatascapeProvisionalPlans"/>.
    /// </summary>
    [ScriptFilterCreatureId(30497u)]
    public class MaelstromAuthorityProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public MaelstromAuthorityProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, DatascapeProvisionalPlans.MaelstromAuthority())
        {
        }
    }

    /// <summary>
    /// Provisional Avatus casts; see <see cref="DatascapeProvisionalPlans"/>. 60057 and 60058 are second
    /// Avatus rows (possibly the other raid size); which size uses which is not established.
    /// </summary>
    [ScriptFilterCreatureId(30505u, 60057u, 60058u)]
    public class AvatusProvisionalEncounterScript : ProvisionalEncounterEntityScript
    {
        public AvatusProvisionalEncounterScript(IFactory<ISpellParameters> spellParametersFactory)
            : base(spellParametersFactory, DatascapeProvisionalPlans.Avatus())
        {
        }
    }
}
