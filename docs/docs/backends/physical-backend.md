# Physical Filesystem Backend

The `PhysicalFileSystemBackend` provides access to the local filesystem through DokiFS's virtual filesystem interface. This backend allows you to mount a local directory and interact with it using the DokiFS API.

| Property       | Description                                                          | Interface                 |
|----------------|----------------------------------------------------------------------|---------------------------|
| PhysicalPaths  | Backend can map back to a physical file system                       | `IPhysicalPathProvider`   |

## Basic Usage
To create a physical backend, you need to provide a path to a local directory:

```csharp
string localPath = "/path/to/directory";
PhysicalFileSystemBackend backend = new PhysicalFileSystemBackend(localPath);
```
The constructor validates that:

* The path is not null or whitespace
* The path is valid and accessible
* The path points to a directory (not a file)
* The directory exists

## Operations
The physical backend supports all the regular `IVfsOperation` methods:
* Exists
* GetInfo
* ListDirectory
* CreateFile
* DeleteFile
* MoveFile
* CopyFile
* OpenRead
* OpenWrite
* CreateDirectory
* DeleteDirectory
* MoveDirectory
* CopyDirectory

All operations are constrained to the root directory. Trying to perform an operation out of the mapped filesystem
will throw an exception

## Physical Path Provider
The physical backend implements the `IPhysicalPathProvider` provider. This allows the backend to return the physical
path of a file (that is within the backend) using the `TryGetPhysicalPath` method:

```csharp
VPath testPath = "/test/file.txt"
if(TryGetPhysicalPath(path, out string physicalPath))
{
    // physicalPath now contains the absolute to the file on disk
}
```


