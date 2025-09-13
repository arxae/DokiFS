using DokiFS.Backends.Archive.Zip;

namespace DokiFS.Tests.Backends.Archive.Zip;

public class ZipArchiveBackendDeleteFileTests : IDisposable
{
    readonly IoTestUtilities util;
    readonly string archivePath;

    public ZipArchiveBackendDeleteFileTests()
    {
        util = new(nameof(ZipArchiveBackendDeleteFileTests));

        archivePath = Path.Combine(util.BackendRoot, $"{nameof(ZipArchiveBackendDeleteFileTests)}.zip");
        ResourceReader.WriteResourceToFile("DokiFS.Test.Backends.Archive.Zip.Assets.ZipArchiveTest.zip", archivePath);
        string archiveCopyPath = Path.Combine(util.BackendRoot, $"{nameof(ZipArchiveBackendDeleteFileTests)}_copy.zip");
        File.Copy(archivePath, archiveCopyPath);
        archivePath = archiveCopyPath;

    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => util.Dispose();

    [Fact(DisplayName = "DeleteFile: Basic delete file")]
    public void DeleteFileBasicDeleteFile()
    {
        VPath fileToDelete = "/toplevelfile.txt";

        ZipArchiveFileSystemBackend backend = new(archivePath, System.IO.Compression.ZipArchiveMode.Update, true);

        Assert.True(File.Exists(archivePath));

        backend.DeleteFile(fileToDelete);

        Assert.False(backend.Exists(fileToDelete));
    }

    [Fact(DisplayName = "DeleteFile: Readonly mode throws")]
    public void DeleteFileReadonlyModeThrows()
    {
        VPath fileToDelete = "/toplevelfile.txt";

        ZipArchiveFileSystemBackend backend = new(archivePath);

        Assert.Throws<NotSupportedException>(() => backend.DeleteFile(fileToDelete));
        Assert.True(File.Exists(archivePath));

    }

    [Fact(DisplayName = "DeleteFile: File not found throws")]
    public void DeleteFileFileNotFoundThrows()
    {
        VPath fileToDelete = "/nonexistentfile.txt";

        ZipArchiveFileSystemBackend backend = new(archivePath, System.IO.Compression.ZipArchiveMode.Update, true);

        Assert.True(File.Exists(archivePath));

        Assert.Throws<FileNotFoundException>(() => backend.DeleteFile(fileToDelete));

        Assert.False(backend.Exists(fileToDelete));
    }
}
