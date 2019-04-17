using System;

using AspNetCore.SignalR.Hazelcast;

using Hazelcast.Config;

using Microsoft.AspNetCore.SignalR;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyResolverExtensions
    {
        public static ISignalRServerBuilder AddHazelcast(this ISignalRServerBuilder signalrBuilder, Action<HazelcastConfiguration> configure)
        {
            signalrBuilder.Services.Configure(configure);
            signalrBuilder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(HazelcastHubLifetimeManager<>));
            return signalrBuilder;
        }

        public static ISignalRServerBuilder AddHazelcast(this ISignalRServerBuilder signalrBuilder, Action<HazelcastConfiguration> configure, string[] hazelcastUrls)
        {
            return AddHazelcast(signalrBuilder, configuration =>
            {
                var config = new ClientConfig();
                config.GetNetworkConfig().AddAddress(hazelcastUrls);
                config.GetNetworkConfig().SetConnectionAttemptLimit(10);

                configuration.ClientConfig = config;

                configure(configuration);
            });
        }
    }
}