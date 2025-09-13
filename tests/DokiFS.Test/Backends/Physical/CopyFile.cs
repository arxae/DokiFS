using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendCopyFileTests : IDisposable
{
    readonly List<IoTestUtilities> utils = [];

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => utils.ForEach(u => u.Dispose());

    [Fact(DisplayName = "CopyFile: Copy File")]
    public void CopyFile()
    {
        IoTestUtilities util = new(nameof(CopyFile));
        utils.Add(util);

        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string source = "source.txt";
        VPath sourcePath = $"/{source}";

        string dest = "dest.txt";
        VPath destPath = $"/{dest}";

        util.CreateTempFile(source);

        Assert.True(util.FileExists(source));

        backend.CopyFile(sourcePath, destPath);

        Assert.True(util.FileExists(source));
        Assert.True(util.FileExists(dest));
    }

    [Fact(DisplayName = "CopyFile: Source does not exist")]
    public void CopyFileSourceNotExist()
    {
        IoTestUtilities util = new(nameof(CopyFileSourceNotExist));
        utils.Add(util);

        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string source = "notexist.txt";
        VPath sourcePath = $"/{source}";

        string dest = "dest.txt";
        VPath destPath = $"/{dest}";

        Assert.False(util.FileExists(source));

        Assert.Throws<FileNotFoundException>(() => backend.MoveFile(sourcePath, destPath));
    }

    [Fact(DisplayName = "CopyFile: Source exists and is directory")]
    public void CopyFileSourceIsDirectory()
    {
        IoTestUtilities util = new(nameof(CopyFileSourceIsDirectory));
        utils.Add(util);

        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string source = "folder";
        VPath sourcePath = $"/{source}";

        string dest = "dest.txt";
        VPath destPath = $"/{dest}";

        util.CreateTempDirectory(source);

        Assert.True(util.DirExists(source));

        Assert.Throws<IOException>(() => backend.MoveFile(sourcePath, destPath));
    }

    [Fact(DisplayName = "CopyFile: Destination exists and is directory")]
    public void CopyFileDestinationIsDirectory()
    {
        IoTestUtilities util = new(nameof(CopyFileDestinationIsDirectory));
        utils.Add(util);

        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string source = "source.txt";
        VPath sourcePath = $"/{source}";

        string dest = "dest";
        VPath destPath = $"/{dest}";

        util.CreateTempFile(source);
        util.CreateTempDirectory(dest);

        Assert.True(util.FileExists(source));
        Assert.True(util.DirExists(dest));

        Assert.Throws<IOException>(() => backend.MoveFile(sourcePath, destPath));
    }
}
