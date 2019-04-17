using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hazelcast.Client;
using Hazelcast.Core;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCore.SignalR.Hazelcast
{
    public class HazelcastHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable
        where THub : Hub
    {
        private readonly ILogger _logger;
        private readonly HubConnectionStore _connections = new HubConnectionStore();
        private readonly Dictionary<string, IDistributedObject> _topics = new Dictionary<string, IDistributedObject>();
        private readonly string _serverName = GenerateServerName();
        
        private IHazelcastInstance _hzInstance;
        private readonly HazelcastConfiguration _configuration;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1);
        private readonly HazelcastTopics _hazelcastTopics;
        private readonly HazelcastProtocol _protocol;
        
        private readonly AckHandler _ackHandler;
        
        public HazelcastHubLifetimeManager(ILogger<HazelcastHubLifetimeManager<THub>> logger, IOptions<HazelcastConfiguration> configuration, IHubProtocolResolver hubProtocolResolver)
        {
            _logger = logger;
            _ackHandler = new AckHandler();
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
            _hazelcastTopics = new HazelcastTopics(typeof(THub).FullName);
            _protocol = new HazelcastProtocol(hubProtocolResolver.AllProtocols);

            HazelcastLog.ConnectingToEndpoints(logger, _configuration.ClientConfig, _serverName);

            _ = EnsureHazelcastServerConnection();
        }

        public override async Task OnConnectedAsync(HubConnectionContext connection)
        {
            await EnsureHazelcastServerConnection();
            var feature = new HazelcastFeature();
            connection.Features.Set<IHazelcastFeature>(feature);

            _connections.Add(connection);

            SubscribeToConnection(connection);

            // TODO: SubscribeToUser(connection);
        }

        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            _connections.Remove(connection);

            var connectionTopic = _hazelcastTopics.Connection(connection.ConnectionId);
            HazelcastLog.Unsubscribe(_logger, connectionTopic);
            var topic = _hzInstance.GetTopic<byte[]>(connectionTopic);
            topic.Destroy();

            var feature = connection.Features.Get<IHazelcastFeature>();
            var groupNames = feature.Groups;
            if (groupNames != null)
            {
                // TODO: Implement remove groupNames
            }

            if (!string.IsNullOrEmpty(connection.UserIdentifier))
            {
                // TODO: tasks.Add(RemoveUserAsync(connection));
            }

            return Task.CompletedTask;
        }

        public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = new CancellationToken())
        {
            var message = _protocol.WriteInvocation(methodName, args);
            return PublishAsync(_hazelcastTopics.All, message);
        }

        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendConnectionAsync(string connectionId, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendGroupAsync(string groupName, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendUserAsync(string userId, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task AddToGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task RemoveFromGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        private async Task PublishAsync(string topic, byte[] payload)
        {
            await EnsureHazelcastServerConnection();
            HazelcastLog.PublishToTopic(_logger, topic);
            ((ITopic<byte[]>)_topics[topic]).Publish(payload);
        }

        public void Dispose()
        {
            foreach (var distributedObject in _topics)
            {
                distributedObject.Value.Destroy();
            }
            _hzInstance.Shutdown();
            _ackHandler.Dispose();
        }

        private async Task EnsureHazelcastServerConnection()
        {
            if (_hzInstance == null)
            {
                await _connectionLock.WaitAsync();
                try
                {
                    _hzInstance = HazelcastClient.NewHazelcastClient(_configuration.ClientConfig);
                    _hzInstance.GetLifecycleService().AddLifecycleListener(new HazelcastLifecycleListener(_logger));

                    HazelcastLog.Connected(_logger);

                    SubscribeToAll();
                    // TODO: SubscribeToGroupManagementChannel()
                    SubscribeToAckChannel();
                }
                catch (Exception exception)
                {
                    HazelcastLog.ConnectionFailed(_logger, exception);
                    throw;
                }
                finally
                {
                    _connectionLock.Release();
                }
            }
        }

        private void SubscribeToAll()
        {
            HazelcastLog.Subscribing(_logger, _hazelcastTopics.All);
            var topic = _hzInstance.GetTopic<byte[]>(_hazelcastTopics.All);
            _topics.Add(_hazelcastTopics.All, topic);

            topic.AddMessageListener(topicMessage =>
            {
                try
                {
                    HazelcastLog.ReceivedFromTopic(_logger, _hazelcastTopics.All);

                    var invocation = _protocol.ReadInvocation(topicMessage.GetMessageObject());

                    var tasks = new List<Task>(_connections.Count);

                    foreach (var connection in _connections)
                    {
                        if (invocation.ExcludedConnectionIds == null ||
                            !invocation.ExcludedConnectionIds.Contains(connection.ConnectionId))
                        {
                            tasks.Add(connection.WriteAsync(invocation.Message).AsTask());
                        }
                    }

                    Task.WhenAll(tasks).GetAwaiter().GetResult();
                }
                catch (Exception exception)
                {
                    HazelcastLog.FailedWritingMessage(_logger, exception);
                }
            });
        }

        private void SubscribeToAckChannel()
        {
            var topicName = _hazelcastTopics.Ack(_serverName);
            var topic = _hzInstance.GetTopic<int>(topicName);
            _topics.Add(topicName, topic);

            topic.AddMessageListener(topicMessage =>
            {
                var ackId = topicMessage.GetMessageObject();

                _ackHandler.TriggerAck(ackId);
            });
        }

        private void SubscribeToConnection(HubConnectionContext connection)
        {
            var connectionTopic = _hazelcastTopics.Connection(connection.ConnectionId);

            HazelcastLog.Subscribing(_logger, connectionTopic);
            var topic = _hzInstance.GetTopic<byte[]>(connectionTopic);
            _topics.Add(connectionTopic, topic);

            topic.AddMessageListener(topicMessage =>
            {
                var invocation = _protocol.ReadInvocation(topicMessage.GetMessageObject());
                connection.WriteAsync(invocation.Message).GetAwaiter().GetResult();
            });
        }

        private interface IHazelcastFeature
        {
            HashSet<string> Groups { get; }
        }

        private class HazelcastFeature : IHazelcastFeature
        {
            public HashSet<string> Groups { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private static string GenerateServerName()
        {
            // Use the machine name for convenient diagnostics, but add a guid to make it unique.
            // Example: MyServerName_02db60e5fab243b890a847fa5c4dcb29
            return $"{Environment.MachineName}_{Guid.NewGuid():N}";
        }
    }
}
