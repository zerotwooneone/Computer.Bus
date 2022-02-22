using Computer.Bus.Domain.Contracts;
using Computer.Bus.Domain.Contracts.Models;
using Computer.Bus.Domain.Model;
using ISubscription = Computer.Bus.Domain.Contracts.Models.ISubscription;
using IDtoRequestService = Computer.Bus.Contracts.IRequestService;

namespace Computer.Bus.Domain;

public class RequestService : IRequestService
{
    private readonly IDtoRequestService _dtoRequestService;
    private readonly IMapperFactory _mapperFactory;
    private readonly Initializer _initializer;

    public RequestService(IDtoRequestService dtoRequestService,
        IMapperFactory mapperFactory,
        Initializer initializer)
    {
        _dtoRequestService = dtoRequestService;
        _mapperFactory = mapperFactory;
        _initializer = initializer;
    }
    public async Task<IResponse> Request(
        object? request, Type requestType, string requestSubject, 
        Type responseType, string responseSubject,
        string? eventId = null, string? correlationId = null, 
        CancellationToken cancellationToken = default)
    {
        if (_initializer.RegistrationsBySubject == null)
        {
            return  GenericResponse.CreateError($"Registrations have not been initialized");
        }
        if(!_initializer.RegistrationsBySubject.TryGetValue(requestSubject, out var requestRegistration))
        {
            return GenericResponse.CreateError($"Unable to find registration for subject:{requestSubject}");
        }
        if (requestRegistration.Domain == null ||
            requestRegistration.Dto == null ||
            requestRegistration.Mapper == null)
        {
            return GenericResponse.CreateError($"No types registered for subject:{requestSubject}");
        }

        if (!requestRegistration.Domain.IsAssignableFrom(requestType))
        {
            return GenericResponse.CreateError($"Type mismatch subject:{requestSubject} domain:{requestType} expected:{requestRegistration.Domain}");
        }
        if(!_initializer.RegistrationsBySubject.TryGetValue(responseSubject, out var responseRegistration))
        {
            return GenericResponse.CreateError($"Unable to find registration for subject:{responseSubject}");
        }
        if (responseRegistration.Domain == null ||
            responseRegistration.Dto == null ||
            responseRegistration.Mapper == null)
        {
            return GenericResponse.CreateError($"No types registered for subject:{responseSubject}");
        }

        if (!responseRegistration.Domain.IsAssignableFrom(responseType))
        {
            return GenericResponse.CreateError($"Type mismatch subject:{responseSubject} domain:{responseType} expected:{responseRegistration.Domain}");
        }

        var requestMapper = _mapperFactory.GetMapper(requestRegistration.Mapper, requestRegistration.Dto,
            requestRegistration.Domain);
        if (requestMapper == null)
        {
            return GenericResponse.CreateError($"Could not find a mapper. subject:{requestSubject} dto:{requestRegistration.Mapper} domain:{requestRegistration.Domain}");
        }
        var responseMapper = _mapperFactory.GetMapper(responseRegistration.Mapper, responseRegistration.Dto,
            responseRegistration.Domain);
        if (responseMapper == null)
        {
            return GenericResponse.CreateError($"Could not find a mapper. subject:{responseSubject} dto:{responseRegistration.Mapper} domain:{responseRegistration.Domain}");
        }

        var requestDto = request == null
            ? null 
            : requestMapper.DomainToDto(requestRegistration.Domain, request,requestRegistration.Dto);
        var rawResponse = await _dtoRequestService.Request(requestDto,requestRegistration.Dto,requestSubject,
            responseRegistration.Dto,responseSubject, 
            eventId: eventId, correlationId: correlationId, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!rawResponse.Success)
        {
            return GenericResponse.CreateError(rawResponse.ErrorReason ?? "Unknown error occurred handling response",
                rawResponse.EventId, rawResponse.CorrelationId);
        }
        
        var response = rawResponse.Obj == null
            ? null
            : responseMapper.DtoToDomain(responseRegistration.Dto, rawResponse.Obj, responseRegistration.Domain);
        
        return GenericResponse.CreateSuccess(response, rawResponse.EventId ?? Guid.NewGuid().ToString(), rawResponse.CorrelationId ?? Guid.NewGuid().ToString());
    }

