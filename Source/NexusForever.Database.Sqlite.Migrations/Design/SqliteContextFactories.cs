using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NexusForever.Database.Auth;
using NexusForever.Database.Character;
using NexusForever.Database.Chat;
using NexusForever.Database.Friendship;
using NexusForever.Database.Group;
using NexusForever.Database.World;

namespace NexusForever.Database.Sqlite.Migrations.Design
{
    internal static class SqliteDesignTime
    {
        public static DbContextOptions<TContext> CreateOptions<TContext>(string databaseName) where TContext : DbContext
        {
            string path = Path.Combine(Path.GetTempPath(), $"nexusforever-{databaseName}-design.sqlite");

            var builder = new DbContextOptionsBuilder<TContext>();
            builder.UseSqlite($"Data Source={path}", b =>
            {
                b.MigrationsAssembly(NexusForever.Database.Extensions.SqliteMigrationsAssembly);
            });
            return builder.Options;
        }
    }

    public sealed class AuthContextFactory : IDesignTimeDbContextFactory<AuthContext>
    {
        public AuthContext CreateDbContext(string[] args)
        {
            return new AuthContext(SqliteDesignTime.CreateOptions<AuthContext>("auth"));
        }
    }

    public sealed class CharacterContextFactory : IDesignTimeDbContextFactory<CharacterContext>
    {
        public CharacterContext CreateDbContext(string[] args)
        {
            return new CharacterContext(SqliteDesignTime.CreateOptions<CharacterContext>("character"));
        }
    }

    public sealed class WorldContextFactory : IDesignTimeDbContextFactory<WorldContext>
    {
        public WorldContext CreateDbContext(string[] args)
        {
            return new WorldContext(SqliteDesignTime.CreateOptions<WorldContext>("world"));
        }
    }

    public sealed class GroupContextFactory : IDesignTimeDbContextFactory<GroupContext>
    {
        public GroupContext CreateDbContext(string[] args)
        {
            return new GroupContext(SqliteDesignTime.CreateOptions<GroupContext>("group"));
        }
    }

    public sealed class ChatContextFactory : IDesignTimeDbContextFactory<ChatContext>
    {
        public ChatContext CreateDbContext(string[] args)
        {
            return new ChatContext(SqliteDesignTime.CreateOptions<ChatContext>("chat"));
        }
    }

    public sealed class FriendshipContextFactory : IDesignTimeDbContextFactory<FriendshipContext>
    {
        public FriendshipContext CreateDbContext(string[] args)
        {
            return new FriendshipContext(SqliteDesignTime.CreateOptions<FriendshipContext>("friendship"));
        }
    }
}
