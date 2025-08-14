using DokiFS.Interfaces;
using FakeItEasy;

namespace DokiFS.Tests.VirtualFileSystems.DefaultVfs;

public class DefaultVfsIsMountedTests
{
    [Fact(DisplayName = "IsMounted: Should return correctly")]
    public void ShouldReturnTrue()
    {
        IFileSystemBackend backend = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend.OnMount(A<VPath>.Ignored)).Returns(DokiFS.Backends.MountResult.Accepted);
        A.CallTo(() => backend.OnUnmount()).Returns(DokiFS.Backends.UnmountResult.Accepted);

        VPath mountPoint = "/";
        VirtualFileSystem fs = new();
        fs.Mount(mountPoint, backend);

        Assert.True(fs.IsMounted(mountPoint));
        Assert.False(fs.IsMounted("/nothingMounted"));
    }
}
