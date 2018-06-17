using System.Collections.Concurrent;
using System.Threading;

namespace Eurydice.Core.Pipeline
{
    /// <summary>
    ///     Event processing stage.
    /// </summary>
    public interface IPipelineStage<TInput, TOutput>
    {
        void Execute(BlockingCollection<TInput> inputEventBuffer, BlockingCollection<TOutput> outputEventBuffer,
            CancellationToken cancellationToken);
    }
}