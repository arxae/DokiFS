using System.Diagnostics.CodeAnalysis;
using DokiFS.Exceptions;

namespace DokiFS.Interfaces;

/// <summary>
/// This interface contain the methods to manage and query the mounts.
/// It is recommended that any class implementing this interface also implements the <seealso cref="IVfsOperations"/> IVfsOperations
/// interface to serve as passthrough and manage cross mount operations
/// </summary>
public interface IVirtualFileSystemContainer
{
    /// <summary>
    /// Mounts a backend to the specified mount point
    /// </summary>
    /// <param name="mountPoint">The path of the mount point</param>
    /// <param name="backend">The backend to mount</param>
    /// <exception cref="ArgumentNullException">Provided backend is null</exception>
    /// <exception cref="FormatException">The mount point is malformed</exception>
    /// <exception cref="MountPointConflictException">A backend is already mounted at the specified point</exception>
    /// <exception cref="MountRefusedException">The backend refused to be mounted</exception>
    /// <exception cref="VfsException">The backend accepted being mounted or has been forced, but something went wrong while mounting</exception>
    void Mount(VPath mountPoint, IFileSystemBackend backend);

    /// <summary>
    /// Mounts a backend to the specified mount point
    /// </summary>
    /// <param name="mountPoint">The path of the mount point</param>
    /// <param name="backend">The backend to mount</param>
    /// <param name="force">If set to true, mount the backend even if it refuses to be mounted</param>
    /// <exception cref="ArgumentNullException">Provided backend is null</exception>
    /// <exception cref="FormatException">The mount point is malformed</exception>
    /// <exception cref="MountPointConflictException">A backend is already mounted at the specified point</exception>
    /// <exception cref="MountRefusedException">The backend refused to be mounted</exception>
    /// <exception cref="VfsException">The backend accepted being mounted or has been forced, but something went wrong while mounting</exception>
    void Mount(VPath mountPoint, IFileSystemBackend backend, bool force);

    /// <summary>
    /// Unmount the backend at the specified mount point
    /// </summary>
    /// <param name="mountPoint">The path of the mount point</param>
    /// <exception cref="InvalidOperationException">No backend is mounted at the specified point</exception>
    /// <exception cref="BackendNotFoundException">Nothing is mounted at the mount point</exception>
    /// <exception cref="UnmountRefusedException">The backend refused to be unmounted</exception>
    /// <exception cref="VfsException">The backend accepted being unmounted or has been forced, but something went wrong while unmounting</exception>
    void Unmount(VPath mountPoint);

    /// <summary>
    /// Unmount the backend at the specified mount point
    /// </summary>
    /// <param name="mountPoint">The path of the mount point</param>
    /// <param name="force">If set to true, unmount the backend even if it refuses to be unmounted</param>
    /// <exception cref="InvalidOperationException">No backend is mounted at the specified point</exception>
    /// <exception cref="BackendNotFoundException">Nothing is mounted at the mount point</exception>
    /// <exception cref="UnmountRefusedException">The backend refused to be unmounted</exception>
    /// <exception cref="VfsException">The backend accepted being unmounted or has been forced, but something went wrong while unmounting</exception>
    void Unmount(VPath mountPoint, bool force);

    /// <summary>
    /// Checks if something is mounted at this mount point
    /// </summary>
    /// <param name="mountPoint">The mount point to check</param>
    /// <returns>True if something is mounted at this point</returns>
    bool IsMounted(VPath mountPoint);

    /// <summary>
    /// Tries to get the backend that is mounted at the specified path
    /// </summary>
    /// <param name="path">The path to retrieve the backend from</param>
    /// <param name="backend">Out parameter, the retrieved backend. Null if it failed</param>
    /// <param name="backendPath">Out parameter, the path inside the backend</param>
    /// <returns>True if a backend was found, false if not</returns>
    bool TryGetMountedBackend(VPath path, [MaybeNullWhen(false)] out IFileSystemBackend backend, out VPath backendPath);

    /// <summary>
    /// Gets all the mount points
    /// </summary>
    /// <returns>An enumeration of all mount points and their associated backends</returns>
    IEnumerable<KeyValuePair<VPath, IFileSystemBackend>> GetMountPoints();

    /// <summary>
    /// Get the mount point of a specific backend
    /// </summary>
    /// <param name="backend">The backend instance to retrieve the mount point from</param>
    /// <returns>The mount point where the backend is mounted</returns>
    /// <exception cref="ArgumentNullException">Thrown when backend is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when the backend is not mounted</exception>
    IEnumerable<VPath> GetMountPoint(IFileSystemBackend backend);

    /// <summary>
    /// Executes an action against a backend of a specific type mounted at the given path
    /// </summary>
    /// <param name="path">The path to the mounted backend</param>
    /// <param name="action">The action to execute against the backend</param>
    /// <typeparam name="T">The expected type of the backend</typeparam>
    /// <exception cref="InvalidCastException">The mounted backend is not of type T</exception>
    /// <exception cref="BackendNotFoundException">Nothing is mounted at the mount point</exception>
    void ExecuteAs<T>(VPath path, Action<T> action);
}
