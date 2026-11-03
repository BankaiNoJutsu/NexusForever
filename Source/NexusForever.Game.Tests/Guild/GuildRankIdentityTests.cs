using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;

namespace NexusForever.Game.Tests.Guild;

public sealed class GuildRankIdentityTests
{
    private readonly DbContextOptions<CharacterContext> providerOptions;
    public GuildRankIdentityTests() { }
    internal GuildRankIdentityTests(DbContextOptions<CharacterContext> options)=>providerOptions=options;
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CachedProviderModelsKeepApplicationAssignedRankKeys(bool mysql)
    {
        var options=new DbContextOptionsBuilder<CharacterContext>();
        if(mysql)options.UseMySql("Server=127.0.0.1;Database=nf_rank_model;User Id=unused;Pooling=false",new MySqlServerVersion(new Version(8,4,13)));
        else options.UseSqlite("Data Source=:memory:");
        object cached=null;
        for(int iteration=0;iteration<512;iteration++)
        {
            using var context=new CharacterContext(options.Options);
            cached??=context.Model;Assert.Same(cached,context.Model);
            context.Guild.Add(Graph(out var leader));
            Assert.Equal(0,context.Entry(leader).Property(row=>row.Index).CurrentValue);
            Assert.False(context.Entry(leader).Property(row=>row.Index).IsTemporary);
            Assert.Equal(new byte[]{0,2,9},context.ChangeTracker.Entries<GuildRankModel>().Select(row=>row.Property(rank=>rank.Index).CurrentValue).Order());
        }
    }

    [Fact]
    public async Task ApplicationAssignedZeroRankGraphRoundTripsWithoutTemporaryKeys()
    {
        await using var connection=providerOptions==null?new SqliteConnection("Data Source=:memory:"):null;
        if(connection!=null)await connection.OpenAsync();
        var options=providerOptions??new DbContextOptionsBuilder<CharacterContext>().UseSqlite(connection).Options;
        await using var context=new CharacterContext(options);await context.Database.EnsureCreatedAsync();
        context.Character.Add(new CharacterModel{Id=42,Name="Leader"});await context.SaveChangesAsync();
        var guild=Graph(out var leader);guild.LeaderId=42;guild.GuildMember.Add(new GuildMemberModel{Id=77,CharacterId=42,Rank=0});
        context.Guild.Add(guild);Assert.False(context.Entry(leader).Property(row=>row.Index).IsTemporary);
        await context.SaveChangesAsync();context.ChangeTracker.Clear();
        Assert.Equal(new byte[]{0,2,9},await context.GuildRank.OrderBy(row=>row.Index).Select(row=>row.Index).ToArrayAsync());
        Assert.Equal((byte)0,(await context.GuildMember.SingleAsync()).Rank);
        Assert.Equal(42ul,(await context.Guild.SingleAsync()).LeaderId);
    }

    private static GuildModel Graph(out GuildRankModel leader)
    {
        leader=new GuildRankModel{Id=77,Index=0,Name="Leader"};
        return new GuildModel{Id=77,Name="Guild",GuildRank=[leader,new GuildRankModel{Id=77,Index=2,Name="Unused"},new GuildRankModel{Id=77,Index=9,Name="Member"}]};
    }
}
