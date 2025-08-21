namespace DokiFS.Interfaces;

/// <summary>
/// This interface contains the methods that can be executed on the backends. The term 'resource' is used interchangeably for
/// both files and directories
/// </summary>
public interface IVfsOperations
{
    // Queries
    /// <summary>
    /// Checks if the specified resource exists on the backend
    /// </summary>
    /// <param name="path">The path to the resource</param>
    /// <returns>True if the path points to an existing resource</returns>
    bool Exists(VPath path);

    /// <summary>
    /// Gets information about the resource.
    /// </summary>
    /// <seealso cref="IVfsEntry"/>
    /// <param name="path">The path to the resource</param>
    /// <returns>An IVfsEntry that contains information about the resource</returns>
    /// <exception cref="FileNotFoundException">The resource does not exist</exception>
    IVfsEntry GetInfo(VPath path);

    /// <summary>
    /// Lists all the resources at the specified paths
    /// </summary>
    /// <param name="path">The path to a resource containing other resources</param>
    /// <returns>An IEnumerable of IVfsEntry objects that are contained in the resource</returns>
    IEnumerable<IVfsEntry> ListDirectory(VPath path);

    // File Operations
    /// <summary>
    /// Creates a resource at the specified path, with an optional size
    /// </summary>
    /// <param name="path">The path to create the resource at</param>
    /// <param name="size">The size of the resource. (Optional, defaults to 0)</param>
    void CreateFile(VPath path, long size = 0);

    /// <summary>
    /// Deletes a file type resource at the specified path.
    /// </summary>
    /// <param name="path">The path to the resource</param>
    /// <exception cref="IOException">The path points to a directory, not a file</exception>
    /// <exception cref="FileNotFoundException">The resource is not found at the specified path</exception>
    void DeleteFile(VPath path);

    /// <summary>
    /// Moves a file type resource
    /// </summary>
    /// <param name="sourcePath">The source file</param>
    /// <param name="destinationPath">The destination file</param>
    /// <exception cref="FileNotFoundException">The source file was not found</exception>
    /// <exception cref="IOException">The source or destination path points to a directory</exception>
    void MoveFile(VPath sourcePath, VPath destinationPath);

    /// <summary>
    /// Moves a file type resource
    /// </summary>
    /// <param name="sourcePath">The source file</param>
    /// <param name="destinationPath">The destination file</param>
    /// <param name="overwrite">Overwrites the destination file if it exists</param>
    void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite);

    /// <summary>
    /// Copies a file type resource
    /// </summary>
    /// <param name="sourcePath">The source file</param>
    /// <param name="destinationPath">The destination file</param>
    void CopyFile(VPath sourcePath, VPath destinationPath);

    /// <summary>
    /// Copies a file type resource
    /// </summary>
    /// <param name="sourcePath">The source file</param>
    /// <param name="destinationPath">The destination file</param>
    /// <param name="overwrite">Overwrites the destination file if it exists</param>
    void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite);

    // File streams
    /// <summary>
    /// Opens a read stream to the specified resource
    /// </summary>
    /// <param name="path">The path to the resource</param>
    /// <exception cref="FileNotFoundException">The resource was not found</exception>
    /// <exception cref="IOException">The path points to a directory</exception>
    /// <returns>The stream to the resource contents</returns>
    Stream OpenRead(VPath path);

    /// <summary>
    /// Opens a write stream to the specified resource with default modes
    /// </summary>
    /// <param name="path">The path to the resource</param>
    /// <exception cref="FileNotFoundException">The resource was not found</exception>
    /// <returns>The stream to the resource contents</returns>
    Stream OpenWrite(VPath path);

    /// <summary>
    /// Opens a write stream to the specified resource
    /// </summary>
    /// <param name="path">The path to the resource</param>
    /// <exception cref="FileNotFoundException">The resource was not found</exception>
    /// <returns>The stream to the resource contents</returns>
    Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share);

    // Directory Operations
    /// <summary>
    /// Creates a directory type resource
    /// </summary>
    /// <param name="path">The path to the resource</param>
    void CreateDirectory(VPath path);

    /// <summary>
    /// Deletes a directory type resource
    /// </summary>
    /// <exception cref="IOException">Path points to a file</exception>
    /// <param name="path">The path to the resource</param>
    void DeleteDirectory(VPath path);

    /// <summary>
    /// Deletes a directory type resource
    /// </summary>
    /// <exception cref="IOException">Path points to a file</exception>
    /// <param name="path">The path to the resource</param>
    /// <param name="recursive">If set to true, also delete all the contents</param>
    void DeleteDirectory(VPath path, bool recursive);

    /// <summary>
    /// Moves a directory type resource
    /// </summary>
    /// <param name="sourcePath">The source directory resource</param>
    /// <param name="destinationPath">The destination directory resource</param>
    void MoveDirectory(VPath sourcePath, VPath destinationPath);

    /// <summary>
    /// Copies a directory type resource
    /// </summary>
    /// <param name="sourcePath">The source directory resource</param>
    /// <param name="destinationPath">The destination directory resource</param>
    void CopyDirectory(VPath sourcePath, VPath destinationPath);
}
