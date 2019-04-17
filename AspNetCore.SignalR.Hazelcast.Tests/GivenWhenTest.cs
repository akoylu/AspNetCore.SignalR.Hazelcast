namespace AspNetCore.SignalR.Hazelcast.Tests
{
    public abstract class GivenWhenTest
    {
        protected GivenWhenTest()
        {
            Run();
        }

        protected void Run()
        {
            Given();

            When();
        }

        protected abstract void Given();

        protected abstract void When();
    }
}
