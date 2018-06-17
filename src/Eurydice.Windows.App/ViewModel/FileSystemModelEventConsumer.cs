using System.Collections.Concurrent;
using Eurydice.Core.Model.Events;
using Eurydice.Core.Pipeline;

namespace Eurydice.Windows.App.ViewModel
{
    /// <summary>
    ///     File system model event consumer.
    /// </summary>
    internal sealed class FileSystemModelEventConsumer : IPipelineConsumer<FileSystemModelEvent>
    {
        private readonly FileSystemViewModel _fileSystemViewModel;

        public FileSystemModelEventConsumer(FileSystemViewModel fileSystemViewModel)
        {
            _fileSystemViewModel = fileSystemViewModel;
        }

        public bool TryConsume(BlockingCollection<FileSystemModelEvent> inputEventBuffer)
        {
            if (inputEventBuffer.TryTake(out var fileSystemModelEvent))
            {
                _fileSystemViewModel.Handle(fileSystemModelEvent);
                return true;
            }

            return false;
        }
    }
}