using DokiFS.Exceptions;
using DokiFS.Interfaces;
using FakeItEasy;

namespace DokiFS.Tests.VirtualFileSystems.Default;

public class DefaultVfsMountTests
{
    [Fact(DisplayName = "Mount: Should mount")]
    public void ShouldMount()
    {
        IFileSystemBackend backend = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend.OnMount(A<VPath>.Ignored))
            .Returns(DokiFS.Backends.MountResult.Accepted);

        VPath mountPoint = "/";
        VirtualFileSystem fs = new();

        Exception? ex = Record.Exception(() => fs.Mount(mountPoint, backend));
        Assert.Null(ex);
        Assert.True(fs.IsMounted(mountPoint));
        A.CallTo(() => backend.OnMount(A<VPath>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Fact(DisplayName = "Mount: Should throw exception on invalid mount format")]
    public void ShouldThrowOnInvalidMountPointFormat()
    {
        IFileSystemBackend backend = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend.OnMount(A<VPath>.Ignored))
            .Returns(DokiFS.Backends.MountResult.Accepted);

        VirtualFileSystem fs = new();
        VPath invalidMountPoint = "test1";
        VPath singleBackslash = "\\test2";
        VPath doubleBackslash = "\\\\test3";

        // Should throw because it doesnt start with /
        Assert.Throws<ArgumentException>(() => fs.Mount(invalidMountPoint, backend));

        // Should not throw because vpath will turn any number of \\ into /
        Exception? ex1 = Record.Exception(() => fs.Mount(singleBackslash, backend));
        Exception? ex2 = Record.Exception(() => fs.Mount(doubleBackslash, backend));

        Assert.Null(ex1);
        Assert.Null(ex2);
    }

    [Fact(DisplayName = "Mount: Should throw exception when using an occupied mountpoint")]
    public void ShouldThrowOnDuplicateMount()
    {
        IFileSystemBackend backend = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend.OnMount(A<VPath>.Ignored))
            .Returns(DokiFS.Backends.MountResult.Accepted);

        VirtualFileSystem fs = new();
        VPath mountPoint = "/";

        Exception? ex = Record.Exception(() => fs.Mount(mountPoint, backend));
        Assert.Null(ex);

        Assert.Throws<MountPointConflictException>(() => fs.Mount(mountPoint, backend));
    }

    [Fact(DisplayName = "Mount: Should throw exception when mounting is refused")]
    public void ShouldThrowWhenMountingIsRefused()
    {
        IFileSystemBackend backend = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend.OnMount(A<VPath>.Ignored))
            .Returns(DokiFS.Backends.MountResult.Refused);

        VirtualFileSystem fs = new();
        VPath mountPoint = "/";

        Exception? ex = Record.Exception(() => fs.Mount(mountPoint, backend));
        Assert.NotNull(ex);
        Assert.True(ex is MountRefusedException);
        Assert.Equal(DokiFS.Backends.MountResult.Refused, ((MountRefusedException)ex).MountResult);
    }

    [Fact(DisplayName = "Mount: Should mount when force")]
    public void ShouldMountWhenForced()
    {
        IFileSystemBackend backend = A.Fake<IFileSystemBackend>();

        A.CallTo(() => backend.OnMount(A<VPath>.Ignored))
            .Returns(DokiFS.Backends.MountResult.Refused);

        VirtualFileSystem fs = new();
        VPath mountPoint = "/";

        Exception? ex = Record.Exception(() => fs.Mount(mountPoint, backend, true));
        Assert.Null(ex);
        A.CallTo(() => backend.OnMount(A<VPath>.Ignored))
            .MustHaveHappenedOnceExactly();
    }
}
