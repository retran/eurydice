using System.Collections.Concurrent;
using System.Threading;

namespace Eurydice.Core.Pipeline
{
    /// <summary>
    ///     Event producer.
    /// </summary>
    public interface IPipelineProducer<TOutput>
    {
        void Produce(BlockingCollection<TOutput> outputEventBuffer, CancellationToken cancellationToken);
    }
}