| Property       | Description                                                          | Interface                 |
|----------------|----------------------------------------------------------------------|---------------------------|
| None           | Default backend, no special properties                               | /                         |
| ReadOnly       | Read-only backend, cannot write or modify files                      | /                         |
| RequiresCommit | Requires operation to finalize changes                               | `ICommit`                 |
| Cached         | Operates mostly on cached data, can become out of date               | /                         |
| Transient      | Data will not persist across sessions, but has no commit operation   | /                         |
| PhysicalPaths  | Backend can map back to a physical file system                       | `IPhysicalPathProvider`   |
| Flat           | Structure contains no directory structure or equivalent              | /                         |
