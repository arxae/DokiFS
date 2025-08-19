namespace DokiFS.Backends;

[Flags]
public enum BackendProperties
{
    Default = 0,            // Default backend, no special properties
    ReadOnly = 1,           // Read-only backend, cannot write or modify files
    RequiresCommit = 2,     // Backend requires commit operations to finalize changes
    Cached = 4,             // Backend operates mostly on cached data
    Transient = 8,          // Backend is transient, data may not persist across sessions
    PhysicalPaths = 16,     // Backend can map back to the physical file system
    Flat = 32               // Backend structure contains no directory structure or equivalently emulated
}
