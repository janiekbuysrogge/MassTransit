namespace MassTransit.KafkaIntegration.Transport
{
    using System.Threading;
    using System.Threading.Tasks;
    using GreenPipes;
    using Util;


    public class KeyedKafkaProducer<TKey, TValue> :
        IKafkaProducer<TValue>
        where TValue : class
    {
        readonly KafkaKeyResolver<TKey, TValue> _keyResolver;
        readonly IKafkaProducer<TKey, TValue> _producer;

        public KeyedKafkaProducer(IKafkaProducer<TKey, TValue> producer, KafkaKeyResolver<TKey, TValue> keyResolver)
        {
            _producer = producer;
            _keyResolver = keyResolver;
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer)
        {
            return _producer.ConnectSendObserver(observer);
        }

        public Task Produce(TValue message, CancellationToken cancellationToken = default)
        {
            return Produce(message, Pipe.Empty<KafkaSendContext<TValue>>(), cancellationToken);
        }

        public Task Produce(TValue message, IPipe<KafkaSendContext<TValue>> pipe, CancellationToken cancellationToken = default)
        {
            return _producer.Produce(default, message, new SetKeyPipe(_keyResolver, pipe), cancellationToken);
        }

        public Task Produce(object values, CancellationToken cancellationToken = default)
        {
            return Produce(values, Pipe.Empty<KafkaSendContext<TValue>>(), cancellationToken);
        }

        public Task Produce(object values, IPipe<KafkaSendContext<TValue>> pipe, CancellationToken cancellationToken = default)
        {
            return _producer.Produce(default, values, new SetKeyPipe(_keyResolver, pipe), cancellationToken);
        }


        class SetKeyPipe :
            IPipe<KafkaSendContext<TKey, TValue>>
        {
            readonly KafkaKeyResolver<TKey, TValue> _keyResolver;
            readonly IPipe<KafkaSendContext<TKey, TValue>> _pipe;

            public SetKeyPipe(KafkaKeyResolver<TKey, TValue> keyResolver, IPipe<KafkaSendContext<TKey, TValue>> pipe = null)
            {
                _keyResolver = keyResolver;
                _pipe = pipe;
            }

            public Task Send(KafkaSendContext<TKey, TValue> context)
            {
                context.Key = _keyResolver(context);
                return _pipe.IsNotEmpty() ? _pipe.Send(context) : TaskUtil.Completed;
            }

            public void Probe(ProbeContext context)
            {
                _pipe?.Probe(context);
            }
        }
    }
}
