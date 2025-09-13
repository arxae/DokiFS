using DokiFS.Backends.Archive.Zip;
using DokiFS.Interfaces;

namespace DokiFS.Tests.Backends.Archive.Zip;

public class ZipArchiveBackendCreateFileTests : IDisposable
{
    readonly IoTestUtilities util;
    readonly string archivePath;

    public ZipArchiveBackendCreateFileTests()
    {
        util = new(nameof(ZipArchiveBackendCreateFileTests));

        archivePath = Path.Combine(util.BackendRoot, $"{nameof(ZipArchiveBackendCreateFileTests)}.zip");
        ResourceReader.WriteResourceToFile("DokiFS.Test.Backends.Archive.Zip.Assets.ZipArchiveTest.zip", archivePath);
        string archiveCopyPath = Path.Combine(util.BackendRoot, $"{nameof(ZipArchiveBackendCreateFileTests)}_copy.zip");
        File.Copy(archivePath, archiveCopyPath);
        archivePath = archiveCopyPath;

    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => util.Dispose();

    [Fact(DisplayName = "CreateFile: Basic create file")]
    public void CreateFileBasicCreateFile()
    {
        VPath newFile = $"/{nameof(ZipArchiveBackendCreateFileTests)}_{nameof(CreateFileBasicCreateFile)}.txt";

        ZipArchiveFileSystemBackend backend = new(archivePath, System.IO.Compression.ZipArchiveMode.Update, true);
        long size = 1024;
        backend.CreateFile(newFile, size);

        Assert.True(File.Exists(archivePath));
        Assert.Equal(size, backend.GetInfo(newFile).Size);
    }

    [Fact(DisplayName = "CreateFile: Basic create file with read only mode")]
    public void CreateFileBasicCreateFileReadOnlyMode()
    {
        VPath newFile = $"/{nameof(ZipArchiveBackendCreateFileTests)}_{nameof(CreateFileBasicCreateFileReadOnlyMode)}.txt";

        ZipArchiveFileSystemBackend backend = new(archivePath);

        Assert.Throws<NotSupportedException>(() => backend.CreateFile(newFile));
    }

    [Fact(DisplayName = "CreateFile: Create file when file already exists")]
    public void CreateFileWhenFileAlreadyExists()
    {
        VPath newFile = "/toplevelfile.txt";

        ZipArchiveFileSystemBackend backend = new(archivePath, System.IO.Compression.ZipArchiveMode.Update, true);

        Assert.Throws<IOException>(() => backend.CreateFile(newFile));
    }
}
