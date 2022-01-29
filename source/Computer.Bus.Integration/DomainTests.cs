using System;
using System.Threading.Tasks;
using Computer.Bus.Domain;
using Computer.Bus.ProtobuffNet;
using Computer.Bus.RabbitMq;
using Computer.Bus.RabbitMq.Contracts;
using NUnit.Framework;

namespace Computer.Bus.Integration;

public class DomainTests
{
    private ISerializer serializer;
    [SetUp]
    public void Setup()
    {
        serializer = new ProtoSerializer();
    }

    [Test]
    public async Task Test1()
    {
        var listenStarted = new TaskCompletionSource();
        var publishCompleted = new TaskCompletionSource();
        var callbackCount = 0;
        
        var mapperFactory = new MapperFactory();
        const string subjectName = "domain subject name";

        async Task Listen()
        {
            try
            {
                var connectionFactory = new SingletonConnectionFactory();
                var clientFactory = new ClientFactory();
                var dtoClient = clientFactory.Create(serializer, connectionFactory);
                var initializer = new Domain.Initializer();
                var domainConfig = new DomainBusConfig(initializer);
                
                var registrations = new[]
                {
                    new SubjectRegistration(subjectName, typeof(subscribeDtoNameSpace.ExampleClass))
                };
                var maps = new[]
                {
                    new MapRegistration(typeof(subscribeDomainNameSpace.ExampleClass),
                    typeof(subscribeDtoNameSpace.ExampleClass),
                    typeof(subscribeDtoNameSpace.ExampleClassMapper))
                };
                domainConfig.Register(registrations, maps);
                
                var client = new Domain.Bus(dtoClient, mapperFactory, initializer);

                async Task Callback(object? obj, Type? type, string eventId, string correlationId)
                {
                    callbackCount++;
                }

                using var subscription = await client.Subscribe(subjectName,typeof(subscribeDomainNameSpace.ExampleClass), Callback).ConfigureAwait(false);
                listenStarted.TrySetResult();

                await publishCompleted.Task.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                listenStarted.TrySetException(e);
                throw;
            }
        }

        const int expectedCallbacks = 50;

        async Task Send()
        {
            try
            {
                var connectionFactory = new SingletonConnectionFactory();
                var clientFactory = new ClientFactory();
                var dtoClient = clientFactory.Create(serializer, connectionFactory);
                var initializer = new Domain.Initializer();
                var domainConfig = new DomainBusConfig(initializer);
                var registrations = new[]
                {
                    new SubjectRegistration(subjectName, typeof(publishDtoNameSpace.ExampleClass))
                };
                var maps = new[]
                {
                    new MapRegistration(typeof(publishDomainNameSpace.ExampleClass),
                        typeof(publishDtoNameSpace.ExampleClass),
                        typeof(publishDtoNameSpace.ExampleClassMapper))
                };
                domainConfig.Register(registrations, maps);
                var client = new Domain.Bus(dtoClient, mapperFactory, initializer);
                await listenStarted.Task.ConfigureAwait(false);
                for (var pubCount = 0; pubCount < expectedCallbacks; pubCount++)
                {
                    var eventId = Guid.NewGuid().ToString();
                    var correlationId = Guid.NewGuid().ToString();
                    var result = await client.Publish(subjectName, new publishDomainNameSpace.ExampleClass
                        {
                            SomeOtherTest = new ulong[1],
                            Test = "something"
                        }, typeof(publishDomainNameSpace.ExampleClass), 
                        eventId, correlationId).ConfigureAwait(false);
                    Assert.IsTrue(result.Success);
                    Task.Delay(100).Wait();
                }

                publishCompleted.TrySetResult();
            }
            catch (Exception e)
            {
                publishCompleted.TrySetException(e);
                throw;
            }
        }

        await Task.WhenAll(Send(), Listen()).ConfigureAwait(false);
    }
}