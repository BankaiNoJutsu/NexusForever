using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Entity;
using NexusForever.Game.Housing;
using NexusForever.Game.Map;
using NexusForever.Game.Map.Instance;
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

public sealed class HousingAssociationPublicationTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public HousingAssociationPublicationTests() { }
    internal HousingAssociationPublicationTests(DbContextOptions<CharacterContext> options)=>this.options=options;

    [Fact]
    public async Task PlacementRetainsLiveAssociationAndMapUntilSqlConfirms()
    {
        using var f=await Create(); Place(f);
        Assert.Null(f.Home.GuildOwnerId); Assert.Null(f.Home.Parent); Assert.Equal(PropertyInfoId.Residence,f.Home.PropertyInfoId);
        Assert.Same(f.Map,f.Home.Map); Assert.Null(f.Map.UnloadStatus); Assert.Empty(PlacementPackets(f));
        await Flush(f); using var sql=f.Context(); Assert.Equal(77ul,sql.Residence.Single(row=>row.Id==101).GuildOwnerId);
        Assert.Equal(PropertyInfoId.CommunityResidence1,f.Home.PropertyInfoId); Assert.Single(PlacementPackets(f));
    }

    [Fact]
    public async Task PlacementCannotReplyBeforeItsCommittedSqlAssociation()
    {
        using var f=await Create(); bool premature=false; int replies=0;
        f.Owner.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted),args=>
        {
            if(args[0] is ServerHousingCommunityPlacement)
            {replies++;using var sql=f.Context();var home=sql.Residence.Single(row=>row.Id==101);premature|=home.GuildOwnerId!=77||home.PropertyInfoId!=(byte)PropertyInfoId.CommunityResidence1;}
            return null;
        });
        Place(f); await Flush(f); Assert.False(premature); Assert.Equal(1,replies);
    }

    [Fact]
    public async Task FailedPlacementSqlCannotChangeLinksGeometryOrUnloadTheMap()
    {
        using var f=await Create(); using(var setup=f.Context())await Reject(setup);
        Place(f); await Tick(f);
        using(var failed=f.Context())
        {
            Assert.Null(failed.Residence.Single(row=>row.Id==101).GuildOwnerId); Assert.Null(f.Home.Parent);
            Assert.Equal(PropertyInfoId.Residence,f.Home.PropertyInfoId); Assert.Null(f.Map.UnloadStatus); Assert.Empty(PlacementPackets(f));
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_association");
        }
        await Flush(f); using var sql=f.Context(); Assert.Equal(77ul,sql.Residence.Single(row=>row.Id==101).GuildOwnerId); Assert.Single(PlacementPackets(f));
    }

    [Fact]
    public async Task RemovalRetainsItsSlotReservationAndMapUntilSqlConfirms()
    {
        using var f=await Create(true); Remove(f);
        Assert.Equal(77ul,f.Home.GuildOwnerId); Assert.Same(f.Owner.Community.Residence,f.Home.Parent);
        Assert.Equal(0,f.Owner.Community.GetMember(42).CommunityPlotReservation); Assert.Same(f.Map,f.Home.Map);
        Assert.Empty(f.Travel.GetInvocations(nameof(IMapManager.AddToMap)));
        await Flush(f); using var sql=f.Context(); Assert.Null(sql.Residence.Single(row=>row.Id==101).GuildOwnerId);
        Assert.Equal(-1,sql.GuildMember.Single(row=>row.Id==77&&row.CharacterId==42).CommunityPlotReservation);
        Assert.Single(f.Travel.GetInvocations(nameof(IMapManager.AddToMap)));
    }

    [Fact]
    public async Task FailedRemovalCannotCommitOnlyTheMemberReservation()
    {
        using var f=await Create(true); using(var setup=f.Context())await Reject(setup);
        Remove(f); await Tick(f);
        using(var failed=f.Context())
        {
            Assert.Equal(77ul,failed.Residence.Single(row=>row.Id==101).GuildOwnerId);
            Assert.Equal(0,failed.GuildMember.Single(row=>row.Id==77&&row.CharacterId==42).CommunityPlotReservation);
            Assert.Equal(0,f.Owner.Community.GetMember(42).CommunityPlotReservation); Assert.Same(f.Map,f.Home.Map);
            Assert.Empty(f.Travel.GetInvocations(nameof(IMapManager.AddToMap)));
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_association");
        }
        await Flush(f); using var sql=f.Context(); Assert.Null(sql.Residence.Single(row=>row.Id==101).GuildOwnerId);
        Assert.Equal(-1,sql.GuildMember.Single(row=>row.Id==77&&row.CharacterId==42).CommunityPlotReservation);
    }

    [Fact]
    public async Task AnOldMapCannotClearTheResidencePointerOwnedByANewMap()
    {
        using var f=await Create(); var replacement=Map(f.Owner); replacement.Initialise(f.Home);
        typeof(ResidenceMapInstance).GetMethod("RemoveResidence",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(f.Map,new object[]{f.Home});
        Assert.Same(replacement,f.Home.Map);
    }

    [Fact]
    public async Task WrongRealmPlacementStillRejectsBeforeAnySideEffect()
    {
        using var f=await Create(); var request=Placement(f);request.TargetResidence.RealmId=2;
        Assert.Throws<InvalidPacketValueException>(()=>f.Place.HandleMessage(f.Owner.Session,request));
        Assert.Null(f.Home.GuildOwnerId);Assert.Null(f.Map.UnloadStatus);Assert.Empty(PlacementPackets(f));
    }

    [Fact]
    public async Task MembershipRemovalAfterPlacementSqlCannotLeaveAnUnownedChildAssociation()
    {
        using var f=await Create();Place(f);await Tick(f);
        typeof(NexusForever.Game.Guild.Community).GetMethod("RemoveMember",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(f.Owner.Community,new object[]{42ul,false});await Flush(f);
        using var sql=f.Context();Assert.DoesNotContain(sql.GuildMember,row=>row.Id==77&&row.CharacterId==42);
        Assert.Null(sql.Residence.Single(row=>row.Id==101).GuildOwnerId);Assert.Null(f.Home.Parent);Assert.Empty(PlacementPackets(f));
    }

    [Fact]
    public async Task DisbandAfterPlacementSqlCannotLeaveAChildBoundToTheDeletedCommunity()
    {
        using var f=await Create();Place(f);await Tick(f);f.Owner.Community.DisbandGuild();await Flush(f);
        using var sql=f.Context();Assert.NotNull(sql.Guild.Single(row=>row.Id==77).DeleteTime);
        Assert.Null(sql.Residence.Single(row=>row.Id==101).GuildOwnerId);Assert.Null(f.Home.Parent);Assert.Empty(PlacementPackets(f));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SqlCompletionCannotApplyTheAssociationOrTravel(bool remove)
    {
        using var f=await Create(remove);if(remove)Remove(f);else Place(f);await Tick(f);
        using(var sql=f.Context())Assert.Equal(remove?null:77ul,sql.Residence.Single(row=>row.Id==101).GuildOwnerId);
        Assert.Equal(remove?77ul:null,f.Home.GuildOwnerId);Assert.Same(f.Map,f.Home.Map);Assert.Null(f.Map.UnloadStatus);
        Assert.Empty(PlacementPackets(f));Assert.Empty(f.Travel.GetInvocations(nameof(IMapManager.AddToMap)));
        await Flush(f);Assert.Equal(remove?null:77ul,f.Home.GuildOwnerId);
    }

    [Fact]
    public async Task FailedPlotWriteRollsBackHeaderAndGeometryTogether()
    {
        using var f=await Create();using(var setup=f.Context())await Reject(setup,"residence_plot");Place(f);await Tick(f);
        using(var failed=f.Context())
        {
            Assert.Null(failed.Residence.Single(row=>row.Id==101).GuildOwnerId);Assert.Equal((ushort)4,failed.ResidencePlot.Single(row=>row.Id==101).PlotInfoId);
            Assert.Equal(PropertyInfoId.Residence,f.Home.PropertyInfoId);Assert.Equal(4u,f.Home.GetPlot(0).PlotInfoEntry.Id);
            await failed.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_association");
        }
        await Flush(f);using var sql=f.Context();Assert.Equal((ushort)5,sql.ResidencePlot.Single(row=>row.Id==101).PlotInfoId);
    }

    [Fact]
    public async Task ChangedPacketObjectsCannotChangeTheirAcceptedSlot()
    {
        using var f=await Create();var request=Placement(f);f.Place.HandleMessage(f.Owner.Session,request);
        request.GetType().GetProperty(nameof(ClientHousingCommunityPlacement.PropertyIndex))!.SetValue(request,1u);
        await Flush(f);Assert.Equal(PropertyInfoId.CommunityResidence1,f.Home.PropertyInfoId);Assert.Equal(0u,Assert.Single(PlacementPackets(f)).PropertyIndex);
    }

    [Fact]
    public async Task AFailedPrivateReplyRetriesFreshWithoutRepeatingTheMapTransition()
    {
        using var f=await Create();bool failed=false;var attempts=new List<ServerHousingCommunityPlacement>();
        f.Owner.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted),args=>
        {
            if(args[0] is ServerHousingCommunityPlacement packet)
            {attempts.Add(packet);if(!failed){failed=true;packet.TargetResidence.ResidenceId=999;throw new IOException("owned placement reply failure");}}
            return null;
        });
        Place(f);await Flush(f);Assert.True(failed);Assert.Equal(2,attempts.Count);Assert.NotSame(attempts[0],attempts[1]);
        Assert.Equal(100ul,attempts[1].TargetResidence.ResidenceId);Assert.NotNull(f.Map.UnloadStatus);Assert.Same(f.Owner.Community.Residence,f.Home.Parent);
    }

    [Fact]
    public async Task AcceptedRepositionRequestsKeepTheirSlotAndReplyOrder()
    {
        using var f=await Create();Place(f);Place(f,1);await Flush(f);
        Assert.Equal(new uint[]{0,1},PlacementPackets(f).Select(packet=>packet.PropertyIndex));
        Assert.Equal(PropertyInfoId.CommunityResidence2,f.Home.PropertyInfoId);
        using var sql=f.Context();Assert.Equal((byte)PropertyInfoId.CommunityResidence2,sql.Residence.Single(row=>row.Id==101).PropertyInfoId);
    }

    [Fact]
    public async Task ReentrantUpdatesCannotReplayTheHeadPlacement()
    {
        using var f=await Create();bool entered=false;
        f.Owner.Messages.SetMethodHandler(nameof(IWorldSession.EnqueueMessageEncrypted),args=>
        {if(args[0] is ServerHousingCommunityPlacement&&!entered){entered=true;f.Owner.Residences.Update(0);}return null;});
        Place(f);await Flush(f);Assert.True(entered);Assert.Single(PlacementPackets(f));
    }

    [Fact]
    public async Task ForeignMixedGuildAndChildOutcomesCannotBeAdopted()
    {
        using var f=await Create(true);using(var setup=f.Context())await Reject(setup);Remove(f);await Tick(f);
        using(var foreign=f.Context())
        {
            await foreign.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_association");
            await foreign.Database.ExecuteSqlRawAsync("UPDATE residence SET guildOwnerId=NULL,propertyInfoId=35 WHERE id=101");
            await foreign.Database.ExecuteSqlRawAsync("UPDATE residence_plot SET plotInfoId=4 WHERE id=101");
        }
        await Pump(f);Assert.Equal(77ul,f.Home.GuildOwnerId);Assert.Equal(0,f.Owner.Community.GetMember(42).CommunityPlotReservation);
        using var sql=f.Context();Assert.Equal(0,sql.GuildMember.Single(row=>row.Id==77&&row.CharacterId==42).CommunityPlotReservation);
        Assert.Empty(f.Travel.GetInvocations(nameof(IMapManager.AddToMap)));
    }

    [Fact]
    public async Task AReplacedSessionBeforePreparationCannotSaveOrReply()
    {
        using var f=await Create();Place(f);var session=RecordingDispatchProxy<IWorldSession>.Create(out var proxy);proxy.SetProperty(nameof(IWorldSession.Player),f.Owner.Actor.Player);
        f.Owner.Actor.Set(nameof(IPlayer.Session),session);await Flush(f);Assert.Null(f.Home.GuildOwnerId);Assert.Empty(PlacementPackets(f));
        using var sql=f.Context();Assert.Null(sql.Residence.Single(row=>row.Id==101).GuildOwnerId);
    }

    [Fact]
    public async Task LaterFlagsAndMemberNotesRemainDirtyThroughTheConfirmedApply()
    {
        using var f=await Create(true);Remove(f);await Tick(f);f.Home.Flags=(ResidenceFlags)16;f.Owner.Community.GetMember(42).Note="Later";
        await Flush(f);using var sql=f.Context();Assert.Equal((ushort)16,sql.Residence.Single(row=>row.Id==101).Flags);
        Assert.Equal("Later",sql.GuildMember.Single(row=>row.Id==77&&row.CharacterId==42).Note);Assert.Null(f.Home.Parent);
    }

    [Fact]
    public async Task EarlierResidenceTransactionsSettleBeforeAssociationCapture()
    {
        using var f=await Create();using(var context=f.Context())
        {
            using var transaction=await context.Database.BeginTransactionAsync();f.Home.Flags=(ResidenceFlags)16;f.Home.Save(context);await context.SaveChangesAsync();
            Place(f);await Tick(f);Assert.Null(f.Home.GuildOwnerId);Assert.Null(f.Map.UnloadStatus);await transaction.CommitAsync();
        }
        await Flush(f);using var sql=f.Context();Assert.Equal(77ul,sql.Residence.Single(row=>row.Id==101).GuildOwnerId);Assert.Equal((ushort)16,sql.Residence.Single(row=>row.Id==101).Flags);
    }

    [Fact]
    public async Task OccupiedSlotsRejectWithoutUnloadingThePersonalResidence()
    {
        using var f=await Create();var other=new Residence(new ResidenceModel{Id=102,OwnerId=43,Name="Other",PropertyInfoId=(byte)PropertyInfoId.CommunityResidence1},gameTableManager:f.Owner.Source.Source.Tables);
        f.Owner.Community.Residence.AddChild(other,true);Assert.Throws<InvalidPacketValueException>(()=>Place(f));
        Assert.Null(f.Home.GuildOwnerId);Assert.Null(f.Map.UnloadStatus);Assert.Empty(PlacementPackets(f));
    }

    [Fact]
    public async Task PreparedAssociationRejectsOneSidedGuildAndResidenceSaves()
    {
        using var f=await Create(true);Remove(f);await Tick(f);
        using(var guild=f.Context())Assert.Throws<InvalidOperationException>(()=>f.Owner.Community.Save(guild));
        using(var child=f.Context())Assert.Throws<InvalidOperationException>(()=>f.Home.Save(child));
        using(var parent=f.Context())Assert.Throws<InvalidOperationException>(()=>f.Owner.Community.Residence.Save(parent));
        await Flush(f);Assert.Null(f.Home.Parent);Assert.Single(f.Travel.GetInvocations(nameof(IMapManager.AddToMap)));
    }

    [Fact]
    public async Task FailedTravelAdmissionRetriesItsOwnPendingTeleport()
    {
        using var f=await Create(true);bool failed=false;int accepted=0;
        f.Travel.SetMethodHandler(nameof(IMapManager.AddToMap),args=>
        {if(!failed){failed=true;throw new IOException("owned map admission failure");}accepted++;return null;});
        Remove(f);await Flush(f);Assert.True(failed);Assert.Equal(1,accepted);
        Assert.Equal(2,f.Travel.GetInvocations(nameof(IMapManager.AddToMap)).Count);
        using var sql=f.Context();Assert.Null(sql.Residence.Single(row=>row.Id==101).GuildOwnerId);
    }

    [Fact]
    public async Task WrongContextCannotPoisonTheAcceptedAssociationsRecoveryWitness()
    {
        using var f=await Create();using var other=await new HousingAssociationPublicationTests().Create();
        other.Home.Flags=(ResidenceFlags)16;using(var changed=other.Context()){other.Home.Save(changed);await changed.SaveChangesAsync();}
        Place(f);
        var command=typeof(NexusForever.Game.Guild.Community).GetProperty("CurrentAssociation",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(f.Owner.Community)!;
        var attempt=(Task)command.GetType().GetMethod("Run",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(command,new object[]{(Func<CharacterContext>)(()=>other.Context())})!;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>attempt);
        await Flush(f);Assert.Equal(77ul,f.Home.GuildOwnerId);Assert.Single(PlacementPackets(f));
        using var sql=other.Context();Assert.Null(sql.Residence.Single(row=>row.Id==101).GuildOwnerId);Assert.Equal((ushort)16,sql.Residence.Single(row=>row.Id==101).Flags);
    }

    [Fact]
    public async Task UntouchedUnresolvedPlotRowsSurviveAssociationApply()
    {
        using var f=await Create(rawPlot:true);var destination=Map(f.Owner);destination.Initialise(f.Owner.Community.Residence);
        f.Owner.Actor.Set(nameof(IPlayer.Map),destination);
        ((IDictionary<uint,IGridEntity>)typeof(BaseMap).GetField("entities",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(destination)!)[f.Owner.Actor.Player.Guid]=f.Owner.Actor.Player;
        Place(f);await Flush(f);
        Assert.Same(f.Owner.Community.Residence,f.Home.Parent);Assert.Single(PlacementPackets(f));
        using var sql=f.Context();Assert.Equal((ushort)65000,sql.ResidencePlot.Single(row=>row.Id==101&&row.Index==7).PlotInfoId);
        var packet=Assert.Single(f.Owner.Messages.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
            .Select(row=>row.Arguments[0]).OfType<ServerHousingPlots>(),row=>row.ResidenceId==101);
        Assert.Equal(65000u,packet.Plots.Single(row=>row.PlotPropertyIndex==7).PlotInfoId);
    }

    [Fact]
    public async Task AMapMoveAfterSqlSuppressesTheOldRequestTravel()
    {
        using var f=await Create(true);Remove(f);await Tick(f);f.Owner.Actor.Set(nameof(IPlayer.Map),RecordingDispatchProxy<IResidenceMapInstance>.Create(out _));
        await Flush(f);Assert.Empty(f.Travel.GetInvocations(nameof(IMapManager.AddToMap)));
    }

    [Fact]
    public async Task FailedPlugAttachmentMustFinishBeforePlacementSuccess()
    {
        using var f=await Create();var destination=Map(f.Owner);destination.Initialise(f.Owner.Community.Residence);
        var factory=RecordingDispatchProxy<IEntityFactory>.Create(out var creation);bool failed=false;int created=0;
        creation.SetMethodHandler(nameof(IEntityFactory.CreateEntity),args=>
        {if(!failed){failed=true;throw new IOException("owned plug creation failure");}created++;return RecordingDispatchProxy<IPlugEntity>.Create(out _);});
        typeof(ResidenceMapInstance).GetField("entityFactory",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(destination,factory);
        f.Home.GetPlot(0).PlugItemEntry=f.Owner.Source.Source.Tables.HousingPlugItem.GetEntry(531);
        using(var setup=f.Context()){f.Home.Save(setup);await setup.SaveChangesAsync();}
        Place(f);await Flush(f);Assert.True(failed);Assert.Equal(1,created);Assert.NotNull(f.Home.GetPlot(0).PlugEntity);Assert.Single(PlacementPackets(f));
    }

    [Fact]
    public async Task FailedPlugRemovalCannotBeSkippedOnTheNextWorldTick()
    {
        using var f=await Create(true);var plug=RecordingDispatchProxy<IPlugEntity>.Create(out var entity);entity.SetProperty(nameof(IPlugEntity.Map),f.Map);
        bool failed=false;int removed=0;entity.SetMethodHandler(nameof(IPlugEntity.RemoveFromMap),args=>
        {if(!failed){failed=true;throw new IOException("owned plug removal failure");}removed++;return null;});
        f.Home.GetPlot(0).PlugItemEntry=f.Owner.Source.Source.Tables.HousingPlugItem.GetEntry(531);f.Home.GetPlot(0).PlugEntity=plug;
        using(var setup=f.Context()){f.Home.Save(setup);await setup.SaveChangesAsync();}
        Remove(f);await Flush(f);Assert.True(failed);Assert.Equal(1,removed);
    }

    [Fact]
    public async Task DepartureDuringTravelCannotReceivePlacementSuccess()
    {
        using var f=await Create(true);bool left=false;
        f.Travel.SetMethodHandler(nameof(IMapManager.AddToMap),args=>
        {
            if(!left){left=true;typeof(NexusForever.Game.Guild.Community).GetMethod("RemoveMember",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(f.Owner.Community,new object[]{42ul,false});}
            return null;
        });
        Place(f,1);await Flush(f);Assert.True(left);Assert.Empty(PlacementPackets(f));
        // Departure was accepted during the preceding world publication, after
        // that tick's guild save. Advance its next normal persistence interval.
        await Flush(f);
        using var sql=f.Context();Assert.Null(sql.Residence.Single(row=>row.Id==101).GuildOwnerId);
    }

    [Fact]
    public async Task AdministratorRemovalPublishesForAnOwnerWithoutAMemberRow()
    {
        using var f=await Create(true,orphanOwner:true);Assert.Null(f.Owner.Community.GetMember(43));
        Remove(f);await Flush(f);
        Assert.Null(f.Home.Parent);Assert.Empty(f.Travel.GetInvocations(nameof(IMapManager.AddToMap)));
        Assert.Contains(f.Owner.Messages.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted))
            .Select(row=>row.Arguments[0]).OfType<ServerHousingProperties>().SelectMany(row=>row.Residences),
            row=>row.ResidenceId==101&&row.ResidenceDeleted);
        using var sql=f.Context();Assert.Null(sql.Residence.Single(row=>row.Id==101).GuildOwnerId);
    }

    private async Task<Frame> Create(bool placed=false,bool rawPlot=false,bool orphanOwner=false)
    {
        var owner=await new HousingRenameFeeRecoveryTests(options).Create();
        var tables=(RecordingDispatchProxy<IGameTableManager>)(object)owner.Source.Source.Tables;
        tables.SetProperty(nameof(IGameTableManager.HousingPlotInfo),PlayerBindPointTests.Table(
            new HousingPlotInfoEntry{Id=3,HousingPropertyInfoId=(uint)PropertyInfoId.Community,HousingPropertyPlotIndex=0},
            new HousingPlotInfoEntry{Id=4,HousingPropertyInfoId=(uint)PropertyInfoId.Residence,HousingPropertyPlotIndex=0},
            new HousingPlotInfoEntry{Id=5,HousingPropertyInfoId=(uint)PropertyInfoId.CommunityResidence1,HousingPropertyPlotIndex=0},
            new HousingPlotInfoEntry{Id=6,HousingPropertyInfoId=(uint)PropertyInfoId.CommunityResidence2,HousingPropertyPlotIndex=0}));
        using(var seed=owner.Source.Context())
        {
            var homeModel=new ResidenceModel{Id=101,OwnerId=orphanOwner?43ul:42ul,GuildOwnerId=placed?77ul:null,Name=orphanOwner?"Other":"Leader",PrivacyLevel=(byte)ResidencePrivacyLevel.Private,
                PropertyInfoId=(byte)(placed?PropertyInfoId.CommunityResidence1:PropertyInfoId.Residence),Plot=[new ResidencePlotModel{Id=101,Index=0,PlotInfoId=(ushort)(placed?5:4)}]};
            if(rawPlot)homeModel.Plot.Add(new ResidencePlotModel{Id=101,Index=7,PlotInfoId=65000});seed.Residence.Add(homeModel);
            await seed.SaveChangesAsync();
        }
        var databases=(IDatabaseManager)typeof(GlobalResidenceManager).GetField("databaseManager",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(owner.Residences)!;
        var home=new Residence(databases.GetDatabase<CharacterDatabase>().GetResidences().Single(row=>row.Id==101),owner.Residences,1,owner.Source.Source.Tables);
        typeof(GlobalResidenceManager).GetMethod("StoreResidence",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(owner.Residences,new object[]{home,orphanOwner?"Other":"Leader"});
        if(placed)
        {
            owner.Community.Residence.AddChild(home,false);if(!orphanOwner)owner.Community.GetMember(42).CommunityPlotReservation=0;
            using var context=owner.Source.Context();owner.Community.Save(context);await context.SaveChangesAsync();
        }
        var oldHousing=owner.Actor.Player.ResidenceManager;var housing=RecordingDispatchProxy<IResidenceManager>.Create(out var hp);hp.SetProperty(nameof(IResidenceManager.Residence),home);
        owner.Actor.Set(nameof(IPlayer.ResidenceManager),housing);
        var map=Map(owner);map.Initialise(placed?owner.Community.Residence:home);var oldMap=owner.Actor.Player.Map;owner.Actor.Set(nameof(IPlayer.Map),map);
        ((IDictionary<uint,IGridEntity>)typeof(BaseMap).GetField("entities",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(map)!)[owner.Actor.Player.Guid]=owner.Actor.Player;
        var mapManager=RecordingDispatchProxy<IMapManager>.Create(out var travel);var managerField=typeof(Player).GetField("mapManager",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var oldManager=managerField.GetValue(owner.Actor.Player);managerField.SetValue(owner.Actor.Player,mapManager);
        var entrance=(IResidenceEntrance)typeof(ClientHousingCommunityUpdateHandlerTests).GetMethod("CreateEntrance",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null)!;
        var geography=RecordingDispatchProxy<IGlobalResidenceManager>.Create(out var geo);geo.SetMethodReturn(nameof(IGlobalResidenceManager.GetResidenceEntrance),entrance);
        var realm=RecordingDispatchProxy<IRealmContext>.Create(out var rp);rp.SetProperty(nameof(IRealmContext.RealmId),(ushort)1);
        var locks=RecordingDispatchProxy<IMapLockManager>.Create(out var lp);lp.SetMethodReturn(nameof(IMapLockManager.GetResidenceLock),RecordingDispatchProxy<NexusForever.Game.Map.Lock.IResidenceMapLock>.Create(out _));
        return new(owner,home,map,oldMap,oldHousing,oldManager,travel,new(geography,realm,locks),new(geography,locks,realm));
    }
    private static ResidenceMapInstance Map(Fixture owner)
    {
        var map=new ResidenceMapInstance(null,null,null,owner.Residences,owner.Source.Source.Tables,null,null);
        typeof(BaseMap).GetProperty(nameof(BaseMap.Entry))!.SetValue(map,new WorldEntry{Id=42});
        typeof(MapInstance).GetProperty(nameof(MapInstance.MapLock))!.SetValue(map,RecordingDispatchProxy<NexusForever.Game.Map.Lock.IResidenceMapLock>.Create(out _));
        return map;
    }
    private static ClientHousingCommunityPlacement Placement(Frame f,uint index=0)
    {var request=new ClientHousingCommunityPlacement();request.TargetResidence.RealmId=1;request.TargetResidence.ResidenceId=100;request.GetType().GetProperty(nameof(ClientHousingCommunityPlacement.PropertyIndex))!.SetValue(request,index);return request;}
    private static void Place(Frame f,uint index=0)=>f.Place.HandleMessage(f.Owner.Session,Placement(f,index));
    private static void Remove(Frame f)
    {var request=new ClientHousingCommunityRemoval();request.GetType().GetProperty(nameof(ClientHousingCommunityRemoval.TargetResidence))!.SetValue(request,new NexusForever.Network.World.Message.Model.Shared.TargetResidence{RealmId=1,ResidenceId=101});f.Remove.HandleMessage(f.Owner.Session,request);}
    private static IEnumerable<ServerHousingCommunityPlacement> PlacementPackets(Frame f)=>f.Owner.Messages.GetInvocations(nameof(IWorldSession.EnqueueMessageEncrypted)).Select(row=>row.Arguments[0]).OfType<ServerHousingCommunityPlacement>();
    private static async Task Tick(Frame f)
    {f.Owner.Guilds.Update(61);f.Owner.Residences.Update(61);await Wait(f.Owner,f.Owner.Residences,"saveGate","savesInFlight");}
    private static async Task Flush(Frame f)
    {
        await Pump(f);
        await Wait(f.Owner,f.Owner.Guilds,"cacheLock","guildSavesInFlight");
    }
    private static async Task Pump(Frame f)
    {
        for(int tick=0;tick<12;tick++)
        {
            f.Owner.Actor.Events.Update(.01);f.Owner.Guilds.Update(tick==0?61:0);f.Owner.Residences.Update(tick==0?61:0);
            await Wait(f.Owner,f.Owner.Residences,"saveGate","savesInFlight");await f.Owner.Actor.Drain(allowErrors:true);await Task.Delay(10);
        }
    }
    private static Task Reject(CharacterContext context,string table="residence")=>context.Database.ExecuteSqlRawAsync(context.Database.IsSqlite()
        ? $"CREATE TRIGGER reject_association BEFORE UPDATE ON {table} BEGIN SELECT RAISE(ABORT, 'owned association SQL failure'); END"
        : $"CREATE TRIGGER reject_association BEFORE UPDATE ON {table} FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='owned association SQL failure'");
    private sealed record Frame(Fixture Owner,Residence Home,ResidenceMapInstance Map,IBaseMap OldMap,IResidenceManager OldHousing,object OldManager,
        RecordingDispatchProxy<IMapManager> Travel,ClientHousingCommunityPlacementHandler Place,ClientHousingCommunityRemovalHandler Remove):IDisposable
    {
        internal CharacterContext Context()=>Owner.Source.Context();
        public void Dispose(){Owner.Actor.Set(nameof(IPlayer.Map),OldMap);Owner.Actor.Set(nameof(IPlayer.Session),Owner.Session);Owner.Actor.Set(nameof(IPlayer.ResidenceManager),OldHousing);typeof(Player).GetField("mapManager",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(Owner.Actor.Player,OldManager);Owner.Dispose();}
    }
}
