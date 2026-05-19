using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Shared;

namespace NexusForever.Game.Tests.Achievement;

[Collection(LegacyServiceProviderCollection.Name)]
public class GuildAchievementManagerTests
{
    [Fact]
    public void CompleteAchievement_NotifiesCharacterAchievementProgress()
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var achievementManagerProxy);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);

        IGuild guild = RecordingDispatchProxy<IGuild>.Create(out _);
        var manager = new GuildAchievementManager(guild);
        var achievement = new Achievement<GuildAchievementModel>(1ul, new TestAchievementInfo(new AchievementEntry
        {
            Id = 73,
            AchievementTypeId = (uint)AchievementType.KillCreatureEntry
        }));

        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = new ServiceCollection()
            .AddSingleton(new GlobalAchievementManager())
            .BuildServiceProvider();

        try
        {
            GetCompleteAchievementMethod().Invoke(manager, [player, achievement]);

            RecordingDispatchProxy<ICharacterAchievementManager>.Invocation call = Assert.Single(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
            Assert.Same(player, call.Arguments[0]);
            Assert.Equal(AchievementType.AchievementComplete, call.Arguments[1]);
            Assert.Equal(73u, call.Arguments[2]);
            Assert.Equal(0u, call.Arguments[3]);
            Assert.Equal(1u, call.Arguments[4]);
            Assert.NotNull(achievement.DateCompleted);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static MethodInfo GetCompleteAchievementMethod()
    {
        return typeof(GuildAchievementManager)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => m.Name == "CompleteAchievement"
                && m.GetParameters().Length == 2
                && m.GetParameters()[0].ParameterType == typeof(IPlayer)
                && m.GetParameters()[1].ParameterType == typeof(IAchievement));
    }

    private sealed class TestAchievementInfo : IAchievementInfo
    {
        public TestAchievementInfo(AchievementEntry entry)
        {
            Entry = entry;
            ChecklistEntries = [];
        }

        public ushort Id => (ushort)Entry.Id;
        public AchievementEntry Entry { get; }
        public List<AchievementChecklistEntry> ChecklistEntries { get; }
        public bool IsPlayerAchievement => false;
        public bool IsRealmFirst => false;
    }
}
