using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Eurydice.Core.Common;

namespace Eurydice.Core.Model
{
    /// <summary>
    ///     Directory entry model.
    /// </summary>
    public sealed class DirectoryEntry : FileSystemModelEntry
    {
        private readonly LinkedList<FileSystemModelEntry> _entries = new LinkedList<FileSystemModelEntry>();
        private readonly FileSystemEntryId _hiddenEntriesNodeId;
        private bool _hiddenVisible;
        private long _lastKnownHiddenEntriesSize;
        private long _lastKnownVisibleEntriesSize;

        private LinkedListNode<FileSystemModelEntry> _lastVisibleNode;
        private int _visibleEntriesCount;

        public DirectoryEntry(FileSystemModel fileSystemModel, FileSystemEntryId id, DirectoryEntry parent,
            string name)
            : base(fileSystemModel, id, parent, name, 0)
        {
            _hiddenEntriesNodeId = new FileSystemEntryId(id.Path, true);
        }

        internal void UpdateEntriesVisibility()
        {
            if (!ShowEntriesIfNeeded())
                HideEntriesIfNeeded();
        }

        internal IEnumerable<FileSystemModelEntry> EnumerateVisible()
        {
            var current = _entries.First;
            while (current != null && current.Value.Visible)
            {
                yield return current.Value;
                current = current.Next;
            }
        }

        internal IEnumerable<FileSystemModelEntry> EnumerateAll()
        {
            return _entries;
        }

        internal void PrepopulateWithSortedEntries(IEnumerable<FileSystemModelEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (_entries.Count != 0) throw new InvalidOperationException();

            long size = 0;
            foreach (var entry in entries)
            {
                size += entry.Size;
                _entries.AddLast(entry);
            }

            UpdateSize(size);
            UpdateEntriesVisibility();
        }

        internal void Add(FileSystemModelEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            UpdateSize(entry.Size);
            DoAdd(entry);
            entry.NotifyVisibilityChanged();
        }

        internal void Delete(FileSystemModelEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var node = _entries.Find(entry);

            DoDelete(node);
            UpdateSize(-entry.Size);
            entry.NotifyVisibilityChanged();
        }

        internal void UpdateSize(long difference)
        {
            if (difference == 0) return;

            Size += difference;

            var stack = new Stack<DirectoryEntry>();
            var current = this;

            while (current.Parent != null)
            {
                stack.Push(current);
                current.Parent.Size += difference;
                current = current.Parent;
            }

            while (stack.Count > 0)
            {
                var entry = stack.Pop();
                entry.Parent.UpdateEntryPosition(entry);
                entry.Parent.UpdateEntriesVisibility();
            }
        }

        internal void UpdateEntryPosition(FileSystemModelEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var node = _entries.Find(entry);

            node = Swim(node);
            Sink(node);
        }

        internal void Update()
        {
            UpdateEntriesVisibility();
            UpdateEntries();
        }

        private static LinkedListNode<FileSystemModelEntry> Sink(LinkedListNode<FileSystemModelEntry> node)
        {
            while (node.Next != null && node.Next.Value.Size > node.Value.Size)
            {
                if (node.Value.Visible != node.Next.Value.Visible)
                {
                    node.Value.Visible = !node.Value.Visible;
                    node.Next.Value.Visible = !node.Next.Value.Visible;

                    node.Value.NotifyVisibilityChanged();
                    node.Next.Value.NotifyVisibilityChanged();
                }

                var temp = node.Next.Value;
                node.Next.Value = node.Value;
                node.Value = temp;
                node = node.Next;
            }

            return node;
        }

        private static LinkedListNode<FileSystemModelEntry> Swim(LinkedListNode<FileSystemModelEntry> node)
        {
            while (node.Previous != null && node.Previous.Value.Size < node.Value.Size)
            {
                if (node.Value.Visible != node.Previous.Value.Visible)
                {
                    node.Value.Visible = !node.Value.Visible;
                    node.Previous.Value.Visible = !node.Previous.Value.Visible;

                    node.Value.NotifyVisibilityChanged();
                    node.Previous.Value.NotifyVisibilityChanged();
                }

                var temp = node.Previous.Value;
                node.Previous.Value = node.Value;
                node.Value = temp;
                node = node.Previous;
            }

            return node;
        }

        private void UpdateEntries()
        {
            double start = 0;
            double end = 0;
            long visibleSize = 0;
            foreach (var entry in EnumerateVisible())
            {
                visibleSize += entry.Size;
                double weight = 0;
                if (Size != 0) weight = (double) entry.Size / Size;
                end += weight;
                entry.Start = start;
                entry.End = end;
                entry.NotifyNodeChanged();
                start = end;
            }

            UpdateHiddenEntriesNode(visibleSize, start);
        }

