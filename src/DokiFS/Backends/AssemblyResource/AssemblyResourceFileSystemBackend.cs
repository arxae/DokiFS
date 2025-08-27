using System.Runtime.Loader;
using DokiFS.Interfaces;
using DokiFS.Logging;
using Microsoft.Extensions.Logging;

namespace DokiFS.Backends.AssemblyResource;

public class AssemblyResourceFileSystemBackend : IFileSystemBackend, IDisposable
{
    public BackendProperties BackendProperties => BackendProperties.ReadOnly | BackendProperties.Flat;

    readonly string resourcePathPrefix; // The namespace of the resource
    readonly AssemblyLoadContext loadContext;
    readonly DateTime assemblyTimestamp;
    readonly Dictionary<VPath, AssemblyFile> fileIndex = [];
    readonly Lock cacheLock = new();

    bool disposed;
    bool isAssemblyLoaded => loadContext != null && loadContext.Assemblies.Any();

    readonly ILogger log = DokiFSLogger.CreateLogger<AssemblyResourceFileSystemBackend>();

    public AssemblyResourceFileSystemBackend(string assemblyPath, string rootResourcePath)
    {
        if (Path.IsPathRooted(assemblyPath) == false)
        {
            throw new ArgumentException("The path to the assembly file must be an absolute path.", nameof(assemblyPath));
        }

        if (File.Exists(assemblyPath) == false)
        {
            throw new FileNotFoundException("The assembly file was not found.", assemblyPath);
        }

        resourcePathPrefix = rootResourcePath;

        try
        {
            loadContext = new AssemblyLoadContext("AssemblyResource", isCollectible: true);
            loadContext.LoadFromAssemblyPath(assemblyPath);
            assemblyTimestamp = File.GetLastWriteTimeUtc(assemblyPath);
        }
        catch (IOException e)
        {
            throw new IOException("Failed to read the assembly file.", e);
        }

        Index();
    }

    public void UnloadAssembly()
    {
        fileIndex.Clear();
        loadContext.Unload();
    }

    public void Index()
    {
        lock (cacheLock)
        {
            fileIndex.Clear();

            string[] manifestResourceNames;
            try
            {
                manifestResourceNames = loadContext.Assemblies.FirstOrDefault()?.GetManifestResourceNames();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to retrieve the manifest resource names from the assembly",
                    e);
            }

            if (manifestResourceNames == null)
            {
                throw new InvalidOperationException("Failed to retrieve the manifest resource names from the assembly");
            }

            foreach (string fullResourceName in manifestResourceNames)
            {
                VPath resourcePath = VPath.DirectorySeparator + fullResourceName.Replace(resourcePathPrefix, string.Empty).TrimStart('.');
                if (resourcePath.IsRoot) continue;

                long fileSize = -1;
                try
                {
                    using Stream stream = loadContext.Assemblies.FirstOrDefault()?
                        .GetManifestResourceStream(fullResourceName);
                    if (stream != null) fileSize = stream.Length;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Failed to get resource stream for {ResourceName}", fullResourceName);
                }

                AssemblyFile file = new(resourcePath)
                {
                    ResourcePath = fullResourceName,
                    EntryType = VfsEntryType.Virtual,
                    Properties = VfsEntryProperties.Readonly,
                    Size = fileSize,
                    LastWriteTime = assemblyTimestamp,
                    FromBackend = GetType(),
                    Description = "Embedded Resource File"
                };

                fileIndex[resourcePath] = file;
            }
        }
    }

    public MountResult OnMount(VPath mountPoint) => isAssemblyLoaded ? MountResult.Accepted : MountResult.ResourceUnavailable;

    public UnmountResult OnUnmount()
    {
        Dispose();

        return UnmountResult.Accepted;
    }

    public bool Exists(VPath path) => fileIndex.ContainsKey(path);

    public IVfsEntry GetInfo(VPath path) => fileIndex.GetValueOrDefault(path);

    /// <summary>
    /// Lists all files fromn the loaded assembly
    /// </summary>
    /// <remarks>Since this backend is flat, the path parameter does nothing</remarks>
    /// <param name="path"></param>
    /// <returns>An IEnumerable of the entries</returns>
    public IEnumerable<IVfsEntry> ListDirectory(VPath path)
    {
        if (isAssemblyLoaded == false)
        {
            throw new IOException("The assembly file was not loaded.");
        }

        return fileIndex.Select(c => c.Value);
    }

    public Stream OpenRead(VPath path)
    {
        AssemblyFile entry = fileIndex.GetValueOrDefault(path)
            ?? throw new FileNotFoundException($"Failed to find the resource file {path}.");

        try
        {
            return loadContext.Assemblies.FirstOrDefault()?
                .GetManifestResourceStream(entry.ResourcePath);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to open resource stream for {path}", ex);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed == false)
        {
            if (disposing)
            {
                UnloadAssembly();
            }

            disposed = true;
        }
    }

    // None of the writing methods are supported due to .net limitation. Cross backend moves away from the
    // assembly resource will be supported by OpenRead. Towards will not be possible.
    const string WriteNotSupportedMessage = "Writing to an assembly resource is not supported.";
    public void CreateFile(VPath path, long size = 0) => throw new NotImplementedException(WriteNotSupportedMessage);
    public void DeleteFile(VPath path) => throw new NotImplementedException(WriteNotSupportedMessage);
    public void MoveFile(VPath sourcePath, VPath destinationPath) => throw new NotImplementedException(WriteNotSupportedMessage);
    public void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite) => throw new NotImplementedException(WriteNotSupportedMessage);
    public void CopyFile(VPath sourcePath, VPath destinationPath) => throw new NotImplementedException(WriteNotSupportedMessage);
    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite) => throw new NotImplementedException(WriteNotSupportedMessage);
    public Stream OpenWrite(VPath path) => throw new NotImplementedException(WriteNotSupportedMessage);
    public Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share) => throw new NotImplementedException(WriteNotSupportedMessage);
    public void CreateDirectory(VPath path) => throw new NotImplementedException(WriteNotSupportedMessage);
    public void DeleteDirectory(VPath path) => throw new NotImplementedException(WriteNotSupportedMessage);
    public void DeleteDirectory(VPath path, bool recursive) => throw new NotImplementedException(WriteNotSupportedMessage);
    public void MoveDirectory(VPath sourcePath, VPath destinationPath) => throw new NotImplementedException(WriteNotSupportedMessage);
    public void CopyDirectory(VPath sourcePath, VPath destinationPath) => throw new NotImplementedException(WriteNotSupportedMessage);
}
