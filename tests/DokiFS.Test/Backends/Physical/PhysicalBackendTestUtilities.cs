using System.Diagnostics;

namespace DokiFS.Tests.Backends.Physical;

/// <summary>
/// Manages temporary folder for testing content
/// </summary>
public class PhysicalBackendTestUtilities : IDisposable
{
    public string BackendRoot { get; private set; }

    public PhysicalBackendTestUtilities(string testName)
    {
        testName = testName
            .Replace("PhysicalFileSystemBackendOpenReadTests", string.Empty)
            .Replace("Tests", string.Empty);

        BackendRoot = Path.Combine(Path.GetTempPath(), "DokiFSTest_" + Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(BackendRoot);
        Debug.WriteLine($"{testName} temp path: " + BackendRoot);
    }

    public string CreateTempFile(string fileName)
    {
        string path = Path.Combine(BackendRoot, fileName);
        using FileStream _ = File.Create(path);

        return path;
    }

    public string CreateTempFileWithSize(string fileName, long size)
    {
        string path = Path.Combine(BackendRoot, fileName);
        using FileStream fs = File.Open(path, FileMode.CreateNew);

        if (size > 0)
        {
            const int bufferSize = 4096;
            byte[] buffer = new byte[Math.Min(bufferSize, size)];

            long remaining = size;
            while (remaining > 0)
            {
                int currentChunk = (int)Math.Min(buffer.Length, remaining);
                fs.Write(buffer, 0, currentChunk);
                remaining -= currentChunk;
            }
        }

        return path;
    }

    public string CreateTempDirectory(string dirName)
    {
        string path = Path.Combine(BackendRoot, dirName);
        Directory.CreateDirectory(path);

        return path;
    }

    public bool FileExists(string path)
    {
        path = Path.Combine(BackendRoot, path);
        return File.Exists(path);
    }

    public bool DirExists(string path)
    {
        path = Path.Combine(BackendRoot, path);
        return Directory.Exists(path);
    }

    public void RemoveFile(string path)
    {
        path = Path.Combine(BackendRoot, path);
        File.Delete(path);
    }

    public void RemoveDir(string path)
    {
        path = Path.Combine(BackendRoot, path);
        Directory.Delete(path, true);
    }

    public string GetContentString(string path)
    {
        path = Path.Combine(BackendRoot, path);
        return File.ReadAllText(path);
    }

    public void Dispose()
    {
        if (Directory.Exists(BackendRoot))
        {
            Directory.Delete(BackendRoot, true);
        }

        GC.SuppressFinalize(this);
    }
}
