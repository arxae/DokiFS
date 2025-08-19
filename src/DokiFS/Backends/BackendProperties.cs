namespace DokiFS.Backends;

[Flags]
public enum BackendProperties
{
    Default,            // Default backend, no special properties
    ReadOnly,           // Read-only backend, cannot write or modify files
    RequiresCommit,     // Backend requires commit operations to finalize changes
    Cached,             // Backend operates mostly on cached data
    Transient,          // Backend is transient, data may not persist across sessions
    PhysicalPaths, 		// Backend can map back to the physical file system
    Flat                // Backend structure contains no directory structure or equivalently emulated
}
