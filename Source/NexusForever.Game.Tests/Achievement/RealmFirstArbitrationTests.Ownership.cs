using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Tests.TestSupport;

namespace NexusForever.Game.Tests.Achievement;

public sealed partial class RealmFirstArbitrationTests
{
    [Fact]
    public async Task CompanionCallbackSaveCannotCommitCompletionBeforeItsClaim()
    {
        using var f = await CreateCharacters();
        var archive = RecordingDispatchProxy<IGalacticArchiveManager>.Create(out var callback);
        int saves = 0;
        callback.SetMethodHandler(nameof(IGalacticArchiveManager.RefreshRuleUnlocks), _ =>
        {
            using var context = f.Source.Context();
            f.First.Actor.Player.Save(context); context.SaveChanges(); saves++; return null;
        });
        f.First.Actor.Set(nameof(Player.GalacticArchiveManager), archive);
        f.First.Grant(); Assert.Equal(1, saves);
        using (var read = f.Source.Context())
        {
            Assert.NotNull(Assert.Single(read.CharacterAchievement).DateCompleted);
            Assert.Equal(42ul, Assert.Single(read.RealmFirstAchievement).OwnerId);
        }
        f.Pump(); f.Second.Grant(); await f.Second.Save(); f.Pump();
        await f.First.Save(); f.Pump(); AssertWinner(f, f.First);
    }

    [Fact]
    public async Task UnownedTrackedClosureCannotConsumeACharacterRealmFirst()
    {
        using var f = await CreateCharacters(); f.First.Grant();
        using (var context = f.Source.Context())
        {
            context.RealmFirstAchievement.Add(new RealmFirstAchievementModel { AchievementId = AchievementId });
            Assert.Throws<InvalidOperationException>(() => f.First.Actor.Player.Save(context));
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        using (var read = f.Source.Context()) { Assert.Empty(read.RealmFirstAchievement); Assert.Empty(read.CharacterAchievement); }
        f.Pump(); Assert.All(f.Endpoints, endpoint => Assert.Empty(endpoint.Announcements));
        await f.First.Save(); f.Pump(); AssertWinner(f, f.First);
    }

    [Fact]
    public async Task UnownedTrackedWinnerCannotConsumeAGuildRealmFirst()
    {
        using var f = await CreateGuilds(); f.First.AchievementManager.GrantAchievement(GuildAchievementId);
        using (var context = f.Characters.Source.Context())
        {
            context.RealmFirstAchievement.Add(new RealmFirstAchievementModel
                { IsGuild = true, AchievementId = GuildAchievementId, OwnerId = f.Second.Id, Token = new string('a', 32), Name = f.Second.Name });
            Assert.Throws<InvalidOperationException>(() => f.First.Save(context));
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        }
        using (var read = f.Characters.Source.Context()) { Assert.Empty(read.RealmFirstAchievement); Assert.Empty(read.GuildAchievement); }
        await f.Pump(); Assert.All(f.Characters.Endpoints, endpoint => Assert.Empty(endpoint.Announcements));
        await f.Save(f.First); await f.Pump(); AssertGuildWinner(f, f.First);
    }

    [Fact]
    public async Task TwoOriginalPlayersCanShareTheSameClaimAndSaveContext()
    {
        using var f = await CreateCharacters(); f.First.Grant(); f.Second.Grant();
        using (var context = f.Source.Context())
        {
            f.First.Actor.Player.Save(context); f.Second.Actor.Player.Save(context);
            await context.SaveChangesAsync();
        }
        f.Pump(); AssertWinner(f, f.First);
        using var read = f.Source.Context();
        Assert.Equal(2, await read.CharacterAchievement.CountAsync(row => row.DateCompleted != null));
    }
}
