
using DokiFS.Backends.VirtualResource;
using DokiFS.Interfaces;
using FakeItEasy;

namespace DokiFS.Tests.Backends.VirtualResource;

public class VirtualResourceBackendHandlerTests
{
    [Fact]
    public void HandlerShouldRegister()
    {
        IVirtualResourceHandler handler = A.Fake<IVirtualResourceHandler>();
        VirtualResourceBackend backend = new();

        backend.RegisterHandler("/test", handler);
        bool found = backend.TryResolveHandler("/test/file.txt", out IVirtualResourceHandler retrievedHandler, out VPath pathRemainder);

        Assert.True(found);
        Assert.Same(handler, retrievedHandler);
        Assert.Equal("/file.txt", pathRemainder.ToString());
    }

    [Fact]
    public void RegisteringDuplicateShouldThrow()
    {
        IVirtualResourceHandler handler = A.Fake<IVirtualResourceHandler>();
        VirtualResourceBackend backend = new();

        backend.RegisterHandler("/test", handler);

        Assert.Throws<InvalidOperationException>(() => backend.RegisterHandler("/test", handler));
    }

    [Fact]
    public void ShouldUnregister()
    {
        IVirtualResourceHandler handler = A.Fake<IVirtualResourceHandler>();
        VirtualResourceBackend backend = new();

        backend.RegisterHandler("/test", handler);
        bool unregistered = backend.UnregisterHandler("/test");

        Assert.True(unregistered);
        Assert.False(backend.TryResolveHandler("/test/file.txt", out _, out _));
    }

    [Fact]
    public void ShouldThrowWhenUnregisteringNonExistentHandler()
    {
        VirtualResourceBackend backend = new();
        bool found = backend.TryResolveHandler("/nonexistent/file.txt",
            out IVirtualResourceHandler retrievedHandler, out VPath pathRemainder);

        Assert.False(backend.UnregisterHandler("/nonexistent"));
        Assert.False(found);
        Assert.Null(retrievedHandler);
        Assert.Equal("/file.txt", pathRemainder);
    }

    [Fact]
    public void ShouldHandleExists()
    {
        IVirtualResourceHandler handler = A.Fake<IVirtualResourceHandler>();
        A.CallTo(() => handler.HandleExist(A<VPath>._)).Returns(true);

        VirtualResourceBackend backend = new();
        backend.RegisterHandler("/test", handler);

        Assert.True(handler.HandleExist("/test/file.txt"));
    }

    [Fact]
    public void ShouldHandleGetInfo()
    {
        IVirtualResourceHandler handler = A.Fake<IVirtualResourceHandler>();

        VfsEntry rootEntry = new(
                "/",
                VfsEntryType.Directory,
                VfsEntryProperties.Default)
        {
            Size = 0,
            LastWriteTime = DateTime.UtcNow,
            FromBackend = typeof(VirtualResourceBackend),
            Description = "Virtual resource backend root"
        };

        A.CallTo(() => handler.HandleGetInfo(A<VPath>._)).Returns(A.Fake<IVfsEntry>());
        A.CallTo(() => handler.HandleGetInfo(VPath.Root)).Returns(rootEntry);

        VirtualResourceBackend backend = new();
        backend.RegisterHandler("/test", handler);

        Assert.NotNull(backend.GetInfo("/test/file.txt"));
        Assert.Equal(rootEntry.FullPath, backend.GetInfo(VPath.Root).FullPath);
        Assert.Null(backend.GetInfo("/nonexistent.txt"));
    }

    [Fact]
    public void ShouldHandleListDirectory()
    {
        IVirtualResourceHandler handler = A.Fake<IVirtualResourceHandler>();
        A.CallTo(() => handler.HandleListDirectory(A<VPath>._)).Returns([
            new VfsEntry("/test/file1.txt", VfsEntryType.File, VfsEntryProperties.Default),
            new VfsEntry("/test/file2.txt", VfsEntryType.File, VfsEntryProperties.Default)
        ]);

        VirtualResourceBackend backend = new();
        backend.RegisterHandler("/test", handler);

        IEnumerable<IVfsEntry> dirs = backend.ListDirectory("/test");

        Assert.Equal(2, dirs.Count());
    }
}
