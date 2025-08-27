# Assembly Resource Filesystem Backend

The `AssemblyResourceFileSystemBackend` creates a filesystem that maps a .net assembly as a filesystem. This allows
read only access to the embedded resource

| Property       | Description                                                          | Interface                 |
|----------------|----------------------------------------------------------------------|---------------------------|
| ReadOnly       | Read-only backend, cannot write or modify files                      | /                         |
| Flat           | Structure contains no directory structure or equivalent              | /                         |


## Basic Usage
To create an assembly resource backend, you will need to supply it with the path to an assembly, along with the
base namespace of the assembly.

```csharp
AssemblyResourceFileSystemBackend backend = new("path/to/assembly.dll", "Test.Namespace");
```

This backend is read only as .net doesn't allow writing embedded resources outside of compilation.

Internally the files are stored prefixed with the default namespace of the assembly, along with the folderdname.
This results in a filename along the lines of `Test.Namespace.Resources.file.txt`. The namespace will be stripped off,
but to not make any guesses, the rest of the filename is stored as is. To access the above file for exa,ple, the internal
path will be `Resources.file.txt`.

## Operations
Due to the limitations of the embedded resources system, the assembly backend supports only the read
methods of `IVfsOperation`. It also has no concept of directories, so these methods are not available either:
* Exists
* GetInfo
* ListDirectory
* OpenRead

## Extra operations
There are some extra methods available specific to this backend:

* `UnloadAssembly`: This will unload the assembly. Since this uses the built in .net methods, if there are still any
references active (eg: a stream), the unload will silently fail. This will automatically clear the index
* `Index`: To keep the amounts of reads to a minimum, the contents of the assembly will be indexed when it's
initially loaded. Calling this method will clear the index, and scan the assembly again.

