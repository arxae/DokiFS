using DokiFS.Exceptions;
using DokiFS.Interfaces;
using FakeItEasy;

namespace DokiFS.Tests.VirtualFileSystems.DefaultVfs;

public class DefaultVfsUnmountTests
{
    [Fact(DisplayName = "Unmount: Should Unmount")]
    public void ShouldUnmount()
    {
        IFileSystemBackend backend = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend.OnMount(A<VPath>.Ignored)).Returns(DokiFS.Backends.MountResult.Accepted);
        A.CallTo(() => backend.OnUnmount()).Returns(DokiFS.Backends.UnmountResult.Accepted);

        VPath mountPoint = "/";
        VirtualFileSystem fs = new();
        fs.Mount(mountPoint, backend);

        Assert.True(fs.IsMounted(mountPoint));

        Exception? ex = Record.Exception(() => fs.Unmount(mountPoint));
        Assert.Null(ex);
        Assert.False(fs.IsMounted(mountPoint));
        A.CallTo(() => backend.OnUnmount())
            .MustHaveHappenedOnceExactly();
    }

    [Fact(DisplayName = "Unmount: Should throw if nothing is mounted")]
    public void ShouldThrowWhenNothingMounted()
    {
        VPath mountPoint = "/";
        VirtualFileSystem fs = new();

        Assert.Throws<MountPointConflictException>(() => fs.Unmount(mountPoint));
    }

    [Fact(DisplayName = "Unmount: Should throw exception when unmounting is refused")]
    public void ShouldThrowWhenUnmountRefused()
    {
        IFileSystemBackend backend = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend.OnMount(A<VPath>.Ignored)).Returns(DokiFS.Backends.MountResult.Accepted);
        A.CallTo(() => backend.OnUnmount()).Returns(DokiFS.Backends.UnmountResult.Refused);

        VPath mountPoint = "/";
        VirtualFileSystem fs = new();
        fs.Mount(mountPoint, backend);

        Assert.True(fs.IsMounted(mountPoint));

        Exception? ex = Record.Exception(() => fs.Unmount(mountPoint));
        Assert.NotNull(ex);
        Assert.True(ex is UnmountRefusedException);
        Assert.Equal(DokiFS.Backends.UnmountResult.Refused, ((UnmountRefusedException)ex).UnmountResult);
    }

    [Fact(DisplayName = "Unmount: Should mount when force")]
    public void ShouldUnmountWhenForced()
    {
        IFileSystemBackend backend = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend.OnMount(A<VPath>.Ignored)).Returns(DokiFS.Backends.MountResult.Accepted);
        A.CallTo(() => backend.OnUnmount()).Returns(DokiFS.Backends.UnmountResult.Refused);

        VirtualFileSystem fs = new();
        VPath mountPoint = "/";
        fs.Mount(mountPoint, backend);

        Exception? ex = Record.Exception(() => fs.Unmount(mountPoint, true));
        Assert.Null(ex);
        A.CallTo(() => backend.OnUnmount())
            .MustHaveHappenedOnceExactly();
    }
}
