using System.Threading.Tasks;
using Computer.Bus.Contracts.Models;

namespace Computer.Bus.Contracts;

public interface IBusClient
{
    Task<IPublishResult> Publish(string subjectId,
        string? eventId = null, string? correlationId = null);

    Task<IPublishResult> Publish<T>(string subjectId,
        T? param,
        string? eventId = null, string? correlationId = null);

    Task<ISubscription> Subscribe<T>(string subjectId, SubscribeCallback<T> callback);
}

public delegate Task SubscribeCallback<in T>(T? param, string eventId, string correlationId);

