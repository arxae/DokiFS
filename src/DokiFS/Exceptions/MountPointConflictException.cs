namespace DokiFS.Exceptions;

public class MountPointConflictException : VfsException
{
    public VPath MountPoint { get; }

    public MountPointConflictException(
        VPath mountPoint,
        string message)
        : base(message)
    {
        MountPoint = mountPoint;
    }

    public MountPointConflictException(
        VPath mountPoint,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        MountPoint = mountPoint;
    }
}
