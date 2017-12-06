using System;
using System.Threading.Tasks;
using Cars.EventSource.SerializedEvents;
using Cars.EventSource.Snapshots;
using Cars.EventSource.Storage;
using Cars.MessageBus.InProcess;
using Cars.Testing.Shared.StubApplication.Domain.Bar;
using Cars.Testing.Shared.StubApplication.Domain.Foo;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Cars.Testing.Shared.EventStore
{
    public class EventStoreTestSuit
    {
        private readonly EventStoreWrapper _eventStore;
        
        public EventStoreTestSuit(IEventStore eventStore)
        {
            _eventStore = new EventStoreWrapper(eventStore);
        }

        public async Task<Bar> EventTestsAsync()
        {
            var bar = GenerateBar();

            var session = CreateSession();

            await session.AddAsync(bar).ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false);

            session = CreateSession();

            var bar2 = await session.GetByIdAsync<Bar>(bar.AggregateId).ConfigureAwait(false);

            var result = _eventStore.CalledMethods.HasFlag(EventStoreMethods.Ctor
                | EventStoreMethods.BeginTransaction
                | EventStoreMethods.SaveAsync
                | EventStoreMethods.CommitAsync
                | EventStoreMethods.GetAllEventsAsync);

            bar.AggregateId.Should().Be(bar2.AggregateId);

            result.Should().BeTrue();

            return bar;
        }

        public async Task SnapshotTestsAsync()
        {
            var foo = GenerateFoo(9);

            var session = CreateSession();

            await session.AddAsync(foo).ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false);

            session = CreateSession();

            var foo2 = await session.GetByIdAsync<Foo>(foo.AggregateId).ConfigureAwait(false);
            
            var result = _eventStore.CalledMethods.HasFlag(
                EventStoreMethods.Ctor 
                | EventStoreMethods.SaveAsync
                | EventStoreMethods.SaveSnapshotAsync 
                | EventStoreMethods.CommitAsync 
                | EventStoreMethods.GetLatestSnapshotByIdAsync 
                | EventStoreMethods.GetEventsForwardAsync);

            foo.AggregateId.Should().Be(foo2.AggregateId);

            result.Should().BeTrue();
        }

        public async Task DoSomeProblemAsync()
        {
            var foo = GenerateFoo();

            var session = CreateFaultSession();

            await session.AddAsync(foo).ConfigureAwait(false);
            try
            {
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                var result = _eventStore.CalledMethods.HasFlag(EventStoreMethods.Ctor 
                    | EventStoreMethods.BeginTransaction 
                    | EventStoreMethods.SaveAsync 
                    | EventStoreMethods.Rollback);

                result.Should().BeTrue();
            }
        }

        private ISession CreateSession()
        {
            var session = new Session(new LoggerFactory(), _eventStore, new EventPublisher(StubEventRouter.Ok()), new EventSerializer(new JsonTextSerializer()), new SnapshotSerializer(new JsonTextSerializer()), null, null, new IntervalSnapshotStrategy(10));

            return session;
        }

        private ISession CreateFaultSession()
        {
            var faultSession = new Session(new LoggerFactory(), _eventStore, new EventPublisher(StubEventRouter.Fault()), new EventSerializer(new JsonTextSerializer()), new SnapshotSerializer(new JsonTextSerializer()), null, null, new IntervalSnapshotStrategy(10));

            return faultSession;
        }

        private static Foo GenerateFoo(int quantity = 10)
        {
            var foo = new Foo(Guid.NewGuid());

            for (var i = 0; i < quantity; i++)
            {
                foo.DoSomething();
            }

            return foo;
        }

        private static Bar GenerateBar(int quantity = 10)
        {
            var bar = Bar.Create(Guid.NewGuid());

            for (var i = 0; i < quantity; i++)
            {
                bar.Speak($"Hello number {i}.");
            }

            return bar;
        }
    }
}