using System;

using Hazelcast.Core;

using Microsoft.Extensions.Logging;

namespace AspNetCore.SignalR.Hazelcast
{
    public class HazelcastLifecycleListener : ILifecycleListener
    {
        private readonly ILogger _logger;

        public HazelcastLifecycleListener(ILogger logger)
        {
            _logger = logger;
        }

        public void StateChanged(LifecycleEvent lifecycleEvent)
        {
            Console.WriteLine(lifecycleEvent);

            switch (lifecycleEvent.GetState())
            {
                case LifecycleEvent.LifecycleState.Starting:
                    HazelcastLog.ConnectionRestored(_logger);
                    break;
                case LifecycleEvent.LifecycleState.Started:
                    HazelcastLog.Connected(_logger);
                    break;
                case LifecycleEvent.LifecycleState.ShuttingDown:
                    break;
                case LifecycleEvent.LifecycleState.Shutdown:
                    break;
                case LifecycleEvent.LifecycleState.Merging:
                    break;
                case LifecycleEvent.LifecycleState.Merged:
                    break;
                case LifecycleEvent.LifecycleState.ClientConnected:
                    break;
                case LifecycleEvent.LifecycleState.ClientDisconnected:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }
    }
}
