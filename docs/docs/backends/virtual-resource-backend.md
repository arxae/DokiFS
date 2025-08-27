# Virtual Resource Filesystem Backend

The `VirtualResourceBackend` creates a filesystem with programmable files. Accessing a file inside this backend will
return data based on the handlers attached to this path. This allows the creation of files
such as the `/proc/meminfo` file from Linux.

| Property       | Description                                                          | Interface                 |
|----------------|----------------------------------------------------------------------|---------------------------|
| Transient      | Data will not persist across sessions, but has no commit operation   | /                         |

## Basic Usage
The virtual resource backend has a parameterless constructor

```csharp
VirtualResourceBackend backend = new();
```

You will then need to create a handler by implementing the `IVirtualResourceHandler` interface. After that, you can
register them with the backend. You can find the code of the example `ProcFileSystem` in the examples folder.

```csharp
backend.RegisterHandler("/proc", new ProcFileSystem())
```

In this case, `/proc` is used as the handler identifier. Everything after this is handled by the handler implementation.
For example, when accessing `/proc/meminfo`, `/proc` is used to identify the handler, and `/meminfo` is sent to handler.
It's then completely up to the handler to handle the rest of this path as it sees fit.

*Note:* Even though only the `/meminfo` segment is shown in this example, there are no restrictions on the length
of this path. Only the first segment is consumed.

## Operations
Since these files don't actually exist, this backend only supports the read methods of `IVfsOperation` with the
exception of `OpenWrite`:
* Exists
* GetInfo
* ListDirectory
* OpenRead
* OpenWrite

`IVirtualResourceHandler` contains a method `HandleOpenWrite` which allows the handler to return data as needed.
Every operation checks if the handler allows reading or writing by the `CanRead` and `CanWrite` properties. Should
any of these 2 return false, then the operation will fail, returning a "failed" result (false for `Exits`, null for
`GetInfo`, etc..) `OpenRead` and `OpenWrite` are an exception. These will throw a `NotAllowedToReadException` and
`NotAllowedToWriteException` instead.

## Extra operations
There are some extra methods available specific to this backend:

* `RegisterHandler`: As shown before, this will actually register a handler. There is both a instance and generic
version of this method available.
* `UnregisterHandler`: This removes a registered handler, making it's path unavailable.
* `TryResolveHandler`: Tries to find and return a given handler for the specified path. It will return the handler
identifier and remainder of the path separately.
