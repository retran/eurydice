using System.Collections.Generic;
using Eurydice.Core.Common;
using Eurydice.Core.Indexer;
using Eurydice.Core.Model;
using Xunit;

namespace Eurydice.Core.Tests
{
    public class FileSystemModelTests
    {
        public FileSystemModelTests()
        {
            _fileSystemModel = new FileSystemModel("ROOT", _rootId);

            _fileSystemModel.NodeCreated += (id, parentId, name) =>
            {
                _receivedEvents.Add($"CREATED {id}, {parentId}, {name}");
            };

            _fileSystemModel.NodeChanged += (id, size, start, end) =>
            {
                _receivedEvents.Add($"CHANGED {id}, {size}, {start}, {end}");
            };

            _fileSystemModel.NodeDeleted += id => { _receivedEvents.Add($"DELETED {id}"); };

            _fileSystemModel.NodeRenamed += (name, id, newId) =>
            {
                _receivedEvents.Add($"RENAMED {name}, {id}, {newId}");
            };

            _fileSystemModel.Initialize();
        }

        private readonly List<string> _receivedEvents = new List<string>();
        private readonly FileSystemEntryId _rootId = new FileSystemEntryId("ROOT");
        private readonly FileSystemModel _fileSystemModel;

        [Fact]
        public void Given_BigFile_When_AddSmallFile_Then_ShowHiddenNode()
        {
            _fileSystemModel.CreateFile(new FileSystemEntryId(@"ROOT\big.txt"), _rootId, "big.txt", 32768);
            _fileSystemModel.Update();
            _receivedEvents.Clear();

            _fileSystemModel.CreateFile(new FileSystemEntryId(@"ROOT\small.txt"), _rootId, "small.txt", 1024);
            _fileSystemModel.Update();

            Assert.Equal(
                "CHANGED ROOT, 33792, 0, 1\r\n" +
                "CHANGED ROOT\\big.txt, 32768, 0, 0.96969696969697\r\n" +
                "CREATED ROOT\\%%hidden%%, ROOT, \r\n" +
                "CHANGED ROOT\\%%hidden%%, 1024, 0.96969696969697, 1",
                string.Join("\r\n", _receivedEvents));
        }

        [Fact]
        public void Given_BigFileAndHiddenFile_When_MakeHiddenFileBig_Then_ShowUpdatedFile()
        {
            var id = new FileSystemEntryId(@"ROOT\small.txt");
            _fileSystemModel.CreateFile(id, _rootId, "small.txt", 1024);
            _fileSystemModel.CreateFile(new FileSystemEntryId(@"ROOT\big.txt"), _rootId, "big.txt", 32768);
            _fileSystemModel.Update();
            _receivedEvents.Clear();

            _fileSystemModel.ChangeFileSize(id, 32768);
            _fileSystemModel.Update();

            Assert.Equal(
                "CREATED ROOT\\small.txt, ROOT, small.txt\r\n" +
                "CHANGED ROOT, 65536, 0, 1\r\n" +
                "CHANGED ROOT\\big.txt, 32768, 0, 0.5\r\n" +
                "CHANGED ROOT\\small.txt, 32768, 0.5, 1\r\n" +
                "DELETED ROOT\\%%hidden%%",
                string.Join("\r\n", _receivedEvents));
        }

        [Fact]
        public void Given_BigFileAndSecondBigFile_When_MakeSecondFileSmal_Then_HideSecondFile()
        {
            var id = new FileSystemEntryId(@"ROOT\small.txt");
            _fileSystemModel.CreateFile(id, _rootId, "small.txt", 32768);
            _fileSystemModel.CreateFile(new FileSystemEntryId(@"ROOT\big.txt"), _rootId, "big.txt", 32768);
            _fileSystemModel.Update();
            _receivedEvents.Clear();

            _fileSystemModel.ChangeFileSize(id, 1024);
            _fileSystemModel.Update();

            Assert.Equal(
                "DELETED ROOT\\small.txt\r\n" +
                "CHANGED ROOT, 33792, 0, 1\r\n" +
                "CHANGED ROOT\\big.txt, 32768, 0, 0.96969696969697\r\n" +
                "CREATED ROOT\\%%hidden%%, ROOT, \r\n" +
                "CHANGED ROOT\\%%hidden%%, 1024, 0.96969696969697, 1",
                string.Join("\r\n", _receivedEvents));
        }

