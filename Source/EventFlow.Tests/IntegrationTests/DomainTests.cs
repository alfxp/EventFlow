﻿// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.MetadataProviders;
using EventFlow.ReadStores.InMemory;
using EventFlow.Subscribers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Commands;
using EventFlow.TestHelpers.Aggregates.Test.Events;
using EventFlow.TestHelpers.Aggregates.Test.ReadModels;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests
{
    [TestFixture]
    public class DomainTests
    {
        public class Subscriber : ISubscribeSynchronousTo<TestAggregate, TestId, DomainErrorAfterFirstEvent>
        {
            public Task HandleAsync(IDomainEvent<TestAggregate, TestId, DomainErrorAfterFirstEvent> e, CancellationToken cancellationToken)
            {
                Console.WriteLine("Subscriber got DomainErrorAfterFirstEvent");
                return Task.FromResult(0);
            }
        }

        [Test]
        public void BasicFlow()
        {
            // Arrange
            using (var resolver = EventFlowOptions.New
                .AddEvents(typeof (TestAggregate).Assembly)
                .AddCommandHandlers(typeof(TestAggregate).Assembly)
                .AddMetadataProvider<AddGuidMetadataProvider>()
                .AddMetadataProvider<AddMachineNameMetadataProvider>()
                .AddMetadataProvider<AddEventTypeMetadataProvider>()
                .UseInMemoryReadStoreFor<TestAggregate, TestId, TestAggregateReadModel>()
                .AddSubscribers(typeof(Subscriber))
                .CreateResolver())
            {
                var commandBus = resolver.Resolve<ICommandBus>();
                var eventStore = resolver.Resolve<IEventStore>();
                var readModelStore = resolver.Resolve<IInMemoryReadModelStore<TestAggregate, TestId, TestAggregateReadModel>>();
                var id = TestId.New;

                // Act
                commandBus.Publish(new DomainErrorAfterFirstCommand(id), CancellationToken.None);
                var testAggregate = eventStore.LoadAggregate<TestAggregate, TestId>(id, CancellationToken.None);
                var testReadModel = readModelStore.Get(id);

                // Assert
                testAggregate.DomainErrorAfterFirstReceived.Should().BeTrue();
                testReadModel.DomainErrorAfterFirstReceived.Should().BeTrue();
            }
        }
    }
}
