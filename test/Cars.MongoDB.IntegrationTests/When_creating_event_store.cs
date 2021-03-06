using System;
using System.Collections.Generic;
using Cars.EventStore.MongoDB;
using FluentAssertions;
using MongoDB.Driver;
using Xunit;

namespace Cars.MongoDB.IntegrationTests
{
	public class When_creating_event_store : IDisposable
    {

        public const string CategoryName = "Integration";
        public const string CategoryValue = "MongoDB";
        public const string DatabaseName = "EventStoreCreateTests";

        private readonly IMongoEventStoreSettings _defaultSettings = new MongoEventStoreSettings {Database = DatabaseName};

        private readonly MongoClient _mongoClient;

        public When_creating_event_store()
        {
            if (string.IsNullOrWhiteSpace(TestSettings.MongoHost)) throw new NullReferenceException("The variable 'MONGODB_HOST' was not configured.");

            _mongoClient = new MongoClient($"mongodb://{TestSettings.MongoHost}");

            _mongoClient.DropDatabase(DatabaseName);
        }

        [Trait(CategoryName, CategoryValue)]
        [Theory, MemberData(nameof(InvalidStates))]
        public void Should_validate_constructor_parameters(MongoClient mongoClient, MongoEventStoreSettings settings)
        {
            Action action = () => new MongoEventStore(mongoClient, settings);

            action.ShouldThrowExactly<ArgumentNullException>();
        }
        

        [Trait(CategoryName, CategoryValue)]
        [Fact]
        public void Should_use_default_settings()
        {
            using (var eventStore = new MongoEventStore(_mongoClient, new MongoEventStoreSettings()))
            {
                eventStore.Settings.EventsCollectionName.Should().Be(_defaultSettings.EventsCollectionName);
	            eventStore.Settings.Database.Should().Be("EventStore");
            }
        }

        [Trait(CategoryName, CategoryValue)]
        [Fact]
        public void Should_validate_settings()
        {
            var defaultSettings = new MongoEventStoreSettings
            {
                EventsCollectionName = null,
				Database = DatabaseName
            };
            
            Action action = () => new MongoEventStore(new MongoClient(), defaultSettings);

            action.ShouldThrowExactly<ArgumentNullException>();
        }

        [Trait(CategoryName, CategoryValue)]
        [Fact]
        public void Should_use_custom_settings()
        {
            var customSettings = new MongoEventStoreSettings
            {
                EventsCollectionName = "MyEvents",
				Database = DatabaseName
            };

            using (var eventStore = new MongoEventStore(_mongoClient, customSettings))
            {
                eventStore.Settings.EventsCollectionName.Should().Be(customSettings.EventsCollectionName);
            }
        }

        public static IEnumerable<object[]> InvalidStates => new[]
        {
            new object[] { null, new MongoEventStoreSettings() },
            new object[] { new MongoClient(), null }
        };

        public void Dispose()
        {
            _mongoClient.DropDatabase(DatabaseName);
        }
    }
}