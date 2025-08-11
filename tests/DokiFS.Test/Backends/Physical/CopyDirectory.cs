using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendCopyDirectoryTests : IDisposable
{
    readonly List<PhysicalBackendTestUtilities> utils = [];

    public void Dispose()
    {
        utils.ForEach(u => u.Dispose());
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "CopyDirectory: Should copy directory and contents")]
    public void ShouldCopyDirectoryAndContents()
    {
        PhysicalBackendTestUtilities util = new(nameof(ShouldCopyDirectoryAndContents));
        utils.Add(util);
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string source = "moveSource";
        VPath sourcePath = $"/{source}";

        string dirFile = $"{source}/file1.txt";

        string destination = "moveDest";
        VPath destPath = $"/{destination}";
        string dirFileDest = $"{destination}/file1.txt";


        util.CreateTempDirectory(source);
        util.CreateTempFile(dirFile);

        Assert.True(util.DirExists(source));
        Assert.False(util.DirExists(destination));

        backend.CopyDirectory(source, destination);

        Assert.True(util.DirExists(source));
        Assert.True(util.FileExists(dirFileDest));
        Assert.True(util.DirExists(destination));
    }
}
