using DokiFS.Backends.Physical;
using DokiFS.Interfaces;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendCreateFileTests : IDisposable
{
    readonly PhysicalBackendTestUtilities util;

    public PhysicalFileSystemBackendCreateFileTests()
    {
        util = new("CreateFile");
    }

    public void Dispose()
    {
        util.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "CreateFile: Basic Create File")]
    public void ShouldCreateNewFileWithZeroSize()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string file = "testfile.txt";
        string path = $"/{file}";
        backend.CreateFile(path);
        IVfsEntry entry = backend.GetInfo(path);

        Assert.True(backend.Exists(path));
        Assert.NotNull(entry);
        Assert.Equal(file, entry.FileName);
        Assert.Equal(path, entry.FullPath);
        Assert.Equal(VfsEntryType.File, entry.EntryType);
        Assert.Equal(0, entry.Size);
        // Check LastWriteTime is recent (within a reasonable margin)
        Assert.True((DateTime.UtcNow - entry.LastWriteTime).TotalSeconds < 5);
        Assert.Equal(typeof(PhysicalFileSystemBackend), entry.FromBackend);
    }

    [Fact(DisplayName = "CreateFile: Create File with Size")]
    public void CreateFileWithSpecifiedSizeFileExistsAndHasCorrectSize()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);
        string filePath = "/largeFile.bin";
        long expectedSize = 1024 * 1024; // 1 MB

        backend.CreateFile(filePath, expectedSize);

        Assert.True(backend.Exists(filePath));
        IVfsEntry fileEntry = backend.GetInfo(filePath);
        Assert.NotNull(fileEntry);
        Assert.Equal(expectedSize, fileEntry.Size);
    }
}
