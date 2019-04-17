using System;
using System.Linq;

using Hazelcast.Config;

using Microsoft.Extensions.Logging;

namespace AspNetCore.SignalR.Hazelcast
{
    internal static class HazelcastLog
    {
        private static readonly Action<ILogger, string, string, Exception> _connectingToEndpoints =
            LoggerMessage.Define<string, string>(LogLevel.Information, new EventId(1, "ConnectingToEndpoints"), "Connecting to Hazelcast endpoints: {Endpoints}. Using Server Name: {ServerName}");

        private static readonly Action<ILogger, Exception> _connected =
            LoggerMessage.Define(LogLevel.Information, new EventId(2, "Connected"), "Connected to Hazelcast.");

        private static readonly Action<ILogger, string, Exception> _subscribing =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(3, "Subscribing"), "Subscribing to topic: {Topic}.");

        private static readonly Action<ILogger, string, Exception> _receivedFromTopic =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(4, "ReceivedFromTopic"), "Received message from Hazelcast topic {Topic}.");

        private static readonly Action<ILogger, string, Exception> _publishToTopic =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(5, "PublishToTopic"), "Publishing message to Hazelcast topic {Topic}.");

        private static readonly Action<ILogger, string, Exception> _unsubscribe =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(6, "Unsubscribe"), "Unsubscribing from topic: {Topic}.");

        private static readonly Action<ILogger, Exception> _notConnected =
            LoggerMessage.Define(LogLevel.Error, new EventId(7, "Connected"), "Not connected to Redis.");

        private static readonly Action<ILogger, Exception> _connectionRestored =
            LoggerMessage.Define(LogLevel.Information, new EventId(8, "ConnectionRestored"), "Connection to Redis restored.");

        private static readonly Action<ILogger, Exception> _connectionFailed =
            LoggerMessage.Define(LogLevel.Error, new EventId(9, "ConnectionFailed"), "Connection to Redis failed.");

        private static readonly Action<ILogger, Exception> _failedWritingMessage =
            LoggerMessage.Define(LogLevel.Warning, new EventId(10, "FailedWritingMessage"), "Failed writing message.");

        private static readonly Action<ILogger, Exception> _internalMessageFailed =
            LoggerMessage.Define(LogLevel.Warning, new EventId(11, "InternalMessageFailed"), "Error processing message for internal server message.");

        public static void ConnectingToEndpoints(ILogger logger, ClientConfig clientConfig, string serverName)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                var addresses = clientConfig.GetNetworkConfig().GetAddresses();

                if (addresses.Any())
                {
                    _connectingToEndpoints(logger, string.Join(", ", addresses), serverName, null);
                }
            }
        }

        public static void Connected(ILogger logger)
        {
            _connected(logger, null);
        }

        public static void Subscribing(ILogger logger, string topicName)
        {
            _subscribing(logger, topicName, null);
        }

        public static void ReceivedFromTopic(ILogger logger, string topicName)
        {
            _receivedFromTopic(logger, topicName, null);
        }

        public static void PublishToTopic(ILogger logger, string topicName)
        {
            _publishToTopic(logger, topicName, null);
        }

        public static void Unsubscribe(ILogger logger, string topicName)
        {
            _unsubscribe(logger, topicName, null);
        }

        public static void NotConnected(ILogger logger)
        {
            _notConnected(logger, null);
        }

        public static void ConnectionRestored(ILogger logger)
        {
            _connectionRestored(logger, null);
        }

        public static void ConnectionFailed(ILogger logger, Exception exception)
        {
            _connectionFailed(logger, exception);
        }

        public static void FailedWritingMessage(ILogger logger, Exception exception)
        {
            _failedWritingMessage(logger, exception);
        }

        public static void InternalMessageFailed(ILogger logger, Exception exception)
        {
            _internalMessageFailed(logger, exception);
        }

        // This isn't DefineMessage-based because it's just the simple TextWriter logging from ConnectionMultiplexer
        public static void InstanceMessage(ILogger logger, string message)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                // We tag it with EventId 100 though so it can be pulled out of logs easily.
                logger.LogDebug(new EventId(100, "HazelcastConnectionLog"), message);
            }
        }
    }
}
