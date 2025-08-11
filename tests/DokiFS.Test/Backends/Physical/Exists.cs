using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendExistsTests : IDisposable
{
    readonly PhysicalBackendTestUtilities util;

    public PhysicalFileSystemBackendExistsTests()
    {
        util = new("Exists");
    }

    public void Dispose()
    {
        util.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "Exists: Basic check")]
    public void ExistsBasicCheck()
    {
        PhysicalFileSystemBackend backed = new(util.BackendRoot);

        string rootFile = Path.Combine(util.BackendRoot, "testFile.txt");
        string subDir = Path.Combine(util.BackendRoot, "testDir");
        string subDirFile = Path.Combine(util.BackendRoot, $"{subDir}/testFile.txt");

        util.CreateTempFile(rootFile);
        util.CreateTempDirectory(subDir);
        util.CreateTempFile(subDirFile);

        Assert.True(backed.Exists("/testFile.txt"));
        Assert.True(backed.Exists("/testDir"));
        Assert.True(backed.Exists("/testDir/testFile.txt"));
    }

    [Fact(DisplayName = "Exists: Non existing")]
    public void ExistsNonExisting()
    {
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        Assert.False(backend.Exists("/nonexisting.txt"));
    }
}
