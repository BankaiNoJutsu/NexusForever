using System.Reflection;
using System.Data.Common;
using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Housing;
using NexusForever.Game.Map;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Static.Guild;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network;
using NexusForever.Network.World.Message.Model;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Housing;
using static NexusForever.Game.Tests.Housing.HousingRenameFeeRecoveryTests;

namespace NexusForever.Game.Tests.Housing;

public sealed class HousingRemodelPublicationTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public HousingRemodelPublicationTests() { }
    internal HousingRemodelPublicationTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Fact]
    public async Task LivePropertiesAndBroadcastWaitForSqlConfirmation()
    {
        using var f = await Create(); Handle(f, Request(f));
        Assert.Equal((ushort)0, f.Root.Music); Assert.Empty(Packets(f));
        await Flush(f.Owner); Assert.Equal((ushort)29, f.Root.Music);
        Assert.Equal((ushort)29, Assert.Single(Assert.Single(Packets(f)).Residences).Music);
    }

    [Fact]
    public async Task SqlCompletionAloneCannotApplyOrPublishTheRemodel()
    {
        using var f = await Create(); Handle(f, Request(f)); await SaveOnce(f);
        using(var sql=f.Context()) Assert.Equal((ushort)29, sql.Residence.Single(row=>row.Id==f.Root.Id).MusicId);
        Assert.Equal((ushort)0, f.Root.Music); Assert.Empty(Packets(f));
        f.Owner.Residences.Update(0); Assert.Equal((ushort)29, f.Root.Music); Assert.Single(Packets(f));
    }

    [Fact]
    public async Task RejectedSqlRetainsPriorPropertiesAndNoBroadcastUntilRecovery()
    {
        using var f = await Create();
        using(var setup=f.Context()) await Reject(setup);
        Handle(f,Request(f)); await SaveOnce(f);
        using(var failed=f.Context())
        {
            Assert.Equal((ushort)0, failed.Residence.Single(row=>row.Id==f.Root.Id).MusicId);
            Assert.Equal((ushort)0,f.Root.Music); Assert.Empty(Packets(f));
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_remodel");
        }
        await Flush(f.Owner);
        using var sql=f.Context(); Assert.Equal((ushort)29,sql.Residence.Single(row=>row.Id==f.Root.Id).MusicId);
        Assert.Single(Packets(f));
    }

    [Fact]
    public async Task InvalidLaterFieldCannotApplyAnEarlierValidField()
    {
        using var f=await Create(); var request=Request(f,999); Set(request,nameof(ClientHousingRemodel.RoofDecorInfoId),201u);
        Assert.Throws<ArgumentOutOfRangeException>(()=>Handle(f,request));
        Assert.Equal((ushort)0,f.Root.Roof); Assert.Equal((ushort)0,f.Root.Music); Assert.Empty(Packets(f));
    }

    [Fact]
    public async Task PublicPropertiesCannotPrecedeTheirCommittedSql()
    {
        using var f=await Create(); bool premature=false; int packets=0;
        f.Owner.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted),args=>
        {
            if(args[0] is ServerHousingProperties properties)
            {
                packets++; using var sql=f.Context();
                premature|=sql.Residence.Single(row=>row.Id==f.Root.Id).MusicId!=properties.Residences.Single().Music;
            }
            return null;
        });
        Handle(f,Request(f)); await Flush(f.Owner); Assert.False(premature); Assert.Equal(1,packets);
    }

    [Fact]
    public async Task SuccessfulRemodelKeepsAllSevenFieldsAndTheCompleteGraph()
    {
        using var f=await Create(); f.Root.Flags=(ResidenceFlags)16; Assert.True(f.Root.AddNeighbor(43));
        var request=Request(f); Set(request,nameof(ClientHousingRemodel.RoofDecorInfoId),201u);
        Set(request,nameof(ClientHousingRemodel.EntrywayDecorInfoId),202u); Set(request,nameof(ClientHousingRemodel.DoorDecorInfoId),203u);
        Set(request,nameof(ClientHousingRemodel.WallpaperId),33u); Set(request,nameof(ClientHousingRemodel.SkyWallpaperId),32u);
        Set(request,nameof(ClientHousingRemodel.GroundWallpaperId),31u); Handle(f,request); await Flush(f.Owner);
        using var sql=f.Context(); var row=Assert.Single(sql.Residence);
        Assert.Equal((ushort)201,row.RoofDecorInfoId); Assert.Equal((ushort)202,row.EntrywayDecorInfoId); Assert.Equal((ushort)203,row.DoorDecorInfoId);
        Assert.Equal((ushort)33,row.WallpaperId); Assert.Equal((ushort)32,row.SkyWallpaperId); Assert.Equal((ushort)31,row.GroundWallpaperId); Assert.Equal((ushort)29,row.MusicId);
        Assert.Equal((ushort)16,row.Flags); Assert.Single(sql.ResidencePlot); Assert.Single(sql.ResidenceNeighbor);
        Assert.Equal((ushort)29,f.Root.Music); Assert.Single(Packets(f));
    }

    [Fact]
    public async Task NoOpRequestStillWaitsForItsConfirmationWitness()
    {
        using var f=await Create(); Handle(f,Request(f,0)); Assert.Empty(Packets(f));
        await Flush(f.Owner); Assert.Equal((ushort)0,f.Root.Music); Assert.Single(Packets(f));
    }

    [Fact]
    public async Task ExistingCommunityPermissionsStillRejectAnUnauthorizedRemodel()
    {
        using var f=await Create(); f.Owner.Community.GetMember(42).Rank.Permissions=GuildRankPermission.None;
        Assert.Throws<InvalidPacketValueException>(()=>Handle(f,Request(f))); Assert.Equal((ushort)0,f.Root.Music); Assert.Empty(Packets(f));
    }

    [Fact]
    public async Task RequestsOnOneResidenceCommitAndPublishInAcceptedOrder()
    {
        using var f=await Create(); Handle(f,Request(f)); Handle(f,Request(f,30)); await Flush(f.Owner);
        Assert.Equal(new uint[]{29,30},Packets(f).Select(packet=>packet.Residences.Single().Music));
        using var sql=f.Context(); Assert.Equal((ushort)30,sql.Residence.Single().MusicId); Assert.Equal((ushort)30,f.Root.Music);
    }

    [Fact]
    public async Task AChangedRequestObjectCannotChangeItsAcceptedProposal()
    {
        using var f=await Create(); var request=Request(f); Handle(f,request); Set(request,nameof(ClientHousingRemodel.MusicId),30u);
        await Flush(f.Owner); using var sql=f.Context(); Assert.Equal((ushort)29,sql.Residence.Single().MusicId);
        Assert.Equal((ushort)29,Assert.Single(Packets(f)).Residences.Single().Music);
    }

    [Fact]
    public async Task ReentrantUpdateCannotReplayTheHeadOrReleaseItsNextRequest()
    {
        using var f=await Create(); bool reentered=false;
        f.Owner.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted),args=>
        {
            if(args[0] is ServerHousingProperties && !reentered){reentered=true;f.Owner.Residences.Update(0);} return null;
        });
        Handle(f,Request(f)); Handle(f,Request(f,30)); await Flush(f.Owner); Assert.True(reentered);
        Assert.Equal(new uint[]{29,30},Packets(f).Select(packet=>packet.Residences.Single().Music));
    }

    [Fact]
    public async Task FailedRecipientRetriesFreshPropertiesWithoutRepeatingSqlOrAnEarlierRecipient()
    {
        using var f=await Create(); using(var setup=f.Context())await AuditWrites(setup);
        var observer=AddObserver(f,400); bool failed=false; var attempts=new List<ServerHousingProperties>();
        observer.Connection.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted),args=>
        {
            if(args[0] is ServerHousingProperties packet)
            {
                attempts.Add(packet);
                if(!failed){failed=true;packet.Residences.Single().Music=999;throw new IOException("owned remodel enqueue failure");}
            }
            return null;
        });
        Handle(f,Request(f)); await SaveOnce(f); f.Owner.Residences.Update(0);
        Assert.True(failed); Assert.Single(Packets(f)); Assert.Throws<InvalidOperationException>(()=>f.Root.Music=30);
        using(var sql=f.Context())Assert.Equal(1,await Writes(sql));
        await Flush(f.Owner); Assert.Single(Packets(f)); Assert.Equal(2,attempts.Count);
        Assert.NotSame(attempts[0],attempts[1]); Assert.Equal((ushort)29,attempts[1].Residences.Single().Music);
        using var final=f.Context(); Assert.Equal(1,await Writes(final));
    }

    [Fact]
    public async Task AReplacementRecipientSessionCannotReceiveARetainedRetry()
    {
        using var f=await Create(); var observer=AddObserver(f,401);
        observer.Connection.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted),args=>
        {if(args[0] is ServerHousingProperties)throw new IOException("owned recipient pause");return null;});
        Handle(f,Request(f)); await SaveOnce(f); f.Owner.Residences.Update(0);
        var replacement=RecordingDispatchProxy<IWorldSession>.Create(out var connection);
        connection.SetProperty(nameof(IWorldSession.Player),observer.Player); observer.State.SetProperty(nameof(IPlayer.Session),replacement);
        await Flush(f.Owner);
        Assert.Single(observer.Connection.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)));
        Assert.Empty(connection.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))); Assert.Single(Packets(f));
    }

    [Fact]
    public async Task LaterUnrelatedFlagsRemainDirtyAndSurviveTheCommittedApply()
    {
        using var f=await Create(); Handle(f,Request(f)); bool dirtyAtPublication=false;
        f.Owner.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted),args=>
        {if(args[0] is ServerHousingProperties)dirtyAtPublication=f.Root.NeedsSave;return null;});
        using(var context=f.Context())
        {
            StageAccepted(f,context); f.Root.Flags=(ResidenceFlags)16;
            await context.SaveChangesAsync();
        }
        using(var sql=f.Context())Assert.Equal((ushort)0,sql.Residence.Single().Flags);
        using(var escape=f.Context())Assert.Throws<InvalidOperationException>(()=>f.Root.Save(escape));
        await Flush(f.Owner); Assert.True(dirtyAtPublication); Assert.Equal((ResidenceFlags)16,f.Root.Flags);
        using var final=f.Context(); Assert.Equal((ushort)29,final.Residence.Single().MusicId); Assert.Equal((ushort)16,final.Residence.Single().Flags);
    }

    [Fact]
    public async Task CallerTransactionRollbackCannotApplyOrPublishItsProposal()
    {
        using var f=await Create(); Handle(f,Request(f));
        using(var context=f.Context())
        {
            using var transaction=await context.Database.BeginTransactionAsync(); StageAccepted(f,context);
            await context.SaveChangesAsync(); Assert.Equal((ushort)29,context.Residence.AsNoTracking().Single().MusicId);
            Assert.Equal((ushort)0,f.Root.Music); Assert.Empty(Packets(f)); await transaction.RollbackAsync();
        }
        using(var sql=f.Context())Assert.Equal((ushort)0,sql.Residence.Single().MusicId);
        await Flush(f.Owner); Assert.Equal((ushort)29,f.Root.Music); Assert.Single(Packets(f));
    }

    [Fact]
    public async Task LostCommitAcknowledgementRecoversTheOriginalProposalWithoutEarlyPublication()
    {
        using var f=await Create(); var lost=new LostAcknowledgement(); Handle(f,Request(f));
        using(var context=f.Owner.Source.Context(lost))
        {
            using var transaction=await context.Database.BeginTransactionAsync(); StageAccepted(f,context); await context.SaveChangesAsync();
            await Assert.ThrowsAsync<IOException>(()=>transaction.CommitAsync());
        }
        Assert.True(lost.Observed); using(var sql=f.Context())Assert.Equal((ushort)29,sql.Residence.Single().MusicId);
        Assert.Equal((ushort)0,f.Root.Music); Assert.Empty(Packets(f));
        await Flush(f.Owner); Assert.Equal((ushort)29,f.Root.Music); Assert.Single(Packets(f));
    }

    [Fact]
    public async Task ForeignDatabaseCannotSaveTheRetainedProposal()
    {
        using var f=await Create(); DbContextOptions<CharacterContext> foreignOptions=null;
        using(var context=f.Context())
            if(context.Database.IsMySql())
            {
                string configured=context.Database.GetDbConnection().ConnectionString;
                var connection=new MySqlConnectionStringBuilder(configured);
                if(connection.Server!="127.0.0.1" || !connection.Database.StartsWith("nf_remodel_",StringComparison.Ordinal))
                    throw new InvalidOperationException("Foreign provider fixture requires its admitted owned schema.");
                connection.Database+="_foreign";
                foreignOptions=new DbContextOptionsBuilder<CharacterContext>().UseMySql(connection.ConnectionString,ServerVersion.AutoDetect(configured)).Options;
            }
        try
        {
            using var other=await new HousingRemodelPublicationTests(foreignOptions).Create(); Handle(f,Request(f));
            using(var foreign=other.Context())Assert.Throws<InvalidOperationException>(()=>StageAccepted(f,foreign));
            using(var sql=other.Context())Assert.Equal((ushort)0,sql.Residence.Single().MusicId);
            Assert.Equal((ushort)0,f.Root.Music); Assert.Empty(Packets(f));
            await Flush(f.Owner); Assert.Equal((ushort)29,f.Root.Music); Assert.Single(Packets(f));
        }
        finally
        {
            if(foreignOptions!=null){await using var cleanup=new CharacterContext(foreignOptions);await cleanup.Database.EnsureDeletedAsync();}
        }
    }

    [Fact]
    public async Task ForeignSqlCannotBeAdoptedAsTheFailedProposalsOutcome()
    {
        using var f=await Create(); using(var setup=f.Context())await Reject(setup);
        Handle(f,Request(f)); await SaveOnce(f);
        using(var foreign=f.Context())
        {
            await foreign.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_remodel");
            await foreign.Database.ExecuteSqlRawAsync("UPDATE residence SET musicId=30 WHERE id=100");
        }
        await Flush(f.Owner); Assert.Equal((ushort)0,f.Root.Music); Assert.Empty(Packets(f));
        using var sql=f.Context(); Assert.Equal((ushort)30,sql.Residence.Single().MusicId);
        using var escape=f.Context(); Assert.Throws<InvalidOperationException>(()=>f.Root.Save(escape));
    }

    [Fact]
    public async Task AReplacedInitiatorSessionBeforePreparationCancelsWithoutSaving()
    {
        using var f=await Create(); Handle(f,Request(f)); var replacement=RecordingDispatchProxy<IWorldSession>.Create(out var session);
        session.SetProperty(nameof(IWorldSession.Player),f.Owner.Actor.Player);
        f.Owner.Actor.Set(nameof(IPlayer.Session),replacement); Assert.Same(replacement,f.Owner.Actor.Player.Session);
        await Flush(f.Owner); Assert.Equal((ushort)0,f.Root.Music); Assert.Empty(Packets(f));
        using var sql=f.Context(); Assert.Equal((ushort)0,sql.Residence.Single().MusicId);
    }

    [Fact]
    public async Task AReplacedMapAfterCommitCannotReceiveTheOldMapsPublication()
    {
        using var f=await Create(); Handle(f,Request(f)); await SaveOnce(f);
        f.Root.Map=RecordingDispatchProxy<IResidenceMapInstance>.Create(out var replacement);
        await Flush(f.Owner); Assert.Equal((ushort)29,f.Root.Music); Assert.Empty(Packets(f));
        Assert.Empty(replacement.GetInvocations(nameof(IResidenceMapInstance.EnqueueToAll)));
        using var sql=f.Context(); Assert.Equal((ushort)29,sql.Residence.Single().MusicId);
    }

    [Fact]
    public async Task AnEarlierResidenceTransactionSettlesBeforeTheRemodelCapturesItsGraph()
    {
        using var f=await Create();
        using(var context=f.Context())
        {
            using var transaction=await context.Database.BeginTransactionAsync();
            f.Root.Flags=(ResidenceFlags)16; f.Root.Save(context); await context.SaveChangesAsync();
            Handle(f,Request(f)); await SaveOnce(f); Assert.Equal((ushort)0,f.Root.Music); Assert.Empty(Packets(f));
            await transaction.CommitAsync();
        }
        await Flush(f.Owner); using var sql=f.Context(); Assert.Equal((ushort)29,sql.Residence.Single().MusicId);
        Assert.Equal((ushort)16,sql.Residence.Single().Flags); Assert.Single(Packets(f));
    }

    [Fact]
    public async Task AReplacementRootCannotReceiveTheOriginalRootsCommittedApply()
    {
        using var f=await Create(); var original=f.Root; Handle(f,Request(f)); await SaveOnce(f);
        var replacement=new Residence(new NexusForever.Database.Character.Model.ResidenceModel{Id=original.Id,GuildOwnerId=77,Name="Replacement",PropertyInfoId=(byte)PropertyInfoId.Community},f.Owner.Residences,1,f.Owner.Source.Source.Tables);
        var registry=(IDictionary<ulong,NexusForever.Game.Abstract.Housing.IResidence>)typeof(GlobalResidenceManager).GetField("residences",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(f.Owner.Residences)!;
        registry[original.Id]=replacement; f.Owner.Community.Residence=replacement; await Flush(f.Owner);
        Assert.Equal((ushort)29,original.Music); Assert.Equal((ushort)0,replacement.Music); Assert.Empty(Packets(f));
        using var sql=f.Context(); Assert.Equal((ushort)29,sql.Residence.Single().MusicId);
    }

    private static void StageAccepted(Frame f,CharacterContext context)
    {
        var command=typeof(Residence).GetProperty("CurrentRemodel",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(f.Root)!;
        Assert.True((bool)Invoke(command,"Prepare")!); Invoke(command,"Stage",context);
    }
    private static object Invoke(object owner,string method,params object[] arguments)
    {
        try{return owner.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)!.Invoke(owner,arguments);}
        catch(TargetInvocationException error) when(error.InnerException!=null){ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw;}
    }
    private static (IPlayer Player,RecordingDispatchProxy<IPlayer> State,RecordingDispatchProxy<IWorldSession> Connection) AddObserver(Frame f,uint guid)
    {
        var player=RecordingDispatchProxy<IPlayer>.Create(out var state); var session=RecordingDispatchProxy<IWorldSession>.Create(out var connection);
        state.SetProperty(nameof(IPlayer.Guid),guid); state.SetProperty(nameof(IPlayer.Map),f.Map); state.SetProperty(nameof(IPlayer.Session),session);
        connection.SetProperty(nameof(IWorldSession.Player),player);
        ((IDictionary<uint,IGridEntity>)typeof(BaseMap).GetField("entities",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(f.Map)!)[guid]=player;
        return(player,state,connection);
    }
    private static async Task AuditWrites(CharacterContext context)
    {
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE remodel_writes (writes INTEGER NOT NULL)");
        await context.Database.ExecuteSqlRawAsync("INSERT INTO remodel_writes VALUES (0)");
        await context.Database.ExecuteSqlRawAsync(context.Database.IsSqlite()
            ? "CREATE TRIGGER count_remodel AFTER UPDATE ON residence BEGIN UPDATE remodel_writes SET writes=writes+1; END"
            : "CREATE TRIGGER count_remodel AFTER UPDATE ON residence FOR EACH ROW UPDATE remodel_writes SET writes=writes+1");
    }
    private static async Task<int> Writes(CharacterContext context)
    {
        await context.Database.OpenConnectionAsync(); using var command=context.Database.GetDbConnection().CreateCommand();
        command.CommandText="SELECT writes FROM remodel_writes"; return Convert.ToInt32(await command.ExecuteScalarAsync());
    }
    private sealed class LostAcknowledgement:DbTransactionInterceptor
    {
        internal bool Observed;
        public override Task TransactionCommittedAsync(DbTransaction transaction,TransactionEndEventData eventData,CancellationToken cancellationToken=default)
        {Observed=true;return Task.FromException(new IOException("owned remodel acknowledgement loss"));}
    }

    private async Task<Frame> Create(IInterceptor interceptor=null)
    {
        var owner=await new HousingRenameFeeRecoveryTests(options).Create(interceptor);
        var table=(RecordingDispatchProxy<IGameTableManager>)(object)owner.Source.Source.Tables;
        table.SetProperty(nameof(IGameTableManager.HousingWallpaperInfo),PlayerBindPointTests.Table(
            new HousingWallpaperInfoEntry{Id=29,Flags=0x100},new HousingWallpaperInfoEntry{Id=30,Flags=0x100},
            new HousingWallpaperInfoEntry{Id=31,Flags=0x200},new HousingWallpaperInfoEntry{Id=32,Flags=0x40},new HousingWallpaperInfoEntry{Id=33}));
        table.SetProperty(nameof(IGameTableManager.HousingDecorInfo),PlayerBindPointTests.Table(
            new HousingDecorInfoEntry{Id=201},new HousingDecorInfoEntry{Id=202},new HousingDecorInfoEntry{Id=203}));
        var map=new ResidenceMapInstance(null,null,null,owner.Residences,owner.Source.Source.Tables,null,null);
        var oldMap=owner.Actor.Player.Map; var oldRootMap=owner.Community.Residence.Map;
        map.Initialise(owner.Community.Residence);
        ((IDictionary<uint,IGridEntity>)typeof(BaseMap).GetField("entities",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(map)!)[owner.Actor.Player.Guid]=owner.Actor.Player;
        owner.Actor.Set(nameof(IPlayer.Map),map); return new(owner,map,oldMap,oldRootMap);
    }
    private static void Handle(Frame f,ClientHousingRemodel request)=>new ClientHousingRemodelHandler().HandleMessage(f.Owner.Session,request);
    private static ClientHousingRemodel Request(Frame f,uint music=29)
    {
        var request=new ClientHousingRemodel(); request.TargetResidence.RealmId=f.Root.RealmId; request.TargetResidence.ResidenceId=f.Root.Id;
        Set(request,nameof(ClientHousingRemodel.MusicId),music); return request;
    }
    private static void Set(object target,string property,object value)=>target.GetType().GetProperty(property)!.SetValue(target,value);
    private static IEnumerable<ServerHousingProperties> Packets(Frame f)=>f.Owner.Messages.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
        .Select(row=>row.Arguments[0]).OfType<ServerHousingProperties>();
    private static async Task SaveOnce(Frame f)
    {
        f.Owner.Residences.Update(61); await Wait(f.Owner,f.Owner.Residences,"saveGate","savesInFlight");
    }
    private static Task Reject(CharacterContext context)=>context.Database.ExecuteSqlRawAsync(context.Database.IsSqlite()
        ? "CREATE TRIGGER reject_remodel BEFORE UPDATE ON residence BEGIN SELECT RAISE(ABORT, 'owned remodel save failure'); END"
        : "CREATE TRIGGER reject_remodel BEFORE UPDATE ON residence FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned remodel save failure'");
    private sealed record Frame(Fixture Owner,ResidenceMapInstance Map,IBaseMap OldMap,IResidenceMapInstance OldRootMap):IDisposable
    {
        internal Residence Root=>(Residence)Owner.Community.Residence;
        internal CharacterContext Context()=>Owner.Source.Context();
        public void Dispose(){Owner.Actor.Set(nameof(IPlayer.Map),OldMap);Owner.Actor.Set(nameof(IPlayer.Session),Owner.Session);Root.Map=OldRootMap;Owner.Dispose();}
    }
}
