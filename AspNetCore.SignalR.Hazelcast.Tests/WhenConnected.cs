using System;
using System.Threading.Tasks;
using Hazelcast.Client;
using Hazelcast.Config;
using Hazelcast.Core;
using NUnit.Framework;

namespace AspNetCore.SignalR.Hazelcast.Tests
{
    public class WhenConnected : GivenWhenTest
    {
        private IHazelcastInstance _client;
        private IHazelcastInstance _client2;


        protected override void Given()
        {
            var config = new ClientConfig();
            config.GetNetworkConfig().AddAddress("10.194.84.20:5701");
            config.GetNetworkConfig().SetConnectionAttemptLimit(10);

            _client = HazelcastClient.NewHazelcastClient(config);
            _client2 = HazelcastClient.NewHazelcastClient(config);

        }

        protected override void When()
        {
            var distributedObjects = _client.GetDistributedObjects();
            foreach (var distributedObject in distributedObjects)
            {
                var name = distributedObject.GetName();
                var topic = _client.GetTopic<object>(name);
            }

            //var topic = _client.GetTopic<string>("name:all");
            //topic.AddMessageListener(message =>
            //{
            //    Console.WriteLine("from 1: " + message.GetMessageObject());
            //});

            //var topic2 = _client2.GetTopic<string>("name:all");
            //topic2.AddMessageListener(message =>
            //{
            //    Console.WriteLine("from 2: " + message.GetMessageObject());
            //});

            //topic.Publish("Test 1 2");
            //topic.Publish("Test 1 2 3");
            //topic.Publish("Test 1 2 3 4");


            Task.Delay(5000);
        }

        // [Test]
        public void Then()
        {

        }
    }
}