        [Fact]
        public void Given_DirectoryAndFile_When_DeleteDirectory_Then_HideDirectoryImmediately()
        {
            var folderId = new FileSystemEntryId(@"ROOT\folder");
            var fileId = new FileSystemEntryId(@"ROOT\folder\file.txt");
            _fileSystemModel.CreateDirectory(folderId, _rootId, "folder");
            _fileSystemModel.CreateFile(fileId, folderId, "file.txt", 1024);
            _fileSystemModel.Update();
            _receivedEvents.Clear();

            _fileSystemModel.DeleteEntry(folderId);
            _fileSystemModel.Update();

            Assert.Equal(
                "DELETED ROOT\\folder\r\n" +
                "CHANGED ROOT, 0, 0, 1",
                string.Join("\r\n", _receivedEvents));
        }

        [Fact]
        public void Given_DirectoryAndFile_When_DeleteFileThenDirectory_Then_HideFileAndDirectory()
        {
            var folderId = new FileSystemEntryId(@"ROOT\folder");
            var fileId = new FileSystemEntryId(@"ROOT\folder\file.txt");
            _fileSystemModel.CreateDirectory(folderId, _rootId, "folder");
            _fileSystemModel.CreateFile(fileId, folderId, "file.txt", 1024);
            _fileSystemModel.Update();
            _receivedEvents.Clear();

            _fileSystemModel.DeleteEntry(fileId);
            _fileSystemModel.DeleteEntry(folderId);
            _fileSystemModel.Update();

            Assert.Equal(
                "DELETED ROOT\\folder\r\n" +
                "DELETED ROOT\\folder\\file.txt\r\n" +
                "CHANGED ROOT, 0, 0, 1",
                string.Join("\r\n", _receivedEvents));
        }


        [Fact]
        public void
            Given_DirectoryWithBigFileAndSmallFile_When_RenameFilesAndDirectory_Then_ShowRenamesOfDirectoryAndBigFile()
        {
            var folderId = new FileSystemEntryId(@"ROOT\folder");
            var smallFileId = new FileSystemEntryId(@"ROOT\folder\small.txt");
            var bigFileId = new FileSystemEntryId(@"ROOT\folder\big.txt");
            _fileSystemModel.CreateDirectory(folderId, _rootId, "folder");
            _fileSystemModel.CreateFile(smallFileId, folderId, "small.txt", 1024);
            _fileSystemModel.CreateFile(bigFileId, folderId, "big.txt", 32768);
            _fileSystemModel.Update();
            _receivedEvents.Clear();

            _fileSystemModel.RenameFile("big2.txt", bigFileId, new FileSystemEntryId(@"ROOT\folder\big2.txt"));
            _fileSystemModel.RenameFile("small2.txt", smallFileId, new FileSystemEntryId(@"ROOT\folder\small2.txt"));
            _fileSystemModel.RenameDirectory("folder2", folderId, new FileSystemEntryId(@"ROOT\folder2"));
            _fileSystemModel.Update();

            Assert.Equal(
                "RENAMED big2.txt, ROOT\\folder\\big.txt, ROOT\\folder\\big2.txt\r\n" +
                "RENAMED folder2, ROOT\\folder, ROOT\\folder2",
                string.Join("\r\n", _receivedEvents));
        }

        [Fact]
        public void Given_EmptyModel_When_AddDirectoryAndFile_Then_ShowDirectoryFileAndSizeChanges()
        {
            var folderId = new FileSystemEntryId(@"ROOT\folder");

            _fileSystemModel.CreateDirectory(folderId, _rootId, "folder");
            _fileSystemModel.CreateFile(new FileSystemEntryId(@"ROOT\folder\file.txt"), folderId, "file.txt", 1024);
            _fileSystemModel.Update();

            Assert.Equal(
                "CREATED ROOT, , ROOT\r\n" +
                "CHANGED ROOT, 0, 0, 1\r\n" +
                "CREATED ROOT\\folder, ROOT, folder\r\n" +
                "CHANGED ROOT, 1024, 0, 1\r\n" +
                "CHANGED ROOT\\folder, 1024, 0, 1\r\n" +
                "CREATED ROOT\\folder\\file.txt, ROOT\\folder, file.txt\r\n" +
                "CHANGED ROOT\\folder\\file.txt, 1024, 0, 1",
                string.Join("\r\n", _receivedEvents));
        }

