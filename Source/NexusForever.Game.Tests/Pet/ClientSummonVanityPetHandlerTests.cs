using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Pet;

namespace NexusForever.Game.Tests.Pet;

public class ClientSummonVanityPetHandlerTests
{
    private const uint Spell4BaseId = 1234u;

    [Fact]
    public void HandleMessage_WithSummonVanityPetSpell_CastsResolvedTierSpell()
    {
        ISpellInfo spellInfo = CreateSpellInfo(SpellEffectType.SummonVanityPet);
        IWorldSession session = CreateSession(
            spellInfo,
            out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientSummonVanityPetHandler();

        handler.HandleMessage(session, ReadSummonVanityPet(Spell4BaseId));

        RecordingDispatchProxy<IPlayer>.Invocation cast =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
        SpellParameters parameters = Assert.IsType<SpellParameters>(cast.Arguments[0]);
        Assert.Same(spellInfo, parameters.SpellInfo);
    }

    [Fact]
    public void HandleMessage_WithNonVanityPetSpell_ThrowsBeforeCasting()
    {
        IWorldSession session = CreateSession(
            CreateSpellInfo(SpellEffectType.Damage),
            out RecordingDispatchProxy<IPlayer> playerProxy);
        var handler = new ClientSummonVanityPetHandler();

        Assert.Throws<InvalidPacketValueException>(() =>
            handler.HandleMessage(session, ReadSummonVanityPet(Spell4BaseId)));

        Assert.Empty(playerProxy.GetInvocations(nameof(IUnitEntity.CastSpell)));
    }

    private static IWorldSession CreateSession(
        ISpellInfo spellInfo,
        out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        ISpellManager spellManager = RecordingDispatchProxy<ISpellManager>.Create(out RecordingDispatchProxy<ISpellManager> spellManagerProxy);
        ICharacterSpell characterSpell = RecordingDispatchProxy<ICharacterSpell>.Create(out RecordingDispatchProxy<ICharacterSpell> characterSpellProxy);
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        playerProxy.SetProperty(nameof(IPlayer.SpellManager), spellManager);
        characterSpellProxy.SetProperty(nameof(ICharacterSpell.BaseInfo), baseInfo);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = Spell4BaseId });
        baseInfoProxy.SetMethodReturn(nameof(ISpellBaseInfo.GetSpellInfo), spellInfo);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpell), characterSpell);
        spellManagerProxy.SetMethodReturn(nameof(ISpellManager.GetSpellTier), (byte)2);

        return session;
    }

    private static ISpellInfo CreateSpellInfo(SpellEffectType effectType)
    {
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Effects), new List<Spell4EffectsEntry>
        {
            new()
            {
                EffectType = effectType
            }
        });
        return spellInfo;
    }

    private static ClientSummonVanityPet ReadSummonVanityPet(uint spell4BaseId)
    {
        using var stream = new MemoryStream();
        var writer = new GamePacketWriter(stream);
        writer.Write(spell4BaseId, 18u);
        writer.FlushBits();

        using var packetStream = new MemoryStream(stream.ToArray());
        using var reader = new GamePacketReader(packetStream);
        var packet = new ClientSummonVanityPet();
        packet.Read(reader);
        return packet;
    }
}
