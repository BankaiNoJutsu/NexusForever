using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Spell;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Spell;

namespace NexusForever.Game.Tests.Spell;

public class CharacterSpellRapidTapComboTests
{
    private const uint PrimaryTargetId = 12345u;
    private const uint ClientContextToken = 54321u;

    public static IEnumerable<object[]> KnownTierOneCombos()
    {
        yield return [18309u, new uint[] { 32078u, 32079u, 32080u, 32078u }];
        yield return [37968u, new uint[] { 58524u, 66987u, 71125u, 71126u, 58524u }];
        yield return [21613u, new uint[] { 36009u, 36062u, 36064u, 36009u }];
        yield return [23148u, new uint[] { 38765u, 38766u, 38765u }];
        yield return [21056u, new uint[] { 35356u, 35357u, 35358u, 35359u, 35356u }];
        yield return [21650u, new uint[] { 36052u, 36053u, 36055u, 36052u }];
        yield return [53001u, new uint[] { 76834u, 76835u, 76836u, 76837u, 76834u }];
        yield return [21677u, new uint[] { 36085u, 36054u, 36089u, 36085u }];
    }

    [Theory]
    [MemberData(nameof(KnownTierOneCombos))]
    public void GetSpellInfoForCast_WithKnownRapidTapComboCommitsOrderedStageSpell4Ids(uint rootSpell4BaseId, uint[] expectedSpell4Ids)
    {
        CharacterSpell characterSpell = CreateComboCharacterSpell(rootSpell4BaseId, 1);

        uint[] actualSpell4Ids = expectedSpell4Ids
            .Select(_ => CastAndCommit(characterSpell))
            .ToArray();

        Assert.Equal(expectedSpell4Ids, actualSpell4Ids);
    }

    [Fact]
    public void GetSpellInfoForCast_WithRelentlessTierFourIncludesFourthStage()
    {
        CharacterSpell characterSpell = CreateComboCharacterSpell(18309u, 4);

        uint[] actualSpell4Ids = Enumerable.Range(0, 5)
            .Select(_ => CastAndCommit(characterSpell))
            .ToArray();

        Assert.Equal([48045u, 48056u, 48067u, 79629u, 48045u], actualSpell4Ids);
    }

    [Fact]
    public void GetSpellInfoForCast_WithSpellslingerTierEightIncludesExtraTapStage()
    {
        CharacterSpell characterSpell = CreateComboCharacterSpell(21056u, 8);

        uint[] actualSpell4Ids = Enumerable.Range(0, 6)
            .Select(_ => CastAndCommit(characterSpell))
            .ToArray();

        Assert.Equal([35356u, 35357u, 35358u, 35359u, 38937u, 35356u], actualSpell4Ids);
    }

    [Fact]
    public void GetSpellInfoForCast_DoesNotAdvanceAfterFailedCast()
    {
        CharacterSpell characterSpell = CreateComboCharacterSpell(18309u, 1);
        ISpellInfo firstSpellInfo = characterSpell.GetSpellInfoForCast();

        characterSpell.CompleteSpellInfoCast(firstSpellInfo, CastResult.SpellCooldown);

        Assert.Equal(32078u, characterSpell.GetSpellInfoForCast().Entry.Id);

        characterSpell.CompleteSpellInfoCast(firstSpellInfo, CastResult.Ok);

        Assert.Equal(32079u, characterSpell.GetSpellInfoForCast().Entry.Id);
    }

    [Theory]
    [InlineData(23523u, 1, 39180u)]
    [InlineData(23587u, 1, 39246u)]
    [InlineData(37920u, 4, 58439u)]
    public void GetSpellInfoForCast_WithConditionalFollowUpRowsDoesNotBlindCycle(uint rootSpell4BaseId, byte tier, uint expectedSpell4Id)
    {
        CharacterSpell characterSpell = CreateComboCharacterSpell(rootSpell4BaseId, tier);

        uint[] actualSpell4Ids = Enumerable.Range(0, 3)
            .Select(_ => CastAndCommit(characterSpell))
            .ToArray();

        Assert.Equal([expectedSpell4Id, expectedSpell4Id, expectedSpell4Id], actualSpell4Ids);
    }

