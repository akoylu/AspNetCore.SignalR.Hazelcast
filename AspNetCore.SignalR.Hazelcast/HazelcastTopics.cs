using System.Runtime.CompilerServices;

namespace AspNetCore.SignalR.Hazelcast
{
    internal class HazelcastTopics
    {
        private readonly string _prefix;

        /// <summary>
        /// Gets the name of the channel for sending to all connections.
        /// </summary>
        public string All { get; }

        /// <summary>
        /// Gets the name of the internal channel for group management messages.
        /// </summary>
        public string GroupManagement { get; set; }
        
        public HazelcastTopics(string prefix)
        {
            _prefix = prefix;

            All = _prefix + ":all";
            GroupManagement = _prefix + ":internal:groups";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Connection(string connectionId)
        {
            return _prefix + ":connection:" + connectionId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Group(string groupName)
        {
            return _prefix + ":group:" + groupName;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string User(string userId)
        {
            return _prefix + ":user:" + userId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Ack(string serverName)
        {
            return _prefix + ":internal:ack:" + serverName;
        }
    }
}
