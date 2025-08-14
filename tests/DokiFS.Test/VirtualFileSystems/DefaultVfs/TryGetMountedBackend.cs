using DokiFS.Interfaces;
using FakeItEasy;

namespace DokiFS.Tests.VirtualFileSystems.DefaultVfs;

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

    [Fact(DisplayName = "TryGetMountedBackend: Return correct mount with multiple mounts")]
    public void ShouldReturnCorrectMountWithMultipleMounts()
    {
        IFileSystemBackend backend1 = A.Fake<IFileSystemBackend>();
        IFileSystemBackend backend2 = A.Fake<IFileSystemBackend>();
        IFileSystemBackend backend3 = A.Fake<IFileSystemBackend>();
        IFileSystemBackend backend4 = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend1.OnMount(A<VPath>.Ignored)).Returns(DokiFS.Backends.MountResult.Accepted);
        A.CallTo(() => backend2.OnMount(A<VPath>.Ignored)).Returns(DokiFS.Backends.MountResult.Accepted);
        A.CallTo(() => backend3.OnMount(A<VPath>.Ignored)).Returns(DokiFS.Backends.MountResult.Accepted);
        A.CallTo(() => backend4.OnMount(A<VPath>.Ignored)).Returns(DokiFS.Backends.MountResult.Accepted);

        VirtualFileSystem fs = new();
        fs.Mount("/", backend1);
        fs.Mount("/test", backend2);
        fs.Mount("/test/inner", backend3);
        fs.Mount("/test/inner/most", backend4);

        // Retrieve in random-ish order
        bool result4 = fs.TryGetMountedBackend("/test/inner/most", out IFileSystemBackend outputBackend4);
        bool result2 = fs.TryGetMountedBackend("/test/", out IFileSystemBackend outputBackend2);
        bool result3 = fs.TryGetMountedBackend("/test/inner", out IFileSystemBackend outputBackend3);

        Assert.True(result2);
        Assert.True(result3);
        Assert.True(result4);
        Assert.Same(backend2, outputBackend2);
        Assert.Same(backend3, outputBackend3);
        Assert.Same(backend4, outputBackend4);
    }
}
