using DokiFS.Backends.Memory;
using DokiFS.Interfaces;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendCreateFileTests
{
    [Fact(DisplayName = "CreateFile: Basic Create File")]
    public void ShouldCreateNewFileWithZeroSize()
    {
        MemoryFileSystemBackend backend = new();

        VPath path = $"/testfile.txt";
        backend.CreateFile(path);
        IVfsEntry entry = backend.GetInfo(path);

        Assert.True(backend.Exists(path));
        Assert.NotNull(entry);
        Assert.Equal(path.GetLeaf(), entry.FileName);
        Assert.Equal(path, entry.FullPath);
        Assert.Equal(VfsEntryType.File, entry.EntryType);
        Assert.Equal(0, entry.Size);
        // Check LastWriteTime is recent (within a reasonable margin)
        Assert.True((DateTime.UtcNow - entry.LastWriteTime).TotalSeconds < 5);
        Assert.Equal(typeof(MemoryFileSystemBackend), entry.FromBackend);
    }

    [Fact(DisplayName = "CreateFile: Create File with Size")]
    public void CreateFileWithSpecifiedSizeFileExistsAndHasCorrectSize()
    {
        MemoryFileSystemBackend backend = new();
        VPath filePath = $"/largeFile.bin";
        long expectedSize = 1024 * 1024; // 1 MB

        backend.CreateFile(filePath, expectedSize);

        Assert.True(backend.Exists(filePath));
        IVfsEntry fileEntry = backend.GetInfo(filePath);
        Assert.NotNull(fileEntry);
        Assert.Equal(expectedSize, fileEntry.Size);
    }
}
