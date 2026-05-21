using Microsoft.Extensions.Hosting;
using NexusForever.Server.GroupServer.Network.Internal;
using Rebus.Bus;

namespace NexusForever.Server.GroupServer
{
    public class HostedService : IHostedService
    {
        #region Dependency Injection

        private readonly IBus _bus;

        public HostedService(
            IBus bus)
        {
            _bus = bus;
        }

        #endregion

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await GroupServerBusSubscriptions.SubscribeAll(_bus);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
