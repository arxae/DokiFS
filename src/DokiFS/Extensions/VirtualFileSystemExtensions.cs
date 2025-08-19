using System.Reflection.Metadata.Ecma335;
using DokiFS.Interfaces;

namespace DokiFS.Extensions;

public static class VirtualFileSystemExtensions
{
    const string tempChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    const string tempPrefix = "tmp";
    const string tempSuffix = ".tmp";

    static Random rng = new();

    public static VPath GetTempFile(this IVirtualFileSystem vfs, VPath basePath = default)
    {
        if (basePath == default)
        {
            basePath = "/temp";
        }

        bool successful = vfs.TryGetMountedBackend(basePath, out IFileSystemBackend backend, out VPath backendPath);

        // If the first attempt failed, use the first available backend
        // TODO: Test this part
        if (successful == false)
        {
            VPath secondAttemptPath = vfs.GetMountPoints().FirstOrDefault().Key;
            successful = vfs.TryGetMountedBackend(secondAttemptPath, out backend, out backendPath);

            if (successful == false)
            {
                throw new FileNotFoundException("Could not find available backend for file", basePath.FullPath);
            }

            basePath = backendPath;

            // KeyValuePair<VPath, IFileSystemBackend> be = vfs.GetMountPoints().FirstOrDefault();
            //
            // backend = be.Value
            //     ?? throw new FileNotFoundException("Could not find available backend for file", basePath.FullPath);
            // basePath = be.Key.Append(basePath);
        }

        for (int attempts = 0; attempts < 10; attempts++)
        {
            string randomPart = new([..Enumerable.Range(0, 6).Select(_ => tempChars[rng.Next(tempChars.Length)])]);
            VPath candidatePath = Path.Combine(backendPath.FullPath, tempPrefix + randomPart + tempSuffix);

            try
            {
                backend.CreateDirectory(candidatePath.GetDirectory());
                backend.CreateFile(candidatePath);
                using Stream _ = backend.OpenWrite(candidatePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

                VPath mp = vfs.GetMountPoint(backend);

                return mp.Append(candidatePath);
            }
            catch
            {
                // A collision occured
            }
        }

        throw new IOException("Unable to create temporary file after multiple attempts");
    }
}
