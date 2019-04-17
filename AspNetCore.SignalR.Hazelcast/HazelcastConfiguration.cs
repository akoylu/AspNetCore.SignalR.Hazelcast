using Hazelcast.Config;

namespace AspNetCore.SignalR.Hazelcast
{
    public class HazelcastConfiguration
    {
        public static int FactoryId = 42;
        public static int ClassId = 42;

        public HazelcastConfiguration()
        {
            FastMode = false;
            TopicName = "signalrTopic";
            CounterName = "signalrCounter";
            LockName = "signalrLock";
        }

        public bool FastMode { get; set; }

        public string TopicName { get; set; }

        public string CounterName { get; set; }

        public string LockName { get; set; }

        public ClientConfig ClientConfig { get; set; }
    }
}
