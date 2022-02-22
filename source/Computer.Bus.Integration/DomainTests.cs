using System;
using System.Threading;
using System.Threading.Tasks;
using Computer.Bus.Domain;
using Computer.Bus.Domain.Contracts;
using Computer.Bus.ProtobuffNet;
using Computer.Bus.RabbitMq;
using Computer.Bus.RabbitMq.Contracts;
using NUnit.Framework;
using publishDtoNameSpace;
using ExampleClass = subscribeDomainNameSpace.ExampleClass;

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
    public async Task PubSub()
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
                
                void ErrorCallback(string reason, object? param, Type? type, string? eid, string? cid)
                {
                    Assert.Fail(reason);
                }

                using var subscription = await client.Subscribe(subjectName,typeof(subscribeDomainNameSpace.ExampleClass), 
                    Callback, ErrorCallback).ConfigureAwait(false);
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
            await Task.Delay(100).ConfigureAwait(false);
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
                    await Task.Delay(100).ConfigureAwait(false);
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
        Assert.AreEqual(expectedCallbacks, callbackCount);
    }

    [Test]
    public async Task Request()
    {
        var listenStarted = new TaskCompletionSource();
        var publishCompleted = new TaskCompletionSource();
        var callbackCount = 0;
        
        var mapperFactory = new MapperFactory();
        const string requestSubject = "domain request subject";
        const string responseSubject = "domain response subject";

        async Task Listen()
        {
            try
            {
                var connectionFactory = new SingletonConnectionFactory();
                var clientFactory = new ClientFactory();
                var dtoClient = clientFactory.Create(serializer, connectionFactory);
                var dtoRequestService = new Computer.Bus.RabbitMq.Client.RequestService(dtoClient);
                
                var initializer = new Domain.Initializer();
                var domainConfig = new DomainBusConfig(initializer);
                
                var registrations = new[]
                {
                    new SubjectRegistration(requestSubject, typeof(subscribeDtoNameSpace.ExampleClass)),
                    new SubjectRegistration(responseSubject, typeof(subscribeDtoNameSpace.ExampleClass))
                };
                var maps = new[]
                {
                    new MapRegistration(typeof(subscribeDomainNameSpace.ExampleClass),
                    typeof(subscribeDtoNameSpace.ExampleClass),
                    typeof(subscribeDtoNameSpace.ExampleClassMapper))
                };
                domainConfig.Register(registrations, maps);
                
                //var bus = new Domain.Bus(dtoClient, mapperFactory, initializer);
                var requestService = new RequestService(dtoRequestService, mapperFactory, initializer);
                
                Task<subscribeDomainNameSpace.ExampleClass?> CreateResponse(
                    subscribeDomainNameSpace.ExampleClass? request, string eventId, string correlationId)
                {
                    callbackCount++;
                    return Task.FromResult<subscribeDomainNameSpace.ExampleClass?>(new subscribeDomainNameSpace.ExampleClass{Test = "response", SomeOtherTest = new []{(ulong)1,(ulong)3,(ulong)5,(ulong)9}});
                }
                
                void ErrorCallback(string reason, ExampleClass? exampleClass, string? eid, string? cid)
                {
                    Assert.Fail(reason);
                }

                var x = requestService.Listen<subscribeDomainNameSpace.ExampleClass, subscribeDomainNameSpace.ExampleClass>(requestSubject, responseSubject,
                    CreateResponse, ErrorCallback);
                //using var subscription = await client.Subscribe(subjectName,typeof(subscribeDomainNameSpace.ExampleClass), Callback).ConfigureAwait(false);
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
                await Task.Delay(100).ConfigureAwait(false);
                var connectionFactory = new SingletonConnectionFactory();
                var clientFactory = new ClientFactory();
                var dtoClient = clientFactory.Create(serializer, connectionFactory);
                var dtoRequestService = new Computer.Bus.RabbitMq.Client.RequestService(dtoClient);
                var initializer = new Domain.Initializer();
                var domainConfig = new DomainBusConfig(initializer);
                var registrations = new[]
                {
                    new SubjectRegistration(requestSubject, typeof(publishDtoNameSpace.ExampleClass)),
                    new SubjectRegistration(responseSubject, typeof(publishDtoNameSpace.ExampleClass))
                };
                var maps = new[]
                {
                    new MapRegistration(typeof(publishDomainNameSpace.ExampleClass),
                        typeof(publishDtoNameSpace.ExampleClass),
                        typeof(publishDtoNameSpace.ExampleClassMapper))
                };
                domainConfig.Register(registrations, maps);
                var requestService = new RequestService(dtoRequestService, mapperFactory, initializer);
                await listenStarted.Task.ConfigureAwait(false);
                for (var pubCount = 0; pubCount < expectedCallbacks; pubCount++)
                {
                    var eventId = Guid.NewGuid().ToString();
                    var correlationId = Guid.NewGuid().ToString();
                    var request = new publishDomainNameSpace.ExampleClass
                    {
                        SomeOtherTest = new ulong[1],
                        Test = "something"
                    };
                    //var result = await client.Publish(subjectName, request, typeof(publishDomainNameSpace.ExampleClass), 
                    //    eventId, correlationId).ConfigureAwait(false);
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var result = await requestService
                        .Request<publishDomainNameSpace.ExampleClass, publishDomainNameSpace.ExampleClass>(
                            request, requestSubject, responseSubject, 
                            eventId, correlationId, cts.Token).ConfigureAwait(false);
                    Assert.IsTrue(result.Success);
                    await Task.Delay(100).ConfigureAwait(false);
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
        Assert.AreEqual(expectedCallbacks, callbackCount);
    }
}