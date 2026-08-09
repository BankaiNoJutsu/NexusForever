using System.Net;
using Microsoft.Extensions.Configuration;
using NexusForever.Aspire.AppHost;
using NexusForever.Database;
using NexusForever.Network.Internal.Static;

internal class Program
{
    private const string AuthDatabase = "nexus_forever_auth";
    private const string CharacterDatabase = "nexus_forever_character";
    private const string WorldDatabase = "nexus_forever_world";
    private const string GroupDatabase = "nexus_forever_group";
    private const string ChatDatabase = "nexus_forever_chat";
    private const string FriendshipDatabase = "nexus_forever_friendship";
    private const string QueryDatabase = "nexus_forever_query";

    private static async Task Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        DatabaseProvider databaseProvider = GetDatabaseProvider(builder.Configuration);

        //builder.AddDockerComposeEnvironment("nexus-forever");

        var rmq = builder.AddRabbitMQ("rmq")
            .WithManagementPlugin();

        if (databaseProvider == DatabaseProvider.MySql)
            ConfigureMySqlHost(builder, rmq);
        else if (databaseProvider == DatabaseProvider.Sqlite)
            ConfigureSqliteHost(builder, rmq);
        else
            throw new NotSupportedException($"Database provider '{databaseProvider}' is not supported by the Aspire app host.");

