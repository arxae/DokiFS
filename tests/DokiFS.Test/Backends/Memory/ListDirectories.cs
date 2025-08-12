using DokiFS.Backends.Memory;
using DokiFS.Interfaces;

namespace DokiFS.Tests.Backends.Memory;

public class MemoryFileSystemBackendListDirectoriesTests
{
    [Fact(DisplayName = "ListDirectories: Basic listing")]
    public void GetInfoBasicGetInfoForExistingFile()
    {
        MemoryFileSystemBackend backend = new();

        string dirName = "testDir";
        string fileName = "fileInDir.txt";

        VPath filePath = $"/{dirName}/{fileName}";
        VPath dirPath = $"/{dirName}";

        backend.CreateDirectory(dirPath);
        backend.CreateFile(filePath);

        IEnumerable<IVfsEntry> rootInfo = backend.ListDirectory("/");
        IEnumerable<IVfsEntry> subInfo = backend.ListDirectory("/testDir");

        Assert.Single(rootInfo);
        Assert.Single(subInfo);

        // Check folder
        Assert.Equal(dirName, rootInfo.First().FileName);
        Assert.Equal($"/{dirName}", rootInfo.First().FullPath);
        Assert.Equal(VfsEntryType.Directory, rootInfo.First().EntryType);
        Assert.Equal(typeof(MemoryFileSystemBackend), rootInfo.First().FromBackend);


        // Check file
        Assert.Equal(fileName, subInfo.First().FileName);
        Assert.Equal($"/{dirName}/{fileName}", subInfo.First().FullPath);
        Assert.Equal(VfsEntryType.File, subInfo.First().EntryType);
        Assert.Equal(0, subInfo.First().Size);
        Assert.Equal($"/{dirName}/{fileName}", subInfo.First().FullPath);
        Assert.Equal(typeof(MemoryFileSystemBackend), subInfo.First().FromBackend);
    }
}
