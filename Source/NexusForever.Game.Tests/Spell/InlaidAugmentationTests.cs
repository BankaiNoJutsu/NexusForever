using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Spell;
using NexusForever.Game.Entity;
using NexusForever.Game.Prerequisite;
using NexusForever.Game.Prerequisite.Check;
using NexusForever.Game.Static.Prerequisite;
using NexusForever.Game.Static.Spell;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Abilities;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Spell;

// Evidence (Decomp/Analysis/PREREQUISITE_231_INLAID_AUGMENTATION_20261107.md): the client keeps a list of unlocked inlaid augmentation ids (player-create packet, replaced by packet 0x019C, capped at 54), prerequisite
// type 231 tests membership (all 241 retail slots are NotEqual: offered only while not yet unlocked) and the UnlockInlaidAugment effect's DataBits00 is that id (235 of the 240 ids in the prerequisite rows).
// The server kept no such list. Status: Implemented, inferred (the effect-to-list link is data-correlated, not read from the client).
[Collection(MissingGameDataDiagnosticsCollection.Name)]
public class InlaidAugmentationTests
{
    [Fact]
    public void TheManagerLoadsThePersistedIdsInAscendingOrder()
    {
        var model = new CharacterModel { Id = 42 };
        model.InlaidAugmentation.Add(new CharacterInlaidAugmentationModel { Id = 42, AugmentationId = 300 });
        model.InlaidAugmentation.Add(new CharacterInlaidAugmentationModel { Id = 42, AugmentationId = 7 });

        var manager = new InlaidAugmentationManager(model);

        Assert.Equal(new ushort[] { 7, 300 }, manager.Ids);
        Assert.True(manager.IsUnlocked(7));
        Assert.False(manager.IsUnlocked(8));
    }

    [Fact]
    public void UnlockRejectsZeroDuplicatesAndAFullList()
    {
        var manager = new InlaidAugmentationManager(new CharacterModel { Id = 42 });

        Assert.False(manager.Unlock(0));
        Assert.True(manager.Unlock(5));
        Assert.False(manager.Unlock(5));

        for (ushort id = 100; manager.Ids.Count < IInlaidAugmentationManager.MaxIds; id++)
            Assert.True(manager.Unlock(id));

        Assert.Equal(IInlaidAugmentationManager.MaxIds, manager.Ids.Count);
        Assert.False(manager.Unlock(900));
        Assert.False(manager.IsUnlocked(900));
        Assert.False(manager.Unlock(5));      // already held ids stay rejected, not re-added
    }

    [Fact]
    public async Task SavePersistsOnlyNewIdsAndMarksThemPersistedAfterACommit()
    {
        using var f = new Fixture(null); f.Seed();
        var manager = new InlaidAugmentationManager(new CharacterModel { Id = 42 });
        manager.Unlock(12);
        manager.Unlock(3);

        await using (var context = f.Context())
        {
            manager.Save(context);
            await context.SaveChangesAsync();
        }

        using (var read = f.Context())
            Assert.Equal(new ushort[] { 3, 12 }, read.CharacterInlaidAugmentation.AsNoTracking().OrderBy(row => row.AugmentationId).Select(row => row.AugmentationId));

        // a second save stages nothing for the ids that are already persisted
        manager.Unlock(20);
        await using (var context = f.Context())
        {
            manager.Save(context);
            Assert.Single(context.ChangeTracker.Entries<CharacterInlaidAugmentationModel>());
            await context.SaveChangesAsync();
        }

        using var after = f.Context();
        Assert.Equal(3, after.CharacterInlaidAugmentation.Count());
    }

    [Fact]
    public async Task ASaveThatDoesNotCommitLeavesTheIdsPendingForTheNextSave()
    {
        using var f = new Fixture(null); f.Seed();
        var manager = new InlaidAugmentationManager(new CharacterModel { Id = 42 });
        manager.Unlock(12);

        await using (var abandoned = f.Context())
            manager.Save(abandoned);      // disposed without saving

        using (var read = f.Context())
            Assert.Empty(read.CharacterInlaidAugmentation);

        await using (var retry = f.Context())
        {
            manager.Save(retry);
            await retry.SaveChangesAsync();
        }

        using var after = f.Context();
        Assert.Equal(12, Assert.Single(after.CharacterInlaidAugmentation).AugmentationId);
    }

