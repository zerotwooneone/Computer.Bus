using System;
using Computer.Bus.Contracts;
using Computer.Bus.Contracts.Models;
using Computer.Bus.RabbitMq.Serialize;

namespace Computer.Bus.RabbitMq.Client
{
    public class BusClient : IBusClient
    {
        private readonly QueueClient _queueClient;
        private readonly ISerializer _serializer;

        public BusClient(
            QueueClient queueClient,
            ISerializer serializer)
        {
            _queueClient = queueClient;
            _serializer = serializer;
        }
        public PublishResult Publish(ISubjectId subjectId)
        {
            return _queueClient.Publish(subjectId.SubjectName);
        }

        public PublishResult Publish<T>(ISubjectId subjectId, T param)
        {
            var body = _serializer.Serialize(param);
            return _queueClient.Publish(subjectId.SubjectName, body);
        }

        public ISubscription Subscribe(ISubjectId subjectId, Action callback)
        {
            return _queueClient.Subscribe(subjectId.SubjectName, callback);
        }

        public ISubscription Subscribe<T>(ISubjectId subjectId, Action<T> callback)
        {
            void innerCallback(byte[] b)
            {
                var body = _serializer.Deserialize<T>(b);
                callback(body);
            }
            return _queueClient.Subscribe(subjectId.SubjectName, innerCallback);
        }
    }
}