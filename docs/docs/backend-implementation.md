# Implementing your own backend.

Implementing your backend is relatively simple. You just need to implement the `IFileSystemBackend` interface, which
contains the methods to be able to be mounted. This interface also inherits from the `IVfsOperations` interface,
containing all the actual file system operations

## The `IFileSystemBackend` interface.

This interface contains all the base methods for the backend to be able to mounted. This interface contains 3 properties:

### Backend Properties
The `BackendProperties` property contains a general overview of the properties of the backend. This does not limit
anything, but is mainly used to indicate certain things in a more performant manner.

Some properties are usually tied to an interface such as `RequiresCommit` usually implements the `ICommit` interface.
Below you can find a table of properties and the interfaces they are associated with.

| Property       | Description                                                          | Interface                 |
|----------------|----------------------------------------------------------------------|---------------------------|
| None           | Default backend, no special properties                               | /                         |
| ReadOnly       | Read-only backend, cannot write or modify files                      | /                         |
| RequiresCommit | Requires operation to finalize changes                               | `ICommit`                 |
| Cached         | Operates mostly on cached data, can become out of date               | /                         |
| Transient      | Data will not persist across sessions, but has no commit operation   | /                         |
| PhysicalPaths  | Backend can map back to a physical file system                       | `IPhysicalPathProvider`   |
| Flat           | Structure contains no directory structure or equivalent              | /                         |

### OnMount

The `OnMount` method is called after the mount is verified, but before the actual mount happens. This allows the backend
to refuse or accept the mount. For example, if a mount is designed to not be mounted at the root path, or requires
certain authentication but is unable to do so, then the mount can refuse being mounted. There is a `Mount` method
overload that allows the mount to be forced. Below you can find a table of reasons why the mount may be refused

| MountResult           | Description                                                           |
|-----------------------|-----------------------------------------------------------------------|
| Accepted              | The backend accepted being mounted                                    |
| Refused               | The backend refused being mounted for no specific reason              |
| NotInitialized        | The backend is not initialized                                        |
| ResourceUnavailable   | One of the underlying resources of the backend is unavailable         |
| AuthenticationFailure | The backend needs to authenticate, but it can't                       |
| PathRefused           | The backend refused to be mounted to this path                        |
| RootPathRefused       | The backend refused to be mounted as root                             |
| NotRootPath           | The backend refused because it must be mounted as root                |

An example usages is an extra verification in `AssemblyResourceFileSystemBackend`. When it's unable to load the
assembly, it will refuse being mounted.

### OnUnmount

In the same manner as `Mount`, there is also a `UnMount` method. It works in the same way, but allows the backend
to refuse being unmounted. This can be used to refuse being mounted when there are still changes to be commited, or
if the backend is still being closed. As with the `Mount` method, there is an overload that allows forcing the unmount
Below you can find a table of reasons why the unmount may be refused.

| UnmountResult         | Description                                                       |
|-----------------------|-------------------------------------------------------------------|
| Accepted              | The backend accepted being unmounted                              |
| Refused               | The backend refused being unmounted for no specific reason        |
| InUse                 | The backend is currently in use and cannot be unmounted           |
| PendingWrites         | The backend still needs to commit data to it's resource           |
| ResourceFailure       | One of the underlying resources has an error or is unavailable    |
| UncommittedChanges    | The backend refused to be unmounted with uncommitted changes      |

An examplke usage is in `JournalFileSystemBackend`. When trying to unmount the backend, but there are uncommited
changes, then the unmount will be refused.

## `IVfsOperations` interface

The other part that needs to be implemented is the `IVfsOperations` interface. This interface contains all the methods
that can be executed on/by a backend.
These methods should be straightforward in what they mean. The provided backends implement these methods like
the methods from the `File`, `Directory` and `Path` classes from standard .NET

While implementing these methods, keep the `BackendProperties` in mind. If the backend does not support writing
for example, please be sure to assign the `ReadOnly` property.

## `IVfsEntry` interface

While not needing to be implemented for a backend, a couple of methods need to return this. `IVfsEntry` containst a
common interface for what a backend could consider a file or directory.

`VfsEntry` contains a base implementation of this interface. Some backends will inherit from this to implement their
own VfsEntry properties. For example `AssemblyFile` is used by the `AssemblyResourceFileSystemBackend` backend.
It contains the original filename inside the assebly, since this is not relevant to any other backends.
