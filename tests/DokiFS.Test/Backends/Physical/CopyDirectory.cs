using DokiFS.Backends.Physical;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendCopyDirectoryTests : IDisposable
{
    readonly List<IoTestUtilities> utils = [];

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) => utils.ForEach(u => u.Dispose());

    [Fact(DisplayName = "CopyDirectory: Should copy directory and contents")]
    public void ShouldCopyDirectoryAndContents()
    {
        IoTestUtilities util = new(nameof(ShouldCopyDirectoryAndContents));
        utils.Add(util);
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string source = "moveSource";

        string dirFile = $"{source}/file1.txt";

        string destination = "moveDest";
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