    [Fact]
    public void Cast_WithRapidTapComboPreservesEquippedRootSpellInfo()
    {
        CharacterSpell characterSpell = CreateComboCharacterSpell(18309u, 1, out RecordingDispatchProxy<IPlayer> playerProxy);

        characterSpell.Cast(nameof(CharacterSpellRapidTapComboTests));

        RecordingDispatchProxy<IPlayer>.Invocation invocation =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
        Assert.Equal(32078u, Assert.IsType<uint>(invocation.Arguments[0]));
        SpellParameters parameters = Assert.IsType<SpellParameters>(invocation.Arguments[1]);
        Assert.Equal(32078u, parameters.SpellInfo.Entry.Id);
        Assert.Equal(32078u, parameters.RootSpellInfo.Entry.Id);
        Assert.Same(characterSpell, parameters.CharacterSpell);
        Assert.True(parameters.UserInitiatedSpellCast);
    }

    [Fact]
    public void GetSpellInfoForCast_WithUnknownRapidTapBaseReturnsRootSpellInfo()
    {
        CharacterSpell characterSpell = CreateSingleCharacterSpell(999u, 1000u, SpellCastMethod.RapidTap);

        Assert.Equal(1000u, characterSpell.GetSpellInfoForCast().Entry.Id);
        Assert.Equal(1000u, characterSpell.GetSpellInfoForCast().Entry.Id);
    }

    [Fact]
    public void GetSpellInfoForCast_WithMappedBaseButNormalCastMethodReturnsRootSpellInfo()
    {
        CharacterSpell characterSpell = CreateSingleCharacterSpell(18309u, 32078u, SpellCastMethod.Normal);

        Assert.Equal(32078u, characterSpell.GetSpellInfoForCast().Entry.Id);
        Assert.Equal(32078u, characterSpell.GetSpellInfoForCast().Entry.Id);
    }