        [Fact]
        public void Given_EmptyModel_When_Prepopulate_Then_ShowValidTree()
        {
            var root = _fileSystemModel.LookupDirectory(_rootId);
            var tempFolderId = new FileSystemEntryId("ROOT\\temp");
            var photosFolderId = new FileSystemEntryId("ROOT\\photos");
            _fileSystemModel.PopulateWithIndexedEntries(root,
                new List<FileSystemModelEntry>
                {
                    new DirectoryEntry(_fileSystemModel, tempFolderId, root, "temp"),
                    new DirectoryEntry(_fileSystemModel, photosFolderId, root, "photos")
                });
            var tempFolder = _fileSystemModel.LookupDirectory(tempFolderId);
            var photosFolder = _fileSystemModel.LookupDirectory(photosFolderId);
            _fileSystemModel.PopulateWithIndexedEntries(tempFolder,
                new List<FileSystemModelEntry>
                {
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\temp\\swap.swp"), tempFolder,
                        "swap.swp", 64 * 1024),
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\temp\\temp.tmp"), tempFolder,
                        "temp.tmp", 1024)
                });
            _fileSystemModel.PopulateWithIndexedEntries(photosFolder,
                new List<FileSystemModelEntry>
                {
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\photos\\1.jpg"), photosFolder, "1.jpg",
                        2 * 1024),
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\photos\\2.jpg"), photosFolder, "2.jpg",
                        3 * 1024),
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\photos\\3.jpg"), photosFolder, "3.jpg",
                        1 * 1024),
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\photos\\4.jpg"), photosFolder, "4.jpg",
                        2 * 1024),
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\photos\\5.jpg"), photosFolder, "5.jpg",
                        1 * 1024)
                });
            _fileSystemModel.Update();

            Assert.Equal(
                "CREATED ROOT, , ROOT\r\n" +
                "CHANGED ROOT, 0, 0, 1\r\n" +
                "CREATED ROOT\\temp, ROOT, temp\r\n" +
                "CREATED ROOT\\temp\\swap.swp, ROOT\\temp, swap.swp\r\n" +
                "CREATED ROOT\\photos, ROOT, photos\r\n" +
                "CREATED ROOT\\photos\\2.jpg, ROOT\\photos, 2.jpg\r\n" +
                "CREATED ROOT\\photos\\1.jpg, ROOT\\photos, 1.jpg\r\n" +
                "CREATED ROOT\\photos\\4.jpg, ROOT\\photos, 4.jpg\r\n" +
                "CREATED ROOT\\photos\\3.jpg, ROOT\\photos, 3.jpg\r\n" +
                "CREATED ROOT\\photos\\5.jpg, ROOT\\photos, 5.jpg\r\n" +
                "CHANGED ROOT, 75776, 0, 1\r\n" +
                "CHANGED ROOT\\temp, 66560, 0, 0.878378378378378\r\n" +
                "CHANGED ROOT\\photos, 9216, 0.878378378378378, 1\r\n" +
                "CHANGED ROOT\\temp\\swap.swp, 65536, 0, 0.984615384615385\r\n" +
                "CREATED ROOT\\temp\\%%hidden%%, ROOT\\temp, \r\n" +
                "CHANGED ROOT\\temp\\%%hidden%%, 1024, 0.984615384615385, 1\r\n" +
                "CHANGED ROOT\\photos\\2.jpg, 3072, 0, 0.333333333333333\r\n" +
                "CHANGED ROOT\\photos\\1.jpg, 2048, 0.333333333333333, 0.555555555555556\r\n" +
                "CHANGED ROOT\\photos\\4.jpg, 2048, 0.555555555555556, 0.777777777777778\r\n" +
                "CHANGED ROOT\\photos\\3.jpg, 1024, 0.777777777777778, 0.888888888888889\r\n" +
                "CHANGED ROOT\\photos\\5.jpg, 1024, 0.888888888888889, 1",
                string.Join("\r\n", _receivedEvents));
        }

        [Fact]
        public void Given_PopulatedModel_When_UpdateWithReindexedEntries_Then_ShowValidTree()
        {
            var root = _fileSystemModel.LookupDirectory(_rootId);
            var tempFolderId = new FileSystemEntryId("ROOT\\temp");
            var photosFolderId = new FileSystemEntryId("ROOT\\photos");
            _fileSystemModel.PopulateWithIndexedEntries(root,
                new List<FileSystemModelEntry>
                {
                    new DirectoryEntry(_fileSystemModel, tempFolderId, root, "temp"),
                    new DirectoryEntry(_fileSystemModel, photosFolderId, root, "photos")
                });
            var tempFolder = _fileSystemModel.LookupDirectory(tempFolderId);
            var photosFolder = _fileSystemModel.LookupDirectory(photosFolderId);
            _fileSystemModel.PopulateWithIndexedEntries(tempFolder,
                new List<FileSystemModelEntry>
                {
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\temp\\swap.swp"), tempFolder,
                        "swap.swp", 64 * 1024),
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\temp\\temp.tmp"), tempFolder,
                        "temp.tmp", 1024)
                });
            _fileSystemModel.PopulateWithIndexedEntries(photosFolder,
                new List<FileSystemModelEntry>
                {
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\photos\\1.jpg"), photosFolder, "1.jpg",
                        2 * 1024),
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\photos\\2.jpg"), photosFolder, "2.jpg",
                        3 * 1024),
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\photos\\3.jpg"), photosFolder, "3.jpg",
                        1 * 1024),
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\photos\\4.jpg"), photosFolder, "4.jpg",
                        2 * 1024),
                    new FileEntry(_fileSystemModel, new FileSystemEntryId("ROOT\\photos\\5.jpg"), photosFolder, "5.jpg",
                        1 * 1024)
                });
            _fileSystemModel.Update();
            _receivedEvents.Clear();

            _fileSystemModel.UpdateWithIndexedEntries(photosFolder,
                new List<FileSystemEntryInfo>
                {
                    new FileEntryInfo("1.jpg", new FileSystemEntryId("ROOT\\photos\\1.jpg"), photosFolderId, 2 * 1024),
                    new FileEntryInfo("4.jpg", new FileSystemEntryId("ROOT\\photos\\4.jpg"), photosFolderId, 3 * 1024),
                    new FileEntryInfo("6.jpg", new FileSystemEntryId("ROOT\\photos\\6.jpg"), photosFolderId, 2 * 1024),
                    new FileEntryInfo("7.jpg", new FileSystemEntryId("ROOT\\photos\\7.jpg"), photosFolderId, 5 * 1024),
                    new FileEntryInfo("8.jpg", new FileSystemEntryId("ROOT\\photos\\8.jpg"), photosFolderId, 4 * 1024)
                });
            _fileSystemModel.Update();

            Assert.Equal(
                "CREATED ROOT\\photos\\6.jpg, ROOT\\photos, 6.jpg\r\n" +
                "CREATED ROOT\\photos\\7.jpg, ROOT\\photos, 7.jpg\r\n" +
                "CREATED ROOT\\photos\\8.jpg, ROOT\\photos, 8.jpg\r\n" +
                "DELETED ROOT\\photos\\2.jpg\r\n" +
                "DELETED ROOT\\photos\\3.jpg\r\n" +
                "DELETED ROOT\\photos\\5.jpg\r\n" +
                "CHANGED ROOT, 82944, 0, 1\r\n" +
                "CHANGED ROOT\\temp, 66560, 0, 0.802469135802469\r\n" +
                "CHANGED ROOT\\photos, 16384, 0.802469135802469, 1\r\n" +
                "CHANGED ROOT\\photos\\7.jpg, 5120, 0, 0.3125\r\n" +
                "CHANGED ROOT\\photos\\8.jpg, 4096, 0.3125, 0.5625\r\n" +
                "CHANGED ROOT\\photos\\4.jpg, 3072, 0.5625, 0.75\r\n" +
                "CHANGED ROOT\\photos\\6.jpg, 2048, 0.75, 0.875\r\n" +
                "CHANGED ROOT\\photos\\1.jpg, 2048, 0.875, 1",
                string.Join("\r\n", _receivedEvents));
        }

        [Fact]
        public void Given_SmallFile_When_AddBigFile_Then_HideSmallFileAndShowBigFileAndHiddenNode()
        {
            _fileSystemModel.CreateFile(new FileSystemEntryId(@"ROOT\small.txt"), _rootId, "small.txt", 1024);
            _fileSystemModel.Update();
            _receivedEvents.Clear();

            _fileSystemModel.CreateFile(new FileSystemEntryId(@"ROOT\big.txt"), _rootId, "big.txt", 32768);
            _fileSystemModel.Update();

            Assert.Equal(
                "CREATED ROOT\\big.txt, ROOT, big.txt\r\n" +
                "CHANGED ROOT, 33792, 0, 1\r\n" +
                "DELETED ROOT\\small.txt\r\n" +
                "CHANGED ROOT\\big.txt, 32768, 0, 0.96969696969697\r\n" +
                "CREATED ROOT\\%%hidden%%, ROOT, \r\n" +
                "CHANGED ROOT\\%%hidden%%, 1024, 0.96969696969697, 1",
                string.Join("\r\n", _receivedEvents));
        }

        [Fact]
        public void Given_SmallFile_When_UpdateFileSize_Then_ShowSizeChanges()
        {
            var id = new FileSystemEntryId(@"ROOT\small.txt");
            _fileSystemModel.CreateFile(id, _rootId, "small.txt", 1024);
            _fileSystemModel.Update();
            _receivedEvents.Clear();

            _fileSystemModel.ChangeFileSize(id, 2048);
            _fileSystemModel.Update();

            Assert.Equal(
                "CHANGED ROOT, 2048, 0, 1\r\n" +
                "CHANGED ROOT\\small.txt, 2048, 0, 1",
                string.Join("\r\n", _receivedEvents));
        }
    }
}