    public async Task<ISubscription> Listen(string requestSubject, Type requestType, 
        string responseSubject, Type responseType,
        IRequestService.CreateResponse createResponse,
        IRequestService.ErrorCallback? errorCallback = null)
    {
        if (_initializer.RegistrationsBySubject == null)
        {
            throw new InvalidOperationException($"Registrations have not been initialized");
        }
        if(!_initializer.RegistrationsBySubject.TryGetValue(requestSubject, out var requestRegistration))
        {
            throw new InvalidOperationException($"Unable to find registration for subject:{requestSubject}");
        }
        if (requestRegistration.Domain == null ||
            requestRegistration.Dto == null ||
            requestRegistration.Mapper == null)
        {
            throw new InvalidOperationException($"No types registered for subject:{requestSubject}");
        }

        if (!requestRegistration.Domain.IsAssignableFrom(requestType))
        {
            throw new InvalidOperationException($"Type mismatch subject:{requestSubject} domain:{requestType} expected:{requestRegistration.Domain}");
        }
        if(!_initializer.RegistrationsBySubject.TryGetValue(responseSubject, out var responseRegistration))
        {
            throw new InvalidOperationException($"Unable to find registration for subject:{responseSubject}");
        }
        if (responseRegistration.Domain == null ||
            responseRegistration.Dto == null ||
            responseRegistration.Mapper == null)
        {
            throw new InvalidOperationException($"No types registered for subject:{responseSubject}");
        }

        if (!responseRegistration.Domain.IsAssignableFrom(responseType))
        {
            throw new InvalidOperationException($"Type mismatch subject:{responseSubject} domain:{responseType} expected:{responseRegistration.Domain}");
        }

        var requestMapper = _mapperFactory.GetMapper(requestRegistration.Mapper, requestRegistration.Dto,
            requestRegistration.Domain);
        if (requestMapper == null)
        {
            throw new InvalidOperationException($"Could not find a mapper. subject:{requestSubject} dto:{requestRegistration.Mapper} domain:{requestRegistration.Domain}");
        }
        var responseMapper = _mapperFactory.GetMapper(responseRegistration.Mapper, responseRegistration.Dto,
            responseRegistration.Domain);
        if (responseMapper == null)
        {
            throw new InvalidOperationException($"Could not find a mapper. subject:{responseSubject} dto:{responseRegistration.Mapper} domain:{responseRegistration.Domain}");
        }
        
        async Task<(object?, Type)> InnerCreateResponse(object? dtoRequest, Type? type, string eventId, string correlationId)
        {
            var domainRequest = dtoRequest == null
                ? null
                : requestMapper.DtoToDomain(requestRegistration.Dto, dtoRequest, requestRegistration.Domain);
            var domainResponse = await createResponse(domainRequest, requestRegistration.Domain, eventId, correlationId).ConfigureAwait(false);
            var dtoResponse = domainResponse.Item1 == null
                ? null
                : responseMapper.DomainToDto(domainResponse.Item2, domainResponse.Item1, responseRegistration.Dto);
            return (dtoResponse, responseRegistration.Dto);
        }

        void InnerDtoErrorCallback(string reason, object? o, Type? type, string? eid, string? cid)
        {
            errorCallback?.Invoke(reason, default, type, eid, cid);
        }

        IDtoRequestService.ErrorCallback? innerDtoErrorCallback = errorCallback == null
            ? (IDtoRequestService.ErrorCallback?)null
            : InnerDtoErrorCallback;
        var dtoSubscription = await _dtoRequestService.Listen(requestSubject, requestRegistration.Dto, 
            responseSubject, responseRegistration.Dto,
            InnerCreateResponse, innerDtoErrorCallback).ConfigureAwait(false);
        return new RequestSubscription(dtoSubscription);
    }
}