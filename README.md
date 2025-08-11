---
outputFileName: index.html
---

# DokiFS

DokiFS is a virtual file system that supports multiple backends. It allows you to mount different types of storage systems and interact with them using a unified interface.

## Core Concepts

### Basic Usage

Mount backends to compose your filesystem
```csharp
VirtualFileSystem FileSystem = new();
FileSystem.Mount("/", new PhysicalFileSystemBackend(AppDomain.CurrentDomain.BaseDirectory));
FileSystem.Mount("/mem", new InMemoryFileSystemBackend());
FileSystem.CopyFile("/DokiFS.dll", "/mem/DokiFS.dll);
```
