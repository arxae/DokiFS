using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendMoveFileTests : IDisposable
{
    readonly List<IoTestUtilities> utils = [];

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => utils.ForEach(u => u.Dispose());

    [Fact(DisplayName = "MoveFile: Basic Move File")]
    public void ShouldMoveFile()
    {
        IoTestUtilities util = new(nameof(ShouldMoveFile));
        utils.Add(util);

        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string source = "source.txt";
        VPath sourcePath = $"/{source}";

        string dest = "dest.txt";
        VPath destPath = $"/{dest}";

        util.CreateTempFile(source);

        Assert.True(util.FileExists(source));

        backend.MoveFile(sourcePath, destPath);

        Assert.False(util.FileExists(source));
        Assert.True(util.FileExists(dest));
    }

    [Fact(DisplayName = "MoveFile: Source does not exist")]
    public void MoveFileSourceNotExist()
    {
        IoTestUtilities util = new(nameof(MoveFileSourceNotExist));
        utils.Add(util);

        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string source = "notexist.txt";
        VPath sourcePath = $"/{source}";

        string dest = "dest.txt";
        VPath destPath = $"/{dest}";

        Assert.False(util.FileExists(source));

        Assert.Throws<FileNotFoundException>(() => backend.MoveFile(sourcePath, destPath));
    }

    [Fact(DisplayName = "MoveFile: Source exists and is directory")]
    public void MoveFileSourceIsDirectory()
    {
        IoTestUtilities util = new(nameof(MoveFileSourceIsDirectory));
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

    [Fact(DisplayName = "MoveFile: Destination exists and is directory")]
    public void MoveFileDestinationIsDirectory()
    {
        IoTestUtilities util = new(nameof(MoveFileDestinationIsDirectory));
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
