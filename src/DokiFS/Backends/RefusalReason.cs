namespace DokiFS.Backends;

public enum MountResult
{
    Accepted,               // The backend accepted being mounted
    Refused,                // The backend refused being mounted for no specific reason
    NotInitialized,         // The backend is not initialized
    ResourceUnavailable,    // One of the underlying resources of the backend is unavailable
    AuthenticationFailure,  // The backend needs to authenticate, but it can't
    PathRefused,            // The backend refused to be mounted to this path
    RootPathRefused,        // The backend refused to be mounted as root
    NotRootPath             // The backend refused because it must be mounted as root
}

public enum UnmountResult
{
    Accepted,               // The backend accepted being unmounted
    Refused,                // The backend refused being unmounted for no specific reason
    InUse,                  // The backend is currently in use and cannot be unmounted
    PendingWrites,          // The backend still needs to commit data to it's resource
    ResourceFailure,        // One of the underlying resources has an error or is unavailable
    UncommittedChanges      // The backend refused to be unmounted with uncommitted changes
}
