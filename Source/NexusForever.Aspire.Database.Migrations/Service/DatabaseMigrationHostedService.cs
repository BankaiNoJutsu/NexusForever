using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexusForever.Aspire.Database.Migrations.Configuration.Model;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Chat;
using NexusForever.Database.Friendship;
using NexusForever.Database.Group;
using NexusForever.Database.World;

namespace NexusForever.Aspire.Database.Migrations.Service
{
    public class DatabaseMigrationHostedService : IHostedService
    {
        #region Dependency Injection

        private readonly ILogger<DatabaseMigrationHostedService> _log;
        private readonly DatabaseMigrationOptions _options;
        private readonly AuthContext _authContext;
        private readonly CharacterContext _characterContext;
        private readonly WorldContext _worldContext;
        private readonly GroupContext _groupContext;
        private readonly ChatContext _chatContext;
        private readonly FriendshipContext _friendshipContext;

        public DatabaseMigrationHostedService(
            ILogger<DatabaseMigrationHostedService> log,
            IOptions<DatabaseMigrationOptions> options,
            AuthContext authContext,
            CharacterContext characterContext,
            WorldContext worldContext,
            GroupContext groupContext,
            ChatContext chatContext,
            FriendshipContext friendshipContext)
        {
            _log               = log;
            _options           = options.Value;
            _authContext       = authContext;
            _characterContext  = characterContext;
            _worldContext      = worldContext;
            _groupContext      = groupContext;
            _chatContext       = chatContext;
            _friendshipContext = friendshipContext;
        }

        #endregion

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_options.Skip)
            {
                _log.LogInformation("Database migrations are disabled for this execution. Skipping EF migration hosted service.");
                return;
            }

            await _authContext.Database.MigrateAsync();
            await _characterContext.Database.MigrateAsync();
            await _worldContext.Database.MigrateAsync();
            await _groupContext.Database.MigrateAsync();
            await _chatContext.Database.MigrateAsync();
            await _friendshipContext.Database.MigrateAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
