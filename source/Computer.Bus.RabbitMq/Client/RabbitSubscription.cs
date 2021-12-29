using System;
using Computer.Bus.Contracts.Models;

namespace Computer.Bus.RabbitMq.Client;

public class RabbitSubscription : ISubscription
{
    public Action Unsubscribe { get; init; } = () => { };

    public void Dispose()
    {
        Unsubscribe();
    }
}