        private void UpdateHiddenEntriesNode(long visibleSize, double start)
        {
            var newHiddenSize = Size - visibleSize;
            if (newHiddenSize == Size) newHiddenSize = 0;

            if (_lastKnownVisibleEntriesSize == visibleSize &&
                _lastKnownHiddenEntriesSize == newHiddenSize)
                return;

            if (_hiddenVisible)
            {
                if (newHiddenSize == 0)
                {
                    _hiddenVisible = false;
                    Model.RaiseNodeDeleted(_hiddenEntriesNodeId);
                }
                else
                {
                    Model.RaiseNodeChanged(_hiddenEntriesNodeId, newHiddenSize, start, 1);
                }
            }
            else
            {
                if (newHiddenSize != 0)
                {
                    _hiddenVisible = true;
                    Model.RaiseNodeCreated(_hiddenEntriesNodeId, Id, string.Empty);
                    Model.RaiseNodeChanged(_hiddenEntriesNodeId, newHiddenSize, start, 1);
                }
            }

            _lastKnownVisibleEntriesSize = visibleSize;
            _lastKnownHiddenEntriesSize = newHiddenSize;
        }

        private bool HideEntriesIfNeeded()
        {
            var lastVisibleNodeChanged = false;

            while (_lastVisibleNode != null
                   && (_visibleEntriesCount > GetMaxVisibleNodes()
                       || !IsShouldBeVisible(_lastVisibleNode.Value)))
            {
                _lastVisibleNode.Value.Visible = false;
                _lastVisibleNode.Value.NotifyVisibilityChanged();
                _lastVisibleNode = _lastVisibleNode.Previous;
                _visibleEntriesCount--;
                lastVisibleNodeChanged = true;

                Debug.Assert(_visibleEntriesCount >= 0);
            }

            return lastVisibleNodeChanged;
        }

        private bool ShowEntriesIfNeeded()
        {
            var lastVisibleNodeChanged = false;

            if (_lastVisibleNode == null)
            {
                if (_entries.Count > 0 && IsShouldBeVisible(_entries.First.Value)
                                       && _visibleEntriesCount < GetMaxVisibleNodes())
                {
                    _lastVisibleNode = _entries.First;
                    _visibleEntriesCount++;
                    _lastVisibleNode.Value.Visible = true;
                    _lastVisibleNode.Value.NotifyVisibilityChanged();
                    lastVisibleNodeChanged = true;
                }
                else
                {
                    return false;
                }
            }

            while (_visibleEntriesCount < GetMaxVisibleNodes() &&
                   _lastVisibleNode.Next != null && IsShouldBeVisible(_lastVisibleNode.Next.Value))
            {
                _lastVisibleNode = _lastVisibleNode.Next;
                _lastVisibleNode.Value.Visible = true;
                _lastVisibleNode.Value.NotifyVisibilityChanged();
                _visibleEntriesCount++;
                lastVisibleNodeChanged = true;
            }

            Debug.Assert(_visibleEntriesCount == _entries.Count(e => e.Visible));

            return lastVisibleNodeChanged;
        }

        private bool IsShouldBeVisible(FileSystemModelEntry entry)
        {
            return entry.Size != 0 && Size / Model.MaxVisibleEntries <= entry.Size;
        }

        private int GetMaxVisibleNodes()
        {
            return Visible
                ? Model.MaxVisibleEntries
                : 0;
        }

        private void DoAdd(FileSystemModelEntry entry)
        {
            Insert(entry);
            if (_lastVisibleNode != null && entry.Size >= _lastVisibleNode.Value.Size)
            {
                entry.Visible = true;
                _visibleEntriesCount++;
            }
        }

        private void DoDelete(LinkedListNode<FileSystemModelEntry> node)
        {
            if (node == null) return;

            if (_lastVisibleNode != null && _lastVisibleNode.Equals(node)) _lastVisibleNode = _lastVisibleNode.Previous;

            if (node.Value.Visible)
            {
                node.Value.Visible = false;
                _visibleEntriesCount--;

                Debug.Assert(_visibleEntriesCount >= 0);
            }

            _entries.Remove(node);
        }

        private LinkedListNode<FileSystemModelEntry> Insert(FileSystemModelEntry entry)
        {
            var current = _entries.First;

            if (current == null) return _entries.AddFirst(entry);

            while (current != null && current.Value.Size > entry.Size) current = current.Next;

            return current != null
                ? _entries.AddBefore(current, entry)
                : _entries.AddLast(entry);
        }
    }
}