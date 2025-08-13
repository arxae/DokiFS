using DokiFS.Interfaces;
using FakeItEasy;

namespace DokiFS.Tests.VirtualFileSystems.Default;

public class DefaultVfTryGetMountedBackendTests
{
    [Fact(DisplayName = "TryGetMountedBackend: Returns correctly")]
    public void ShouldReturnMountedBackend()
    {
        IFileSystemBackend backend = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend.OnMount(A<VPath>.Ignored)).Returns(DokiFS.Backends.MountResult.Accepted);

        VPath mountPoint = "/";
        VirtualFileSystem fs = new();
        fs.Mount(mountPoint, backend);

        bool result = fs.TryGetMountedBackend(mountPoint, out IFileSystemBackend outputBackend);

        Assert.True(result);
        Assert.IsAssignableFrom<IFileSystemBackend>(outputBackend);
    }

    [Fact(DisplayName = "TryGetMountedBackend: Returns correctly from path")]
    public void ShouldReturnMountedBackendFromPath()
    {
        IFileSystemBackend backend = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend.OnMount(A<VPath>.Ignored)).Returns(DokiFS.Backends.MountResult.Accepted);

        VPath mountPoint = "/";
        VPath retrievalPath = "/test";
        VirtualFileSystem fs = new();
        fs.Mount(mountPoint, backend);

        bool result = fs.TryGetMountedBackend(retrievalPath, out IFileSystemBackend outputBackend);

        Assert.True(result);
        Assert.IsAssignableFrom<IFileSystemBackend>(outputBackend);
    }

    [Fact(DisplayName = "TryGetMountedBackend: Returns false for unknown path")]
    public void ShouldReturnFalse()
    {
        IFileSystemBackend backend = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend.OnMount(A<VPath>.Ignored)).Returns(DokiFS.Backends.MountResult.Accepted);

        VPath mountPoint = "/test";
        VPath retrievalPath = "/test2";
        VirtualFileSystem fs = new();
        fs.Mount(mountPoint, backend);

        bool result = fs.TryGetMountedBackend(retrievalPath, out IFileSystemBackend outputBackend);

        Assert.False(result);
        Assert.Null(outputBackend);
    }
}
