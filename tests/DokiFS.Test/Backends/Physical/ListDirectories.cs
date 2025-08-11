using DokiFS.Backends.Physical;
using DokiFS.Interfaces;

namespace DokiFS.Tests.Backends.Physical;

public class PhysicalFileSystemBackendListDirectoriesTests : IDisposable
{
    readonly List<PhysicalBackendTestUtilities> utils = [];

    public void Dispose()
    {
        utils.ForEach(u => u.Dispose());
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "ListDirectories: Basic listing")]
    public void GetInfoBasicGetInfoForExistingFile()
    {
        PhysicalBackendTestUtilities util = new("ListDirectoriesBasic");
        utils.Add(util);
        PhysicalFileSystemBackend backend = new(util.BackendRoot);

        string dir = "testDir";
        string file = "fileInDir.txt";
        string filePath = $"{dir}/{file}";

        util.CreateTempDirectory(dir);
        util.CreateTempFile(filePath);

        IEnumerable<IVfsEntry> rootInfo = backend.ListDirectory("/");
        IEnumerable<IVfsEntry> subInfo = backend.ListDirectory("/testDir");

        Assert.Single(rootInfo);
        Assert.Single(subInfo);

        // Check folder
        Assert.Equal(dir, rootInfo.First().FileName);
        Assert.Equal($"/{dir}", rootInfo.First().FullPath);
        Assert.Equal(VfsEntryType.Directory, rootInfo.First().EntryType);
        Assert.Equal(typeof(PhysicalFileSystemBackend), rootInfo.First().FromBackend);


        // Check file
        Assert.Equal(file, subInfo.First().FileName);
        Assert.Equal($"/{dir}/{file}", subInfo.First().FullPath);
        Assert.Equal(VfsEntryType.File, subInfo.First().EntryType);
        Assert.Equal(0, subInfo.First().Size);
        Assert.Equal($"/{dir}/{file}", subInfo.First().FullPath);
        Assert.Equal(typeof(PhysicalFileSystemBackend), subInfo.First().FromBackend);
    }
}
