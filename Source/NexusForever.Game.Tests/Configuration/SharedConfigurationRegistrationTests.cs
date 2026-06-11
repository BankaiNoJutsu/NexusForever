using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Shared;
using NexusForever.Shared.Configuration;

namespace NexusForever.Game.Tests.Configuration;

public class SharedConfigurationRegistrationTests
{
    [Fact]
    public void AddSharedConfiguration_InitialisesConfigurationBeforeResolution()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Test:Value"] = "ready"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddSharedConfiguration<TestServerConfiguration>(configuration);

        ISharedConfiguration sharedConfiguration = services
            .BuildServiceProvider()
            .GetRequiredService<ISharedConfiguration>();
        TestConfig testConfig = sharedConfiguration.Get<TestConfig>();

        Assert.Equal("ready", testConfig.Value);
    }

    [Fact]
    public void Get_BeforeInitialiseThrowsClearException()
    {
        var sharedConfiguration = new SharedConfiguration(new ConfigurationBuilder().Build());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => sharedConfiguration.Get<TestConfig>());

        Assert.Contains("must be initialised", exception.Message);
    }

    private sealed class TestServerConfiguration
    {
        public TestConfig Test { get; set; }
    }

    [ConfigurationBind]
    private sealed class TestConfig
    {
        public string Value { get; set; }
    }
}