        DistributedApplication host = builder.Build();
        await host.RunAsync();
    }

    private static void ConfigureMySqlHost(IDistributedApplicationBuilder builder, IResourceBuilder<RabbitMQServerResource> rmq)
    {
        var mysql = builder.AddMySql("mysql")
            .WithPhpMyAdmin()
            .WithDataVolume("mysql-data");

        var authdb       = mysql.AddDatabase("authdb");
        var characterdb  = mysql.AddDatabase("characterdb");
        var worlddb      = mysql.AddDatabase("worlddb");
        var groupdb      = mysql.AddDatabase("groupdb");
        var chatdb       = mysql.AddDatabase("chatdb");
        var friendshipdb = mysql.AddDatabase("friendshipdb");
        var querydb      = mysql.AddDatabase("querydb");

        IResourceBuilder<ProjectResource> dbMigration = builder.AddProject<Projects.NexusForever_Aspire_Database_Migrations>("database-migrations")
            .WithReference(authdb)
            .WithReference(characterdb)
            .WithReference(worlddb)
            .WithReference(groupdb)
            .WithReference(chatdb)
            .WithReference(friendshipdb)
            .WithReference(querydb)
            .WaitFor(authdb)
            .WaitFor(characterdb)
            .WaitFor(worlddb)
            .WaitFor(groupdb)
            .WaitFor(chatdb)
            .WaitFor(friendshipdb)
            .WaitFor(querydb);

        builder.AddProject<Projects.NexusForever_AuthServer>("auth-server")
            .WithNexusForeverTcp(IPAddress.Any, 23115)
            .WithNexusForeverDatabase("Auth", DatabaseProvider.MySql, authdb.Resource)
            .WaitFor(authdb)
            .WaitForCompletion(dbMigration);

        builder.AddProject<Projects.NexusForever_StsServer>("sts-server")
            .WithNexusForeverTcp(IPAddress.Any, 6600)
            .WithNexusForeverDatabase("Auth", DatabaseProvider.MySql, authdb.Resource)
            .WaitFor(authdb)
            .WaitForCompletion(dbMigration);

        IResourceBuilder<ProjectResource> worldServer = builder.AddProject<Projects.NexusForever_WorldServer>("world-server")
            .WithNexusForeverTcp(IPAddress.Any, 24000)
            .WithNexusForeverHttp(5000)
            .WithNexusForeverDatabase("Auth", DatabaseProvider.MySql, authdb.Resource)
            .WithNexusForeverDatabase("Character", DatabaseProvider.MySql, characterdb.Resource)
            .WithNexusForeverDatabase("World", DatabaseProvider.MySql, worlddb.Resource)
            .WithNexusForeverMessageBroker("WorldServer_1", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithEnvironment("Realm:RealmId", "1")
            .WaitFor(authdb)
            .WaitFor(characterdb)
            .WaitFor(worlddb)
            .WaitFor(rmq)
            .WaitForCompletion(dbMigration);

        ConfigureWorldConsoleUrl(worldServer);

        IResourceBuilder<ProjectResource> accountApi = builder.AddProject<Projects.NexusForever_API_Account>("account-api")
            .WithNexusForeverHttp(4001)
            .WithNexusForeverDatabase("Auth", DatabaseProvider.MySql, authdb.Resource)
            .WaitFor(authdb)
            .WaitForCompletion(dbMigration);

        IResourceBuilder<ProjectResource> characterApi = builder.AddProject<Projects.NexusForever_API_Character>("character-api")
            .WithNexusForeverHttp(4000)
            .WithNexusForeverDatabase("Auth", DatabaseProvider.MySql, authdb.Resource)
            .WithNexusForeverDatabase("Character:0", DatabaseProvider.MySql, characterdb.Resource)
            .WithEnvironment("Database:Character:0:RealmId", "1")
            .WaitFor(authdb)
            .WaitFor(characterdb)
            .WaitForCompletion(dbMigration);

        builder.AddProject<Projects.NexusForever_Server_GroupServer>("group-server")
            .WithNexusForeverDatabase("Group", DatabaseProvider.MySql, groupdb.Resource)
            .WithNexusForeverMessageBroker("GroupServer", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithNexusForeverApi("Character", characterApi.Resource)
            .WaitFor(rmq)
            .WaitFor(groupdb)
            .WaitForCompletion(dbMigration)
            .WaitFor(characterApi);

        builder.AddProject<Projects.NexusForever_Server_ChatServer>("chat-server")
            .WithNexusForeverDatabase("Chat", DatabaseProvider.MySql, chatdb.Resource)
            .WithNexusForeverMessageBroker("ChatServer", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithNexusForeverApi("Character", characterApi.Resource)
            .WaitFor(rmq)
            .WaitFor(chatdb)
            .WaitForCompletion(dbMigration)
            .WaitFor(characterApi);

        builder.AddProject<Projects.NexusForever_Server_Friendship>("friendship-server")
            .WithNexusForeverDatabase("Friendship", DatabaseProvider.MySql, friendshipdb.Resource)
            .WithNexusForeverMessageBroker("FriendshipServer", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithNexusForeverApi("Account", accountApi.Resource)
            .WithNexusForeverApi("Character", characterApi.Resource)
            .WaitFor(rmq)
            .WaitFor(friendshipdb)
            .WaitForCompletion(dbMigration)
            .WaitFor(accountApi)
            .WaitFor(characterApi);

        builder.AddProject<Projects.NexusForever_Server_Character>("character-server")
            .WithNexusForeverDatabase("Query", DatabaseProvider.MySql, querydb.Resource)
            .WithNexusForeverMessageBroker("CharacterServer", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithNexusForeverApi("Character", characterApi.Resource)
            .WaitFor(rmq)
            .WaitFor(querydb)
            .WaitFor(characterApi);
    }

    private static void ConfigureSqliteHost(IDistributedApplicationBuilder builder, IResourceBuilder<RabbitMQServerResource> rmq)
    {
        string sqliteDirectory = GetSqliteDirectory(builder.Configuration);
        Directory.CreateDirectory(sqliteDirectory);

        string authdb       = GetSqliteConnectionString(sqliteDirectory, AuthDatabase);
        string characterdb  = GetSqliteConnectionString(sqliteDirectory, CharacterDatabase);
        string worlddb      = GetSqliteConnectionString(sqliteDirectory, WorldDatabase);
        string groupdb      = GetSqliteConnectionString(sqliteDirectory, GroupDatabase);
        string chatdb       = GetSqliteConnectionString(sqliteDirectory, ChatDatabase);
        string friendshipdb = GetSqliteConnectionString(sqliteDirectory, FriendshipDatabase);
        string querydb      = GetSqliteConnectionString(sqliteDirectory, QueryDatabase);

        IResourceBuilder<ProjectResource> dbMigration = builder.AddProject<Projects.NexusForever_Aspire_Database_Migrations>("database-migrations")
            .WithNexusForeverDatabase("Auth", DatabaseProvider.Sqlite, authdb)
            .WithNexusForeverDatabase("Character", DatabaseProvider.Sqlite, characterdb)
            .WithNexusForeverDatabase("World", DatabaseProvider.Sqlite, worlddb)
            .WithNexusForeverDatabase("Group", DatabaseProvider.Sqlite, groupdb)
            .WithNexusForeverDatabase("Chat", DatabaseProvider.Sqlite, chatdb)
            .WithNexusForeverDatabase("Friendship", DatabaseProvider.Sqlite, friendshipdb)
            .WithNexusForeverDatabase("Query", DatabaseProvider.Sqlite, querydb);

        builder.AddProject<Projects.NexusForever_AuthServer>("auth-server")
            .WithNexusForeverTcp(IPAddress.Any, 23115)
            .WithNexusForeverDatabase("Auth", DatabaseProvider.Sqlite, authdb)
            .WaitForCompletion(dbMigration);

        builder.AddProject<Projects.NexusForever_StsServer>("sts-server")
            .WithNexusForeverTcp(IPAddress.Any, 6600)
            .WithNexusForeverDatabase("Auth", DatabaseProvider.Sqlite, authdb)
            .WaitForCompletion(dbMigration);

        IResourceBuilder<ProjectResource> worldServer = builder.AddProject<Projects.NexusForever_WorldServer>("world-server")
            .WithNexusForeverTcp(IPAddress.Any, 24000)
            .WithNexusForeverHttp(5000)
            .WithNexusForeverDatabase("Auth", DatabaseProvider.Sqlite, authdb)
            .WithNexusForeverDatabase("Character", DatabaseProvider.Sqlite, characterdb)
            .WithNexusForeverDatabase("World", DatabaseProvider.Sqlite, worlddb)
            .WithNexusForeverMessageBroker("WorldServer_1", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithEnvironment("Realm:RealmId", "1")
            .WaitFor(rmq)
            .WaitForCompletion(dbMigration);

        ConfigureWorldConsoleUrl(worldServer);

        IResourceBuilder<ProjectResource> accountApi = builder.AddProject<Projects.NexusForever_API_Account>("account-api")
            .WithNexusForeverHttp(4001)
            .WithNexusForeverDatabase("Auth", DatabaseProvider.Sqlite, authdb)
            .WaitForCompletion(dbMigration);

        IResourceBuilder<ProjectResource> characterApi = builder.AddProject<Projects.NexusForever_API_Character>("character-api")
            .WithNexusForeverHttp(4000)
            .WithNexusForeverDatabase("Auth", DatabaseProvider.Sqlite, authdb)
            .WithNexusForeverDatabase("Character:0", DatabaseProvider.Sqlite, characterdb)
            .WithEnvironment("Database:Character:0:RealmId", "1")
            .WaitForCompletion(dbMigration);

        builder.AddProject<Projects.NexusForever_Server_GroupServer>("group-server")
            .WithNexusForeverDatabase("Group", DatabaseProvider.Sqlite, groupdb)
            .WithNexusForeverMessageBroker("GroupServer", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithNexusForeverApi("Character", characterApi.Resource)
            .WaitFor(rmq)
            .WaitForCompletion(dbMigration)
            .WaitFor(characterApi);

        builder.AddProject<Projects.NexusForever_Server_ChatServer>("chat-server")
            .WithNexusForeverDatabase("Chat", DatabaseProvider.Sqlite, chatdb)
            .WithNexusForeverMessageBroker("ChatServer", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithNexusForeverApi("Character", characterApi.Resource)
            .WaitFor(rmq)
            .WaitForCompletion(dbMigration)
            .WaitFor(characterApi);

        builder.AddProject<Projects.NexusForever_Server_Friendship>("friendship-server")
            .WithNexusForeverDatabase("Friendship", DatabaseProvider.Sqlite, friendshipdb)
            .WithNexusForeverMessageBroker("FriendshipServer", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithNexusForeverApi("Account", accountApi.Resource)
            .WithNexusForeverApi("Character", characterApi.Resource)
            .WaitFor(rmq)
            .WaitForCompletion(dbMigration)
            .WaitFor(accountApi)
            .WaitFor(characterApi);

        builder.AddProject<Projects.NexusForever_Server_Character>("character-server")
            .WithNexusForeverDatabase("Query", DatabaseProvider.Sqlite, querydb)
            .WithNexusForeverMessageBroker("CharacterServer", BrokerProvider.RabbitMQ, rmq.Resource)
            .WithNexusForeverApi("Character", characterApi.Resource)
            .WaitFor(rmq)
            .WaitFor(characterApi);
    }

    private static void ConfigureWorldConsoleUrl(IResourceBuilder<ProjectResource> worldServer)
    {
        worldServer.WithEnvironment(c =>
        {
            if (c.Resource.TryGetUrls(out var urls))
            {
                foreach (ResourceUrlAnnotation url in urls)
                {
                    if (url.Endpoint?.Scheme != "http")
                        continue;

                    url.DisplayText = "Web Console";
                    url.Url = new UriBuilder(url.Url) { Path = "console.html" }.ToString();
                }
            }
        });
    }

    private static DatabaseProvider GetDatabaseProvider(IConfiguration configuration)
    {
        string? configuredProvider = configuration["AppHost:DatabaseProvider"];
        if (string.IsNullOrWhiteSpace(configuredProvider))
            return DatabaseProvider.MySql;

        if (Enum.TryParse(configuredProvider, ignoreCase: true, out DatabaseProvider provider))
            return provider;

        throw new InvalidOperationException($"Invalid AppHost:DatabaseProvider value '{configuredProvider}'.");
    }

    private static string GetSqliteDirectory(IConfiguration configuration)
    {
        string? configuredDirectory = configuration["AppHost:SqliteDirectory"];
        string sqliteDirectory = string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine(Directory.GetCurrentDirectory(), ".nexusforever-runtime", "sqlite")
            : configuredDirectory;

        return Path.GetFullPath(sqliteDirectory);
    }

    private static string GetSqliteConnectionString(string sqliteDirectory, string database)
    {
        return $"Data Source={Path.Combine(sqliteDirectory, $"{database}.sqlite")}";
    }
}
