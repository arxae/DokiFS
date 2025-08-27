# Getting Started

## Install

Install via Nuget

## Usage

DokiFS uses different backends to expose data towards the user and allows the users to operate on it in a unified
manner. Multiple backends can be combined in a container to allow the same operations, on multiple backends.

### Working with a backend

You can instantiate a backend by itself, without using a container, if you only need to access a single source. Here is
a short example of creating a backend, creating a file, write to it and move it.

```csharp
// Instantiate a new backend using the working directory
PhysicalFileSystemBackend backend = new("~/);

VPath path = "/testfile.txt";

backend.CreateFile(path);

using Stream stream = backend.OpenWrite(path);
using StreamWriter writer = new(stream);
writer.WriteLine("This is a test");

// Create a directory,
backend.CreateDirectory("/testDirectory");
backend.MoveFile(path, "/testDirectory" / path);
```