    [Fact]
    public void GetSpellInfoForCast_WithPartialComboTableReturnsRootSpellInfo()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out _);
        ISpellBaseInfo baseInfo = CreateSpellBaseInfo(18309u, SpellCastMethod.RapidTap, new Dictionary<byte, uint>
        {
            [1] = 32078u
        });
        IGlobalSpellManager globalSpellManager = RecordingDispatchProxy<IGlobalSpellManager>.Create(out RecordingDispatchProxy<IGlobalSpellManager> globalSpellManagerProxy);
        globalSpellManagerProxy.SetMethodHandler(nameof(IGlobalSpellManager.GetSpellBaseInfo), _ => throw new ArgumentOutOfRangeException());
        var characterSpell = new CharacterSpell(player, baseInfo, 1, item, globalSpellManager: globalSpellManager);

        Assert.Equal(32078u, characterSpell.GetSpellInfoForCast().Entry.Id);
        Assert.Equal(32078u, characterSpell.GetSpellInfoForCast().Entry.Id);
    }

    [Fact]
    public void CastCharacterSpell_WithNormalCastPacketUsesResolvedComboSpellInfo()
    {
        IWorldSession session = CreateWorldSession(out RecordingDispatchProxy<IPlayer> playerProxy);
        ICharacterSpell characterSpell = RecordingDispatchProxy<ICharacterSpell>.Create(out RecordingDispatchProxy<ICharacterSpell> characterSpellProxy);
        ISpellInfo rootSpellInfo = CreateSpellInfo(32078u, 18309u, 1, null);
        ISpellInfo resolvedSpellInfo = CreateSpellInfo(32079u, 18310u, 1, null);

        characterSpellProxy.SetProperty(nameof(ICharacterSpell.SpellInfo), rootSpellInfo);
        characterSpellProxy.SetMethodReturn(nameof(ICharacterSpell.GetSpellInfoForCast), resolvedSpellInfo);
        playerProxy.SetMethodReturn(nameof(IUnitEntity.TryCastSpell), CastResult.Ok);

        ClientCastSpellHandler.CastCharacterSpell(
            session,
            characterSpell,
            PrimaryTargetId,
            clientContextToken: ClientContextToken,
            clientRequestSource: nameof(ClientCastSpell));

        RecordingDispatchProxy<IPlayer>.Invocation invocation =
            Assert.Single(playerProxy.GetInvocations(nameof(IUnitEntity.TryCastSpell)));
        Assert.Equal(32079u, Assert.IsType<uint>(invocation.Arguments[0]));
        SpellParameters parameters = Assert.IsType<SpellParameters>(invocation.Arguments[1]);
        Assert.Same(resolvedSpellInfo, parameters.SpellInfo);
        Assert.Same(rootSpellInfo, parameters.RootSpellInfo);
        Assert.Equal(PrimaryTargetId, parameters.PrimaryTargetId);
        Assert.Equal(ClientContextToken, parameters.ClientContextToken);
        Assert.Equal(nameof(ClientCastSpell), parameters.ClientRequestSource);
        Assert.True(parameters.UserInitiatedSpellCast);

        RecordingDispatchProxy<ICharacterSpell>.Invocation completeInvocation =
            Assert.Single(characterSpellProxy.GetInvocations(nameof(ICharacterSpell.CompleteSpellInfoCast)));
        Assert.Same(resolvedSpellInfo, completeInvocation.Arguments[0]);
        Assert.Equal(CastResult.Ok, completeInvocation.Arguments[1]);
    }

    private static CharacterSpell CreateComboCharacterSpell(uint rootSpell4BaseId, byte tier)
    {
        return CreateComboCharacterSpell(rootSpell4BaseId, tier, out _);
    }

    private static CharacterSpell CreateComboCharacterSpell(uint rootSpell4BaseId, byte tier, out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        Dictionary<uint, Dictionary<byte, uint>> spellRows = CreateComboSpellRows();
        Dictionary<uint, ISpellBaseInfo> spellBaseInfos = spellRows.ToDictionary(
            p => p.Key,
            p => CreateSpellBaseInfo(p.Key, SpellCastMethod.RapidTap, p.Value));

        IGlobalSpellManager globalSpellManager = RecordingDispatchProxy<IGlobalSpellManager>.Create(out RecordingDispatchProxy<IGlobalSpellManager> globalSpellManagerProxy);
        globalSpellManagerProxy.SetMethodHandler(nameof(IGlobalSpellManager.GetSpellBaseInfo), args => spellBaseInfos[(uint)args[0]]);

        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 1u);
        IItem item = RecordingDispatchProxy<IItem>.Create(out _);
        return new CharacterSpell(player, spellBaseInfos[rootSpell4BaseId], tier, item, globalSpellManager: globalSpellManager);
    }

    private static CharacterSpell CreateSingleCharacterSpell(uint spell4BaseId, uint spell4Id, SpellCastMethod castMethod)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IItem item = RecordingDispatchProxy<IItem>.Create(out _);
        ISpellBaseInfo baseInfo = CreateSpellBaseInfo(spell4BaseId, castMethod, new Dictionary<byte, uint>
        {
            [1] = spell4Id
        });

        return new CharacterSpell(player, baseInfo, 1, item);
    }

    private static ISpellBaseInfo CreateSpellBaseInfo(uint spell4BaseId, SpellCastMethod castMethod, IReadOnlyDictionary<byte, uint> spell4IdsByTier)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out RecordingDispatchProxy<ISpellBaseInfo> baseInfoProxy);
        var spellInfosByTier = new Dictionary<byte, ISpellInfo>();

        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry
        {
            Id         = spell4BaseId,
            CastMethod = (uint)castMethod
        });
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.CastMethod), castMethod);
        baseInfoProxy.SetMethodHandler(nameof(ISpellBaseInfo.GetSpellInfo), args =>
        {
            byte requestedTier = (byte)args[0];
            return spellInfosByTier.GetValueOrDefault(requestedTier);
        });

        foreach ((byte tier, uint spell4Id) in spell4IdsByTier)
            spellInfosByTier[tier] = CreateSpellInfo(spell4Id, spell4BaseId, tier, baseInfo);

        return baseInfo;
    }

    private static ISpellInfo CreateSpellInfo(uint spell4Id, uint spell4BaseId, byte tier, ISpellBaseInfo baseInfo)
    {
        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out RecordingDispatchProxy<ISpellInfo> spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry
        {
            Id                    = spell4Id,
            Spell4BaseIdBaseSpell = spell4BaseId,
            TierIndex             = tier
        });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);
        return spellInfo;
    }

    private static Dictionary<uint, Dictionary<byte, uint>> CreateComboSpellRows()
    {
        return new Dictionary<uint, Dictionary<byte, uint>>
        {
            [18309u] = new() { [1] = 32078u, [4] = 48045u },
            [18310u] = new() { [1] = 32079u, [4] = 48056u },
            [18311u] = new() { [1] = 32080u, [4] = 48067u },
            [55309u] = new() { [1] = 79626u, [4] = 79629u },

            [37968u] = new() { [1] = 58524u },
            [44605u] = new() { [1] = 66987u },
            [47921u] = new() { [1] = 71125u },
            [47922u] = new() { [1] = 71126u },

            [21613u] = new() { [1] = 36009u },
            [21660u] = new() { [1] = 36062u },
            [21662u] = new() { [1] = 36064u },

            [23148u] = new() { [1] = 38765u },
            [23149u] = new() { [1] = 38766u },
            [23523u] = new() { [1] = 39180u },
            [23531u] = new() { [1] = 39188u },
            [23587u] = new() { [1] = 39246u },
            [23592u] = new() { [1] = 39252u },

            [21056u] = new() { [1] = 35356u, [8] = 35356u },
            [21057u] = new() { [1] = 35357u },
            [21058u] = new() { [1] = 35358u },
            [21059u] = new() { [1] = 35359u },
            [23306u] = new() { [1] = 38937u },
            [21650u] = new() { [1] = 36052u },
            [21651u] = new() { [1] = 36053u },
            [21653u] = new() { [1] = 36055u },
            [47751u] = new() { [1] = 70845u },
            [53001u] = new() { [1] = 76834u },
            [53002u] = new() { [1] = 76835u },
            [53003u] = new() { [1] = 76836u },
            [53004u] = new() { [1] = 76837u },
            [62207u] = new() { [1] = 87106u },
            [21677u] = new() { [1] = 36085u },
            [21652u] = new() { [1] = 36054u },
            [21681u] = new() { [1] = 36089u },
            [47750u] = new() { [1] = 70844u },
            [37920u] = new() { [1] = 58428u, [4] = 58439u },
            [37921u] = new() { [1] = 58429u }
        };
    }

    private static uint CastAndCommit(CharacterSpell characterSpell)
    {
        ISpellInfo spellInfo = characterSpell.GetSpellInfoForCast();
        characterSpell.CompleteSpellInfoCast(spellInfo, CastResult.Ok);
        return spellInfo.Entry.Id;
    }

    private static IWorldSession CreateWorldSession(out RecordingDispatchProxy<IPlayer> playerProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        sessionProxy.SetMethodHandler(nameof(IWorldSession.TryConsumeNextClientSpellEvidenceCapture), args =>
        {
            args[0] = false;
            return false;
        });
        return session;
    }
}
