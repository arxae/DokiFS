using DokiFS.Backends;

namespace DokiFS.Exceptions;

public class MountRefusedException : Exception
{
    public MountResult MountResult { get; }

    public MountRefusedException(MountResult result)
        : this(result, GetDefaultMessageForMountResult(result), null) { }

    public MountRefusedException(MountResult result, string message)
        : this(result, message, null) { }
    public MountRefusedException(MountResult result, string message, Exception innerException)
        : base(message, innerException)
    {
        MountResult = result;
    }

    static string GetDefaultMessageForMountResult(MountResult result)
    {
        return result switch
        {
            MountResult.Accepted => "The backend accepted the mount, but an unspecified error occured",
            MountResult.Refused => "The backend rejected the mount, but no reason was given",
            MountResult.NotInitialized => "The backend rejected the mount because it was not initialized",
            MountResult.ResourceUnavailable => "The backend rejected the mount because it's underlying resource is not available",
            MountResult.AuthenticationFailure => "The backend rejected the mount because it has to authenticate with a resource, but it couldn't",
            MountResult.PathRefused => "The backend refused to be mounted to this path",
            MountResult.RootPathRefused => "The backend refused to be mounted as root",
            MountResult.NotRootPath => "The backend refused because it must be mounted as root",
            _ => "The backend accepted the mount, but an unspecified error occured",
        };
    }
}

public class UnmountRefusedException : Exception
{
    public UnmountResult UnmountResult { get; }

    public UnmountRefusedException(UnmountResult result)
        : this(result, GetDefaultMessageForUnmountResult(result), null) { }

    public UnmountRefusedException(UnmountResult result, string message)
        : this(result, message, null) { }
    public UnmountRefusedException(UnmountResult result, string message, Exception innerException)
        : base(message, innerException)
    {
        UnmountResult = result;
    }

    static string GetDefaultMessageForUnmountResult(UnmountResult result)
    {
        return result switch
        {
            UnmountResult.Accepted => "The backend accepted the unmount, but an unspecified error occured",
            UnmountResult.Refused => "The backend rejected the unmount, but no reason was given",
            UnmountResult.InUse => "The backend rejected the unmount because it's still in use",
            UnmountResult.PendingWrites => "The backend rejected the unmount because it still has writes pending",
            UnmountResult.ResourceFailure => "The backend rejected the unmount because the underlying resource has an error or is unavailable",
            UnmountResult.UncommittedChanges => "The backend refused to be unmounted with uncommitted changes",
            _ => "The backend accepted the unmount, but an unspecified error occured"
        };
    }
}
