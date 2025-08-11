using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendTryGetPhysicalPathTests : IDisposable
{
    readonly PhysicalBackendTestUtilities util;

    public PhysicalFileSystemBackendTryGetPhysicalPathTests()
    {
        util = new(nameof(PhysicalFileSystemBackendTryGetPhysicalPathTests));
    }

    public void Dispose()
    {
        util.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "TryGetPhysicalPath: Valid path")]
    public void TryGetPhysicalPathValidPathReturnsTrue()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string file = "test.txt";
        VPath path = $"/{file}";

        util.CreateTempFile(file);

        bool success = backend.TryGetPhysicalPath(path, out string physicalPath);

        Assert.True(success);
        Assert.NotNull(physicalPath);
        Assert.Equal(util.GetFullPath(file), physicalPath);
    }

    [Fact(DisplayName = "TryGetPhysicalPath: Invalid path")]
    public void TryGetPhysicalPathInvalidPathReturnsFalse()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        bool success = backend.TryGetPhysicalPath("/invalidPath", out string physicalPath);

        Assert.False(success);
        Assert.Null(physicalPath);
    }
}
