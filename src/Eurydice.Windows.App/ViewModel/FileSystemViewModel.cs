using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Eurydice.Core.Common;
using Eurydice.Core.Model;
using Eurydice.Core.Model.Events;
using Eurydice.Windows.App.Common;

namespace Eurydice.Windows.App.ViewModel
{
    /// <summary>
    ///     File system view model.
    /// </summary>
    internal sealed class FileSystemViewModel : ViewModelBase
    {
        public delegate void NodeDeletedEventHandler(FileSystemEntryId id);

        public delegate void UnrecoverableErrorEventHandler(Exception exception);

        private readonly ICommand _navigateToCommand;

        private readonly Dictionary<FileSystemEntryId, FileSystemNodeViewModel> _nodeLookupTable =
            new Dictionary<FileSystemEntryId, FileSystemNodeViewModel>();

        private ObservableCollection<FileSystemNodeViewModel> _root =
            new ObservableCollection<FileSystemNodeViewModel>();

        private FileSystemModelState _state;

        public FileSystemViewModel(ICommand navigateToCommand)
        {
            _navigateToCommand = navigateToCommand ?? throw new ArgumentNullException(nameof(navigateToCommand));
        }

        public ObservableCollection<FileSystemNodeViewModel> Root
        {
            get => _root;
            set
            {
                _root = value;
                OnPropertyChanged(nameof(Root));
            }
        }

        public FileSystemModelState State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        public event NodeDeletedEventHandler NodeDeleted;

        public event UnrecoverableErrorEventHandler ErrorReceived;

        public void Update(FileSystemNodeViewModel rootNode)
        {
            if (rootNode == null) throw new ArgumentNullException(nameof(rootNode));

            var queue = new Queue<FileSystemNodeViewModel>();
            queue.Enqueue(rootNode);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var node in current.Nodes) queue.Enqueue(node);
                current.Update();
            }
        }

        public void Handle(FileSystemModelEvent fileSystemModelEvent)
        {
            if (fileSystemModelEvent == null) throw new ArgumentNullException(nameof(fileSystemModelEvent));

            switch (fileSystemModelEvent)
            {
                case NodeCreatedEvent nodeCreatedEvent:
                    CreateNode(nodeCreatedEvent);
                    break;

                case NodeChangedEvent nodeChangedEvent:
                    ChangeNode(nodeChangedEvent);
                    break;

                case NodeDeletedEvent nodeDeletedEvent:
                    DeleteNode(nodeDeletedEvent);
                    break;

                case NodeRenamedEvent nodeRenamedEvent:
                    RenameNode(nodeRenamedEvent);
                    break;

                case FileSystemModelStateChangedEvent modelStateChangedEvent:
                    State = modelStateChangedEvent.State;
                    break;

                case FileSystemModelUnrecoverableErrorEvent unrecoverableErrorEvent:
                    ErrorReceived?.Invoke(unrecoverableErrorEvent.Exception);
                    break;
            }
        }

        private void RenameNode(NodeRenamedEvent nodeRenamedEvent)
        {
            if (!_nodeLookupTable.TryGetValue(nodeRenamedEvent.OldId, out var renamedNode)) return;

            _nodeLookupTable.Remove(renamedNode.Id);
            _nodeLookupTable.Add(nodeRenamedEvent.NewId, renamedNode);
            renamedNode.Rename(nodeRenamedEvent.NewId, nodeRenamedEvent.Name);
        }

        private void DeleteNode(NodeDeletedEvent nodeDeletedEvent)
        {
            if (!_nodeLookupTable.TryGetValue(nodeDeletedEvent.Id, out var deletedNode)) return;

            NodeDeleted?.Invoke(nodeDeletedEvent.Id);

            var queue = new Queue<FileSystemNodeViewModel>();
            queue.Enqueue(deletedNode);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var node in current.Nodes) queue.Enqueue(node);
                _nodeLookupTable.Remove(current.Id);
            }

            var parentNode = deletedNode.Parent;
            parentNode.Nodes.Remove(deletedNode);
        }

        private void ChangeNode(NodeChangedEvent nodeChangedEvent)
        {
            if (!_nodeLookupTable.TryGetValue(nodeChangedEvent.Id, out var changedNode)) return;

            changedNode.Change(nodeChangedEvent.Size, nodeChangedEvent.Start, nodeChangedEvent.End);
        }

        private void CreateNode(NodeCreatedEvent nodeCreatedEvent)
        {
            if (nodeCreatedEvent.ParentId.Equals(FileSystemEntryId.Empty))
            {
                var node = new FileSystemNodeViewModel(_navigateToCommand, nodeCreatedEvent.Id, null,
                    nodeCreatedEvent.Name);
                _nodeLookupTable.Add(node.Id, node);
                _root.Add(node);
            }
            else
            {
                if (!_nodeLookupTable.TryGetValue(nodeCreatedEvent.ParentId, out var parentNode)) return;

                var name = nodeCreatedEvent.Id.IsHiddenEntriesNode ? "other files" : nodeCreatedEvent.Name;
                var node = new FileSystemNodeViewModel(_navigateToCommand, nodeCreatedEvent.Id, parentNode, name);
                _nodeLookupTable.Add(node.Id, node);
                parentNode.Nodes.Add(node);
            }
        }
    }
}