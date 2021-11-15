using System;
using Computer.Bus.Contracts.Models;

namespace Computer.Bus.Contracts
{
    public interface IBusClient
    {
        PublishResult Publish(ISubjectId subjectId);
        PublishResult Publish<T>(ISubjectId subjectId, T param);
        ISubscription Subscribe(ISubjectId subjectId, Action callback);
        ISubscription Subscribe<T>(ISubjectId subjectId, Action<T> callback);
    }
}