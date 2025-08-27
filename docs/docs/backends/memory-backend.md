# Memory Filesystem Backend

The `MemoryFileSystemBackend` creates a filesystem that only exists in memory. As soon as you close the application,
the backends data will be removed

| Property       | Description                                                          | Interface                 |
|----------------|----------------------------------------------------------------------|---------------------------|
| Transient      | Data will not persist across sessions, but has no commit operation   | /                         |

## Basic Usage
To create a memory backend, you just need to instantiate a new one:

```csharp
MemoryFileSystemBackend backend = new();
```

The constructor will automatically create a root node.

## Operations
The memory backend supports all the regular `IVfsOperation` methods:
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

## Saving the contents to disk
The memory backend can dump it's entire contents to disk using the `Dump` method. This method will write the entire
memory filesystem to disk.

*NOTE:* When `using` without braces, be sure to close the streams before dumping to disk. Otherwise these changes
will no be written.
