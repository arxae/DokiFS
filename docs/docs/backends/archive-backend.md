# Zip Archive Filesystem Backend

The `ZipArchiveFileSystemBackend` creates a filesystem that maps a zip archive to the filesystem.

| Property       | Description                                                          | Interface                 |
|----------------|----------------------------------------------------------------------|---------------------------|
| RequiresCommit | Requires operation to finalize changes                               | `ICommit`                 |
| Cached         | Operates mostly on cached data, can become out of date               | /                         |


## Basic Usage
To create a archive backend, you will need to supply it with the path to a zip archive. When instantiating the
backend with no extra parameters will open the zip archive in read only mode, disallowing changes.

```csharp
ZipArchiveFileSystemBackend backend = new("path/to/archive.zip");
```

You can also provide it with a mode and option to auto commit

```csharp
ZipArchiveFileSystemBackend backend = new("path/to/archive.zip", ZipArchiveMode.Update, true);
```

The zip archive backend internally uses the default `ZipArchive` class from dotnet. Due to the way this class works,
changes are only written after commiting the changes. This can be done by calling the `Commit` method.

*Note:* While there is a `Discard` method present due to the `ICommit` interface, this method doesn't actually
discard any changes. Due to the way the `ZipArchive` class works, once the instance is disposed, the changes are
written to disk.

## Operations
The archive backend supports all the regular `IVfsOperation` methods:
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

