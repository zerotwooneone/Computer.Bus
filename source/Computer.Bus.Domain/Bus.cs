using Computer.Bus.Domain.Contracts;
using IDtoBus = Computer.Bus.Contracts.IBusClient;

namespace Computer.Bus.Domain;

public class Bus : IBus
{
    private readonly IDtoBus _dtoBus;
    private readonly IMapperFactory _mapperFactory;
    private readonly Initializer _initializer;

    public Bus(IDtoBus dtoBus,
        IMapperFactory mapperFactory,
        Initializer initializer)
    {
        _dtoBus = dtoBus;
        _mapperFactory = mapperFactory;
        _initializer = initializer;
    }

    public async Task<IPublishResult> Publish(string subjectId, string? eventId = null, string? correlationId = null)
    {
        return PublishResult.FromDto(await _dtoBus.Publish(subjectId, eventId, correlationId));
    }

    public async Task<IPublishResult> Publish(string subjectId, object param, Type type, string? eventId = null, string? correlationId = null)
    {
        if (_initializer.RegistrationsBySubject == null)
        {
            return PublishResult.CreateError($"Registrations have not been initialized");
        }
        if(!_initializer.RegistrationsBySubject.TryGetValue(subjectId, out var registration))
        {
            return PublishResult.CreateError($"Unable to find registration for subject:{subjectId}");
        }

        if (registration.Domain == null ||
            registration.Dto == null ||
            registration.Mapper == null)
        {
            return PublishResult.CreateError($"No types registered for subject:{subjectId}");
        }

        if (!registration.Domain.IsAssignableFrom(type))
        {
            return PublishResult.CreateError($"Type mismatch subject:{subjectId} domain:{type} expected:{registration.Domain}");
        }

        var mapper = _mapperFactory.GetMapper(registration.Mapper, registration.Dto, registration.Domain);
        if (mapper == null)
        {
            return PublishResult.CreateError($"Could not find a mapper. subject:{subjectId} dto:{registration.Mapper} domain:{registration.Domain}");
        }
        var dto = mapper.DomainToDto(registration.Domain, param, registration.Dto);
        if (dto == null)
        {
            return PublishResult.CreateError($"Could not convert to dto subject:{subjectId} dto:{registration.Dto} domain:{registration.Domain}");
        }
        return PublishResult.FromDto(await _dtoBus.Publish(subjectId, dto, registration.Dto, eventId, correlationId));
    }

    public async Task<ISubscription> Subscribe(string subjectId, Type type, SubscribeCallbackP callback)
    {
        if (_initializer.RegistrationsBySubject == null)
        {
            throw new InvalidOperationException($"Registrations have not been initialized");
        }
        if(!_initializer.RegistrationsBySubject.TryGetValue(subjectId, out var registration))
        {
            throw new InvalidOperationException($"Unable to find registration for subject:{subjectId}");
        }

        if (registration.Domain == null ||
            registration.Dto == null ||
            registration.Mapper == null)
        {
            throw new InvalidOperationException($"No types registered for subject:{subjectId}");
        }

        if (!registration.Domain.IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"Type mismatch subject:{subjectId} domain:{type} expected:{registration.Domain}");
        }
        async Task InnerCallback(object? param, Type? dtoType,  string eventId, string correlationId)
        {
            var mapper = _mapperFactory.GetMapper(registration.Mapper, registration.Dto, registration.Domain);
            var domain = param == null || mapper == null
                ? null
                : mapper.DtoToDomain(registration.Dto, param, registration.Domain);
            await callback(domain, registration.Domain, eventId, correlationId);
        }
        var innerSubscription = await _dtoBus.Subscribe(subjectId, type, InnerCallback);
        return new DomainSubscription(innerSubscription);
    }

    public async Task<ISubscription> Subscribe(string subjectId, SubscribeCallbackNp callback)
    {
        async Task InnerCallback(string eventId, string correlationId)
        {
            await callback(eventId, correlationId);
        }
        var innerSubscription = await _dtoBus.Subscribe(subjectId, InnerCallback);
        return new DomainSubscription(innerSubscription);
    }
}