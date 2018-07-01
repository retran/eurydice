using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Eurydice.Core.Model.Events;
using Eurydice.Core.Pipeline;
using Eurydice.Core.Watcher.Events;

namespace Eurydice.Windows.App.ViewModel
{
    /// <summary>
    ///     Pipeline manager.
    /// </summary>
    internal class Pipeline : IDisposable
    {
        private readonly IPipelineConsumer<FileSystemModelEvent> _consumer;
        private readonly CancellationTokenSource _cst;
        private readonly DispatcherTimer _dispatcherTimer;
        private readonly IPipelineProducer<FileSystemEvent> _producer;
        private readonly BlockingCollection<FileSystemEvent> _producerEventBuffer;
        private readonly IPipelineStage<FileSystemEvent, FileSystemModelEvent> _stage;
        private readonly BlockingCollection<FileSystemModelEvent> _stageEventBuffer;
        private readonly Action _updateModel;
        private readonly TimeSpan _updateTimeTreshold = TimeSpan.FromMilliseconds(10);
        private bool _disposed;
        private Task _producerTask;
        private bool _running;
        private Task _stageTask;

        public Pipeline(IPipelineProducer<FileSystemEvent> producer,
            IPipelineStage<FileSystemEvent, FileSystemModelEvent> stage,
            IPipelineConsumer<FileSystemModelEvent> consumer,
            Action updateModel,
            TimeSpan updateInterval)
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _stage = stage ?? throw new ArgumentNullException(nameof(stage));
            _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
            _updateModel = updateModel;

            _producerEventBuffer = new BlockingCollection<FileSystemEvent>();
            _stageEventBuffer = new BlockingCollection<FileSystemModelEvent>();
            _cst = new CancellationTokenSource();

            _dispatcherTimer = new DispatcherTimer {Interval = updateInterval};
            _dispatcherTimer.Tick += OnTick;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            Cancel();
            _dispatcherTimer.Tick -= OnTick;
            _cst.Dispose();
        }

        private void OnTick(object sender, EventArgs e)
        {
            var dt = DateTime.UtcNow;
            while (_consumer.TryConsume(_stageEventBuffer))
            {
                if (DateTime.UtcNow - dt > _updateTimeTreshold) break;
            }

            _updateModel();
        }

        public void Run()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().FullName);
            if (_running) throw new InvalidOperationException();

            _running = true;

            _producerTask = Task.Factory.StartNew(() => { _producer.Produce(_producerEventBuffer, _cst.Token); },
                _cst.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);

            _stageTask = Task.Factory.StartNew(
                () => { _stage.Execute(_producerEventBuffer, _stageEventBuffer, _cst.Token); }, _cst.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);

            _dispatcherTimer.Start();
        }

        public void Cancel()
        {
            if (!_running)
            {
                return;
            }

            _dispatcherTimer.Stop();
            _cst.Cancel();
            Task.WaitAll(_producerTask, _stageTask);
        }
    }
}