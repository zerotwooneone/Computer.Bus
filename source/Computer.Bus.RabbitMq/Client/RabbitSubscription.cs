using System;
using System.Diagnostics.CodeAnalysis;
using Computer.Bus.Contracts.Models;

namespace Computer.Bus.RabbitMq.Client
{
    public class RabbitSubscription : ISubscription
    {
        [NotNull] public Action Unsubscribe { get; init; } = () => { };

        public void Dispose()
        {
            Unsubscribe();
        }
    }
}