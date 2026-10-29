using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database.Group;
using NexusForever.Database.Group.Model;
using NexusForever.Database.Group.Repository;
using NexusForever.Network.Internal.Message.Group;
using NexusForever.Server.GroupServer.Group;
using NexusForever.Server.GroupServer.Network.Internal;
using Quartz;

namespace NexusForever.Server.GroupServer.Job
{
    [DisallowConcurrentExecution]
    public class CharacterDirtyStatsScheduledJob : IJob
    {
        #region Dependency Injection

        private readonly IServiceScopeFactory _scopeFactory;

        public CharacterDirtyStatsScheduledJob(
            IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        #endregion

        public async Task Execute(IJobExecutionContext context)
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                // Fresh tracking per character lets a later iteration see external
                // dirty writes. The outbox and flag share this scope's commit.
                await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<CharacterRepository>();
                var groupContext = scope.ServiceProvider.GetRequiredService<GroupContext>();
                var messagePublisher = scope.ServiceProvider.GetRequiredService<OutboxMessagePublisher>();

                CharacterModel model = await repository.GetNextCharacterWithDirtyStats();
                if (model == null)
                    break;

                var character = scope.ServiceProvider.GetRequiredService<Character.Character>();
                character.Initialise(model);

                foreach (Character.CharacterGroup characterGroup in character.GetGroups())
                {
                    Group.Group group = await characterGroup.GetGroupAsync();
                    if (group == null)
                        continue;

                    GroupMember member = group.GetMember(character.Identity);
                    if (member == null)
                        continue;

                    await messagePublisher.PublishAsync(new GroupMemberStatsUpdatedMessage
                    {
                        Group  = await group.ToInternalGroup(),
                        Member = await member.ToInternalGroupMember(),
                    });
                }

                character.StatsDirty = false;

                await groupContext.SaveChangesAsync();
            }
        }
    }
}