    [Theory]
    [InlineData(PrerequisiteComparison.NotEqual, 442u, true)]      // not unlocked: offered
    [InlineData(PrerequisiteComparison.NotEqual, 7u, false)]       // unlocked: no longer offered
    [InlineData(PrerequisiteComparison.Equal, 7u, true)]
    [InlineData(PrerequisiteComparison.Equal, 442u, false)]
    [InlineData(PrerequisiteComparison.GreaterThan, 7u, false)]
    public void TheCheckTestsMembershipOfObjectId(PrerequisiteComparison comparison, uint objectId, bool expected)
    {
        var manager = new InlaidAugmentationManager(new CharacterModel { Id = 42 });
        manager.Unlock(7);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.InlaidAugmentationManager), manager);

        Assert.Equal(expected, new PrerequisiteCheckEldanAugmentation231().Meets(player, comparison, 0u, objectId, new PrerequisiteParameters()));
    }

    [Fact]
    public void TheCheckHandlesAMissingManagerAndAnIdBeyondTheWireRange()
    {
        IPlayer noManager = RecordingDispatchProxy<IPlayer>.Create(out _);
        var check = new PrerequisiteCheckEldanAugmentation231();

        Assert.True(check.Meets(noManager, PrerequisiteComparison.NotEqual, 0u, 5u, new PrerequisiteParameters()));
        Assert.False(check.Meets(noManager, PrerequisiteComparison.Equal, 0u, 5u, new PrerequisiteParameters()));
        Assert.False(check.Meets(null, PrerequisiteComparison.NotEqual, 0u, 5u, new PrerequisiteParameters()));

        var manager = new InlaidAugmentationManager(new CharacterModel { Id = 42 });
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var proxy);
        proxy.SetProperty(nameof(IPlayer.InlaidAugmentationManager), manager);
        Assert.False(check.Meets(player, PrerequisiteComparison.Equal, 0u, 70000u, new PrerequisiteParameters()));
    }

    [Fact]
    public void TheEffectUnlocksTheIdAndSendsTheFullList()
    {
        var manager = new InlaidAugmentationManager(new CharacterModel { Id = 42 });
        manager.Unlock(3);
        IPlayer player = Player(manager, out var sessionProxy);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectUnlockInlaidAugment(Spell(player), player, Info(9u));

        Assert.True(manager.IsUnlocked(9));
        var sent = Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var packet = Assert.IsType<ServerActionSetUInt16List>(sent.Arguments[0]);
        Assert.Equal(new ushort[] { 3, 9 }, packet.UnlockedInlaidAugmentationIds);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(70000u)]
    [InlineData(3u)]      // already unlocked
    public void TheEffectIgnoresZeroOutOfRangeAndAlreadyUnlockedIds(uint dataBits)
    {
        var manager = new InlaidAugmentationManager(new CharacterModel { Id = 42 });
        manager.Unlock(3);
        IPlayer player = Player(manager, out var sessionProxy);

        global::NexusForever.Game.Spell.SpellHandler.HandleEffectUnlockInlaidAugment(Spell(player), player, Info(dataBits));

        Assert.Equal(new ushort[] { 3 }, manager.Ids);
        Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
    }

    private static ISpell Spell(IPlayer caster)
    {
        ISpellBaseInfo baseInfo = RecordingDispatchProxy<ISpellBaseInfo>.Create(out var baseInfoProxy);
        baseInfoProxy.SetProperty(nameof(ISpellBaseInfo.Entry), new Spell4BaseEntry { Id = 70u });

        ISpellInfo spellInfo = RecordingDispatchProxy<ISpellInfo>.Create(out var spellInfoProxy);
        spellInfoProxy.SetProperty(nameof(ISpellInfo.Entry), new Spell4Entry { Id = 700u });
        spellInfoProxy.SetProperty(nameof(ISpellInfo.BaseInfo), baseInfo);

        ISpellParameters parameters = RecordingDispatchProxy<ISpellParameters>.Create(out var parametersProxy);
        parametersProxy.SetProperty(nameof(ISpellParameters.SpellInfo), spellInfo);

        ISpell spell = RecordingDispatchProxy<ISpell>.Create(out var spellProxy);
        spellProxy.SetProperty(nameof(ISpell.Caster), caster);
        spellProxy.SetProperty(nameof(ISpell.CastingId), 7u);
        spellProxy.SetProperty(nameof(ISpell.Parameters), parameters);
        return spell;
    }

    private static IPlayer Player(IInlaidAugmentationManager manager, out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        playerProxy.SetProperty(nameof(IPlayer.InlaidAugmentationManager), manager);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static ISpellTargetEffectInfo Info(uint dataBits00)
    {
        ISpellTargetEffectInfo info = RecordingDispatchProxy<ISpellTargetEffectInfo>.Create(out var proxy);
        proxy.SetProperty(nameof(ISpellTargetEffectInfo.Entry), new Spell4EffectsEntry
        {
            Id         = 1u,
            EffectType = SpellEffectType.UnlockInlaidAugment,
            DataBits00 = dataBits00
        });
        return info;
    }
}
