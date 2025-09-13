using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendMoveDirectoryTests : IDisposable
{
    readonly List<IoTestUtilities> utils = [];

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => utils.ForEach(u => u.Dispose());

    [Fact(DisplayName = "MoveDirectory: Should move directory")]
    public void ShouldMoveDirectory()
    {
        IoTestUtilities util = new(nameof(ShouldMoveDirectory));
        utils.Add(util);
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string source = "moveSource";
        VPath sourcePath = $"/{source}";

        string destination = "moveDest";
        VPath destPath = $"/{destination}";

        util.CreateTempDirectory(source);

        Assert.True(util.DirExists(source));
        Assert.False(util.DirExists(destination));

        backend.MoveDirectory(sourcePath, destPath);

        Assert.False(util.DirExists(source));
        Assert.True(util.DirExists(destination));
    }
}
