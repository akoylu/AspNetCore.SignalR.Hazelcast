using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace AspNetCore.SignalR.Hazelcast
{
    public struct HazelcastInvocation
    {
        /// <summary>
        /// Gets a list of connections that should be excluded from this invocation.
        /// May be null to indicate that no connections are to be excluded.
        /// </summary>
        public IReadOnlyList<string> ExcludedConnectionIds { get; }

        /// <summary>
        /// Gets the message serialization cache containing serialized payloads for the message.
        /// </summary>
        public SerializedHubMessage Message { get; }

        public HazelcastInvocation(SerializedHubMessage message, IReadOnlyList<string> excludedConnectionIds)
        {
            Message = message;
            ExcludedConnectionIds = excludedConnectionIds;
        }

        public static HazelcastInvocation Create(string target, object[] arguments, IReadOnlyList<string> excludedConnectionIds = null)
        {
            return new HazelcastInvocation(new SerializedHubMessage(new InvocationMessage(Guid.NewGuid().ToString(), target, arguments)), excludedConnectionIds);
        }
    }
}
