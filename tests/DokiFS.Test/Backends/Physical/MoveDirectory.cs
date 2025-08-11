using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendMoveDirectoryTests : IDisposable
{
    readonly List<PhysicalBackendTestUtilities> utils = [];

    public void Dispose()
    {
        utils.ForEach(u => u.Dispose());
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "MoveDirectory: Should move directory")]
    public void ShouldMoveDirectory()
    {
        PhysicalBackendTestUtilities util = new(nameof(ShouldMoveDirectory));
        utils.Add(util);
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string source = "moveSource";
        VPath soucePath = $"/{source}";

        string destination = "moveDest";
        VPath destPath = $"/{destination}";

        util.CreateTempDirectory(source);

        Assert.True(util.DirExists(source));
        Assert.False(util.DirExists(destination));

        backend.MoveDirectory(source, destination);

        Assert.False(util.DirExists(source));
        Assert.True(util.DirExists(destination));
    }
}
