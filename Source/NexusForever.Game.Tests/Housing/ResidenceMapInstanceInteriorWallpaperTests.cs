using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Housing;
using NexusForever.Game.Abstract.Map.Lock;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Map.Instance;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Housing;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Model.Shared;
using NexusForever.Script;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Tests.Housing;

[Collection(LegacyServiceProviderCollection.Name)]
public class ResidenceMapInstanceInteriorWallpaperTests
{
    private const ushort RealmId = 7;
    private const ulong ResidenceId = 1234ul;
    private const ulong DecorId = 5678ul;

    [Fact]
    public void InteriorWallpaperUpdate_DefaultRestoreId_IsValidForEverySlotAndIsFree()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        try
        {
            LegacyServiceProvider.Provider = BuildConfigurationProvider();

            ResidenceMapInstance map = CreateResidenceMapInstance(out RecordingDispatchProxy<IGameTableManager> gameTableProxy);
            gameTableProxy.SetProperty(nameof(IGameTableManager.HousingWallpaperInfo), CreateGameTable(new HousingWallpaperInfoEntry
            {
                Id                 = 5u,
                Cost               = 999u,
                CostCurrencyTypeId = (uint)CurrencyType.Credits,
                Flags              = 0u
            }));

            IResidence residence = RecordingDispatchProxy<IResidence>.Create(out RecordingDispatchProxy<IResidence> residenceProxy);
            IDecor decor = RecordingDispatchProxy<IDecor>.Create(out RecordingDispatchProxy<IDecor> decorProxy);
            residenceProxy.SetProperty(nameof(IResidence.Id), ResidenceId);
            residenceProxy.SetMethodReturn(nameof(IResidence.GetChildren), Array.Empty<IResidenceChild>());
            residenceProxy.SetMethodReturn(nameof(IResidence.GetPlots), Array.Empty<IPlot>());
            residenceProxy.SetMethodReturn(nameof(IResidence.CanModifyResidence), true);
            residenceProxy.SetMethodReturn(nameof(IResidence.GetDecor), decor);
            map.Initialise(residence);

            IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
            ICurrencyManager currencyManager = RecordingDispatchProxy<ICurrencyManager>.Create(out RecordingDispatchProxy<ICurrencyManager> currencyProxy);
            playerProxy.SetProperty(nameof(IPlayer.CurrencyManager), currencyManager);

            ClientHousingInteriorWallpaperUpdate update = CreateWallpaperRestoreUpdate(slotIndex: 5);

            map.InteriorWallpaperUpdate(player, update);

            RecordingDispatchProxy<IDecor>.Invocation replace = Assert.Single(decorProxy.GetInvocations(nameof(IDecor.UpdateDecorInfoId)));
            Assert.Equal(5u, replace.Arguments[0]);
            Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CanAfford)));
            Assert.Empty(currencyProxy.GetInvocations(nameof(ICurrencyManager.CurrencySubtractAmount)));
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static IServiceProvider BuildConfigurationProvider()
    {
        var configuration = new SharedConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Realm:Map:GridUnloadTimer"] = "600",
                ["Realm:Map:InstancePlayerLimit"] = "100"
            })
            .Build());
        configuration.Initialise<TestConfiguration>();

        return new ServiceCollection()
            .AddSingleton(configuration)
            .BuildServiceProvider();
    }

    private static ResidenceMapInstance CreateResidenceMapInstance(out RecordingDispatchProxy<IGameTableManager> gameTableProxy)
    {
        IEntityFactory entityFactory = RecordingDispatchProxy<IEntityFactory>.Create(out _);
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out _);
        IMapLockManager mapLockManager = RecordingDispatchProxy<IMapLockManager>.Create(out _);
        IGlobalResidenceManager globalResidenceManager = RecordingDispatchProxy<IGlobalResidenceManager>.Create(out _);
        IGameTableManager gameTableManager = RecordingDispatchProxy<IGameTableManager>.Create(out gameTableProxy);
        IRealmContext realmContext = RecordingDispatchProxy<IRealmContext>.Create(out RecordingDispatchProxy<IRealmContext> realmProxy);
        IScriptManager scriptManager = RecordingDispatchProxy<IScriptManager>.Create(out _);

        realmProxy.SetProperty(nameof(IRealmContext.RealmId), RealmId);

        return new ResidenceMapInstance(
            entityFactory,
            publicEventManager,
            mapLockManager,
            globalResidenceManager,
            gameTableManager,
            realmContext,
            scriptManager);
    }

    private static ClientHousingInteriorWallpaperUpdate CreateWallpaperRestoreUpdate(int slotIndex)
    {
        var update = new ClientHousingInteriorWallpaperUpdate();
        for (int i = 0; i < ClientHousingInteriorWallpaperUpdate.SlotCount; i++)
        {
            update.ExistingDecorFlags.Add((uint)(i == slotIndex ? 1u : 0u));

            var decor = new DecorInfo();
            if (i == slotIndex)
            {
                decor.TargetResidence.RealmId = RealmId;
                decor.TargetResidence.ResidenceId = ResidenceId;
                SetProperty(decor, nameof(DecorInfo.DecorId), DecorId);
                SetProperty(decor, nameof(DecorInfo.DecorType), DecorType.InteriorWallpaper);
                SetProperty(decor, nameof(DecorInfo.HookIndex), (uint)slotIndex + 1u);
                SetProperty(decor, nameof(DecorInfo.DecorInfoId), 5u);
            }

            update.DecorUpdates.Add(decor);
        }

        return update;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0u : entries.Max(GetEntryId) + 1u
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(IReadOnlyList<T> entries)
    {
        if (entries.Count == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1u)).ToArray();
        for (int i = 0; i < entries.Count; i++)
            lookup[GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static uint GetEntryId<T>(T entry)
    {
        return (uint)typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0].GetValue(entry)!;
    }

    private static void SetProperty(object instance, string propertyName, object value)
    {
        PropertyInfo property = instance.GetType().GetProperty(propertyName)!;
        property.GetSetMethod(true)!.Invoke(instance, [value]);
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private sealed class TestConfiguration
    {
        public RealmConfig Realm { get; set; }
    }
}
