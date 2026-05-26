using System.Numerics;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Configuration.Model;
using NexusForever.Game.Map;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Tests.Map;

[Collection(LegacyServiceProviderCollection.Name)]
public class BaseMapRelocateTests
{
    [Fact]
    public void RelocateEntity_WithInvalidCurrentPosition_RecoversToDestination()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildProvider();

        try
        {
            var map = new TestBaseMap();
            IGridEntity entity = RecordingDispatchProxy<IGridEntity>.Create(out RecordingDispatchProxy<IGridEntity> entityProxy);
            Vector3 destination = new(10f, 0f, 10f);

            entityProxy.SetProperty(nameof(IGridEntity.Guid), 57u);
            entityProxy.SetProperty(nameof(IGridEntity.Map), map);
            entityProxy.SetProperty(nameof(IGridEntity.Position), new Vector3(float.NaN, float.NaN, float.NaN));
            entityProxy.SetProperty(nameof(IGridEntity.ActivationRange), 0f);

            map.RelocateForTest(entity, destination);

            RecordingDispatchProxy<IGridEntity>.Invocation relocate = Assert.Single(entityProxy.GetInvocations(nameof(IGridEntity.OnRelocate)));
            Assert.Equal(destination, relocate.Arguments[0]);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static IServiceProvider BuildProvider()
    {
        var configuration = new SharedConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Realm:Map:GridUnloadTimer"] = "600"
            })
            .Build());
        configuration.Initialise<TestConfiguration>();

        return new ServiceCollection()
            .AddSingleton(configuration)
            .BuildServiceProvider();
    }

    private sealed class TestBaseMap : BaseMap
    {
        public TestBaseMap()
            : base(
                RecordingDispatchProxy<IEntityFactory>.Create(out _),
                RecordingDispatchProxy<IPublicEventManager>.Create(out _))
        {
            PropertyInfo entryProperty = typeof(BaseMap).GetProperty(nameof(Entry))!;
            entryProperty.SetValue(this, new WorldEntry { Id = 426u });
        }

        public void RelocateForTest(IGridEntity entity, Vector3 vector)
        {
            RelocateEntity(entity, vector);
        }

        protected override void SpawnGrid(uint gridX, uint gridZ)
        {
        }
    }

    private sealed class TestConfiguration
    {
        public RealmConfig Realm { get; set; }
    }
}
