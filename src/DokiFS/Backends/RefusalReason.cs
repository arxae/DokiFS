namespace DokiFS.Backends;

public enum MountResult
{
    Accepted,               // The backend accepted being mounted
    Refused,                // The backend refused being mounted for no specific reason
    NotInitialized,         // The backend is not initialized
    ResourceUnavailable,    // One of the underlying resources of the backend is unavailable
    AuthenticationFailure   // The backend needs to authenticate, but it can't
}

public enum UnmountResult
{
    Accepted,       // The backend accepted being unmounted
    Refused,        // The backend refused being unmounted for no specific reason
    InUse,          // The backend is currently in use and cannot be unmounted
    PendingWrites,  // The backend still needs to commit data to it's resource
    ResourceFailure // One of the underlying resources has an error or is unavailable
}
