# Virtual Filesystem Implementation

Backends can be used by themselves if needed, but they can also be combined into a single contaner. This allows to
treat the entire filesystem in a unified way. For example:

```csharp
// Create a new container and mount 2 separate filesystems to it
VirtualFileSystem fs = new();
fs.Mount("/", new PhysicalFileSystemBackend("~"));
fs.Mount("/mem", new MemoryFileSystemBackend());

// Create a new file on the physical filesystem
fs.CreateFile("/testfile.txt");

// Move the file from the physical filesystem, to the in memory filesystem
fs.MoveFile("/testfile.txt", "/mem/testfile.txt");
```

The `fs.CreateFile("/testfile.txt")` call will create the file on the physical filesystem. But since `/mem/testfile.txt`
resolves to the memory filesystem, the file will be removed from disk, and moved into memory. Letting the application
run like this will effecively remove the file.

The default implementation also implements `IVfsOperations`. Using the provided path of an operation, it will resolve
the correct backend and pass the operation through. Cross backend operations are supported as well. Thit relies
on the `OpenRead` and `OpenWrite` methods of the backends.

## Mounting and Unmounting

Backends can be mounted and unmounted using the `Mount` and `Unmount` method. This will do some preliminary checks
to see if the mount point is correctly formed and if the mount point is already in use. It will also call the
`OnMount` and `OnUnmount` methods on the backend to see if it can be (un)mounted.

## Retrieving mounts

* `TryGetMountedBackend`: Given a path, it will try to return the backend associated with it.

```csharp
bool found = fs.TryGetMountedBackend("/", out IFileSystemBackend backend, out VPath backendPath);
```

In the above example, `backend` is the actual backend while `backendPath` is the path inside the backend.

* `ExecuteAs`: Will resolve the backend at the provided path, and return the backend as context. Eg:

```csharp
FileSystem.ExecuteAs<MemoryFileSystemBackend>("/mem", backend =>
{
    backend.CreateFile("/test.txt");
});
```

## Implementing your own VFS container

The default implementation of the `VirtualFileSystem` should be adequate for most scenario's. But there might be
scenario's that you want to have a custom implementation. Although not necesary, the `IVirtualFileSystemContainer` can
be implemented for this purpose.

