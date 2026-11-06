using Microsoft.EntityFrameworkCore;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model.Achievement;

namespace NexusForever.Game.Tests.Guild;

public sealed partial class GuildCreationFinanceTests
{
    [Fact]
    public async Task CharacterFeedbackCannotCommitAheadOfItsRejectedGuildCompletion()
    {
        var result = await ProbeGuildFeedbackCommit(
            new AchievementEntry { Id = 600, Flags = (uint)AchievementFlags.Guild,
                AchievementTypeId = (uint)AchievementType.KillCreatureEntry, ObjectId = 500, RequiredProgress = 1 },
            new AchievementEntry { Id = 601, AchievementTypeId = (uint)AchievementType.AchievementComplete, ObjectId = 600, RequiredProgress = 2 }, []);
        Assert.Equal(0, result.FirstGuildRows);
        Assert.Equal(0, result.FirstCharacterRows);
        Assert.Equal(0, result.FirstCharacterPackets);
        Assert.Equal(1, result.FinalGuildRows);
        Assert.Equal(1, result.FinalCharacterRows);
        Assert.Equal(1u, result.FinalProgress);
        Assert.Null(result.FinalCompleted);
        Assert.Equal(1, result.FinalCharacterPackets);
    }

    internal sealed record GuildFeedbackProbe(int FirstGuildRows, int FirstCharacterRows, int FirstCharacterPackets,
        int FinalGuildRows, int FinalCharacterRows, uint FinalProgress, uint FinalChecklist, DateTime? FinalCompleted, int FinalCharacterPackets);

