---
_layout: landing
---

![plot](./images/dokifs.png)

DokiFS is a flexible virtual file system for .NET that supports multiple backend storage systems through a
unified interface. It allows you to mount different types of storage and interact with them using consistent
path-based operations.

## Features
* Multiple backend support: Physical file systems, in-memory storage, zip archives, assembly resources, and more
* Virtual path abstraction: Work with consistent paths regardless of the underlying storage
* Journaling capability: Record and replay file operations with the journal backend
* Backend composition: Mount multiple backends at different paths to create a unified file system
* Read/write operations: Full support for common file operations (create, read, write, delete, copy, move)
* Only a single dependency in `Microsoft.Extensions.Logging`
* Cross-platform: Works on Windows, macOS, and Linux

## Installation
Nuget soon™

## Basic Usage

```csharp
using DokiFS;
using DokiFS.Backends.Memory;
using DokiFS.Backends.Physical;

// Create a new virtual file system
VirtualFileSystem fs = new();

// Mount a physical directory at the root
fs.Mount("/", new PhysicalFileSystemBackend("C:/MyFiles"));

// Mount an in-memory file system at /temp
fs.Mount("/temp", new MemoryFileSystemBackend());

// Create a file in memory
fs.CreateFile("/temp/example.txt");

// Write to the file
using (Stream stream = fs.OpenWrite("/temp/example.txt"))
using (StreamWriter writer = new(stream))
{
    writer.WriteLine("Hello, DokiFS!");
}

// Copy a file from memory to the physical backend
fs.CopyFile("/temp/example.txt", "/example.txt");

// List files in a directory
foreach (IVfsEntry entry in fs.ListDirectory("/"))
{
    Console.WriteLine($"{entry.FileName} - {entry.EntryType}");
}

// Clean up when done
fs.Unmount("/temp");
fs.Unmount("/");
```
