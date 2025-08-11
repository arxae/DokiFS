using System.Runtime.InteropServices;
using DokiFS.Interfaces;

namespace DokiFS;

public class VfsEntry : IVfsEntry
{
    /// <summary>
    /// The name of the file or directory.
    /// </summary>
    public string FileName => FullPath.GetFileName();

    /// <summary>
    /// The full path of the file or directory within the virtual file system.
    /// </summary>
    public VPath FullPath { get; set; }

    /// <summary>
    /// Indicates whether this entry is a directory or a file.
    /// </summary>
    public VfsEntryType EntryType { get; set; }

    /// <summary>
    /// The properties of the entry, such as Readonly, Hidden, etc.
    /// </summary>
    public VfsEntryProperties Properties { get; set; }

    /// <summary>
    /// The size of the file in bytes. For directories, this is typically 0.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// The last write time of the file or directory.
    /// </summary>
    public DateTime LastWriteTime { get; set; }

    /// <summary>
    /// The type of backend this entry is from (e.g., FileSystem, Archive, etc.).
    /// </summary>
    public Type FromBackend { get; set; }

    /// <summary>
    /// A description of the entry, if applicable.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VfsEntry"/> class.
    /// </summary>
    public VfsEntry() { }

    public override string ToString()
        => $"{FileName} ({(EntryType == VfsEntryType.Directory ? "Directory" : "File")}) - {Size} bytes, Backend: {FromBackend?.FullName ?? "Unknown"}, Description: {Description ?? "N/A"}";


    /// <summary>
    /// Initializes a new instance of the <see cref="VfsEntry"/> class.
    /// </summary>
    public VfsEntry(VPath path, VfsEntryType type, VfsEntryProperties properties = VfsEntryProperties.Default)
    {
        FullPath = path;
        EntryType = type;
        Properties = properties;
    }

    /// <summary>
    /// Creates a VfsEntry from a FileSystemInfo object.
    /// </summary>
    /// <param name="info">FileSystemInfo object pointing to a file or directory</param>
    /// <param name="path">The full virtual path within the VFS.</param>
    /// <param name="FromBackend">The type of backend this entry is from (e.g., FileSystem, Archive, etc.).</param>
    /// <param name="Description">A description of the entry, if applicable.</param>
    /// <returns>A new VfsEntry instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if info is null.</exception>
    /// <exception cref="ArgumentException">Thrown if path is null or empty.</exception>
    /// <remarks>
    /// This method checks if the entry is read-only and hidden based on the platform and attributes.
    /// On Windows, it checks the FileAttributes for ReadOnly and Hidden attributes.
    /// On Linux and macOS, it attempts to open the file for writing to determine if it's read-only.
    ///
    /// The hidden check is based on the file attributes on windows, and on Linux/macOS, it checks if the file name
    /// starts with a dot ('.').
    /// </remarks>
    /// <returns>A VfsEntry representing the file or directory.</returns>
    public static VfsEntry FromFileSystemInfo(FileSystemInfo info, VPath path, Type FromBackend, string Description)
    {
        bool isReadOnly = CheckIsReadOnly(info);
        bool isHidden = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden
            : info.Name.StartsWith('.');

        VfsEntryProperties properties = VfsEntryProperties.Default;
        if (isReadOnly) properties |= VfsEntryProperties.Readonly;
        if (isHidden) properties |= VfsEntryProperties.Hidden;

        if (properties != VfsEntryProperties.Default)
        {
            properties &= ~VfsEntryProperties.Default;
        }

        return new VfsEntry()
        {
            FullPath = path,
            EntryType = info is DirectoryInfo ? VfsEntryType.Directory : VfsEntryType.File,
            Properties = properties,
            Size = info is FileInfo finfo ? finfo.Length : 0,
            LastWriteTime = info.LastWriteTimeUtc,
            FromBackend = FromBackend,
            Description = Description
        };
    }

    /// <summary>
    /// Checks if the given FileSystemInfo is read-only.
    /// </summary>
    /// <param name="info">The FileSystemInfo object to check.</param>
    /// <returns>True if the entry is read-only, false otherwise.</returns>
    /// <remarks>
    /// On Windows, it checks the FileAttributes for ReadOnly.
    /// On Linux and macOS, it attempts to open the file for writing to determine if it's read-only.
    /// For directories, it tries to create a temporary file to check write permissions.
    /// </remarks>
    /// <returns>True if the entry is read-only, false otherwise.</returns>
    static bool CheckIsReadOnly(FileSystemInfo info)
    {
        if (info is FileInfo fileInfo)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return (fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    using FileStream _ = new(fileInfo.FullName, FileMode.Open, FileAccess.Write);
                    return false; // If we can open it for read/write, it's not read-only
                }
                catch (UnauthorizedAccessException)
                {
                    return true;
                }
                catch (IOException)
                {
                    return true;
                }
            }
        }
        else if (info is DirectoryInfo dirInfo)
        {
            string tempFileName = Path.Combine(dirInfo.FullName, ".tempfile");
            try
            {
                using FileStream fs = File.Create(tempFileName);

                // If we can create a file in the directory, it's not read-only
                fs.Close();
                File.Delete(tempFileName);

                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
            catch (IOException)
            {
                return true;
            }
        }

        return true; // Default to true if we can't determine to be on the safe side
    }

    public Stream OpenRead(VPath path)
        => throw new NotImplementedException();

    public Stream OpenWrite(VPath path, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite)
        => throw new NotImplementedException();
}