    internal async Task<GuildFeedbackProbe> ProbeGuildFeedbackCommit(AchievementEntry guildEntry, AchievementEntry characterEntry,
        AchievementChecklistEntry[] checklist)
    {
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var prepared);
        prepared.SetMethodReturn(nameof(IPrerequisiteManager.Meets), true);
        using var f = await Create(achievementPrerequisites: prerequisites);
        f.Register(); await f.Flush();
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Tables).SetProperty(nameof(IGameTableManager.AchievementChecklist), PlayerBindPointTests.Table(checklist));
        var guildInfo = new AchievementInfo(guildEntry, f.Tables);
        var characterInfo = new AchievementInfo(characterEntry, f.Tables);
        var catalog = (RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements;
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args =>
            (ushort)args[0] == guildEntry.Id ? guildInfo : (ushort)args[0] == characterEntry.Id ? characterInfo : null);
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetGuildAchievements), args =>
            (uint)(AchievementType)args[0] == guildEntry.AchievementTypeId ? new IAchievementInfo[] { guildInfo } : []);
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
            (AchievementType)args[0] == AchievementType.AchievementComplete ? new IAchievementInfo[] { characterInfo } : []);
        using (var setup = f.Source.Context())
            await setup.Database.ExecuteSqlRawAsync(setup.Database.IsMySql()
                ? "CREATE TRIGGER reject_parent_completion BEFORE INSERT ON guild_achievement FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'owned guild completion failure'"
                : "CREATE TRIGGER reject_parent_completion BEFORE INSERT ON guild_achievement BEGIN SELECT RAISE(ABORT, 'owned guild completion failure'); END");
        var guild = f.Actor.Player.GuildManager.Guild;
        var guildChecks = checklist.Where(c => c.AchievementId == guildEntry.Id).ToArray();
        if (guildChecks.Length == 0)
            f.Actor.Player.AchievementManager.CheckAchievements(f.Actor.Player, (AchievementType)guildEntry.AchievementTypeId, guildEntry.ObjectId, guildEntry.ObjectIdAlt);
        else
            foreach (var item in guildChecks)
                guild.AchievementManager.CheckAchievements(f.Actor.Player, (AchievementType)guildEntry.AchievementTypeId, item.ObjectId, item.ObjectIdAlt);
        Assert.True(guild.AchievementManager.HasCompletedAchievement((ushort)guildEntry.Id));
        await f.Flush();
        f.Actor.StartSave(); await f.Actor.Drain(allowErrors: true);
        int CharacterPackets() => f.Packets<ServerAchievementUpdate>().Count(p => p.Achievements.Any(a => a.AchievementId == characterEntry.Id));
        int firstGuild, firstCharacter;
        using (var first = f.Source.Context())
        {
            firstGuild = first.GuildAchievement.Count(row => row.AchievementId == guildEntry.Id);
            firstCharacter = first.CharacterAchievement.Count(row => row.AchievementId == characterEntry.Id);
            await first.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_parent_completion");
        }
        int firstPackets = CharacterPackets();
        await f.Flush(); f.Actor.StartSave(); await f.Actor.Drain();
        await f.Flush(); f.Actor.StartSave(); await f.Actor.Drain();
        using var final = f.Source.Context();
        var row = final.CharacterAchievement.SingleOrDefault(row => row.AchievementId == characterEntry.Id);
        return new(firstGuild, firstCharacter, firstPackets,
            final.GuildAchievement.Count(row => row.AchievementId == guildEntry.Id), row == null ? 0 : 1,
            row?.ProgressState ?? 0, row?.CreditedChecklistMask ?? 0, row?.DateCompleted, CharacterPackets());
    }

    internal sealed record GuildFeedbackChecklistProbe(uint PartialProgress, bool PartialCompleted, uint FinalProgress,
        bool FinalCompleted, int GuildCompletions, int CharacterRows, int IntentReceipts, int DeliveryReceipts, int CharacterPackets);

    internal async Task<GuildFeedbackChecklistProbe> ProbeGuildFeedbackChecklist(AchievementEntry[] parents,
        AchievementEntry child, AchievementChecklistEntry[] checks)
    {
        var prerequisites = RecordingDispatchProxy<IPrerequisiteManager>.Create(out var prepared); prepared.SetMethodReturn(nameof(IPrerequisiteManager.Meets), true);
        using var f = await Create(achievementPrerequisites: prerequisites); f.Register(); await f.Flush();
        ((RecordingDispatchProxy<IGameTableManager>)(object)f.Tables).SetProperty(nameof(IGameTableManager.AchievementChecklist), PlayerBindPointTests.Table(checks));
        var infos = parents.Append(child).Select(entry => new AchievementInfo(entry, f.Tables)).ToDictionary(info => info.Id);
        var catalog = (RecordingDispatchProxy<IGlobalAchievementManager>)(object)f.Achievements;
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetAchievement), args => infos.GetValueOrDefault((ushort)args[0]));
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetGuildAchievements), args => infos.Values
            .Where(info => !info.IsPlayerAchievement && info.Entry.AchievementTypeId == (uint)(AchievementType)args[0]).Cast<IAchievementInfo>().ToArray());
        catalog.SetMethodHandler(nameof(IGlobalAchievementManager.GetCharacterAchievements), args =>
            (AchievementType)args[0] == AchievementType.AchievementComplete ? new IAchievementInfo[] { infos[(ushort)child.Id] } : []);
        uint partial = 0; bool partialCompleted = false;
        foreach (AchievementEntry parent in parents)
        {
            var parentChecks = checks.Where(check => check.AchievementId == parent.Id).ToArray();
            if (parentChecks.Length == 0)
                f.Actor.Player.GuildManager.Guild.AchievementManager.CheckAchievements(f.Actor.Player,
                    (AchievementType)parent.AchievementTypeId, parent.ObjectId, parent.ObjectIdAlt, Math.Max(1, parent.RequiredProgress));
            else foreach (var check in parentChecks)
                f.Actor.Player.GuildManager.Guild.AchievementManager.CheckAchievements(f.Actor.Player,
                    (AchievementType)parent.AchievementTypeId, check.ObjectId, check.ObjectIdAlt);
            Assert.True(f.Actor.Player.GuildManager.Guild.AchievementManager.HasCompletedAchievement((ushort)parent.Id));
            await CommitFeedbackParent(f); await f.Actor.Drain();
            if (parent == parents[0])
            {
                using var first = f.Source.Context(); var row = first.CharacterAchievement.Single(value => value.AchievementId == child.Id);
                partial = row.ProgressState; partialCompleted = row.DateCompleted != null;
            }
        }
        using var final = f.Source.Context(); var last = final.CharacterAchievement.Single(row => row.AchievementId == child.Id);
        return new(partial, partialCompleted, last.ProgressState, last.DateCompleted != null,
            final.GuildAchievement.Count(row => row.DateCompleted != null), final.CharacterAchievement.Count(),
            final.BankReconciliationReceipt.Count(row => row.Id.StartsWith("g")), final.BankReconciliationReceipt.Count(row => row.Id.StartsWith("h")),
            f.Packets<ServerAchievementUpdate>().Count(packet => packet.Achievements.Any(row => row.AchievementId == child.Id)));
    }
}
