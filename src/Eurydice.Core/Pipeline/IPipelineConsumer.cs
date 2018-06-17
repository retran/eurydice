using System.Collections.Concurrent;

namespace Eurydice.Core.Pipeline
{
    /// <summary>
    ///     Event consumer.
    /// </summary>
    public interface IPipelineConsumer<TInput>
    {
        bool TryConsume(BlockingCollection<TInput> inputEventBuffer);
    }
}