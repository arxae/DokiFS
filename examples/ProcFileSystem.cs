using DokiFS;
using DokiFS.Backends.VirtualResource;
using DokiFS.Interfaces;

namespace TestApplication;

public class ProcFileSystem : IVirtualResourceHandler
{
    readonly Dictionary<VPath, Func<string>> fileProviders = [];

    public bool CanRead => true;
    public bool CanWrite => false;

    public ProcFileSystem()
    {
        fileProviders.Add("/meminfo", GetMemInfo);
    }

    public bool HandleExist(VPath path)
        => (path == "/") || fileProviders.ContainsKey(path);

    public IVfsEntry HandleGetInfo(VPath path)
    {
        if (path == "/")
        {
            return new VfsEntry("/", VfsEntryType.Directory, VfsEntryProperties.Readonly)
            {
                Size = 0,
                LastWriteTime = DateTime.UtcNow,
                FromBackend = typeof(ProcFileSystem),
                Description = "Process information pseudo-filesystem"
            };
        }

        if (fileProviders.TryGetValue(path, out Func<string>? _))
        {
            return new VfsEntry(path, VfsEntryType.File, VfsEntryProperties.Readonly)
            {
                Size = 0,
                LastWriteTime = DateTime.UtcNow,
                FromBackend = typeof(ProcFileSystem),
                Description = $"Virtual proc file: {path}"
            };
        }

        throw new FileNotFoundException($"Path not found: {path}");
    }

    public IEnumerable<IVfsEntry> HandleListDirectory(VPath path)
    {
        List<IVfsEntry> entries = [];

        foreach (VPath file in fileProviders.Keys)
        {
            entries.Add(new VfsEntry($"/{file}", VfsEntryType.File, VfsEntryProperties.Readonly)
            {
                Size = 0,
                LastWriteTime = DateTime.UtcNow,
                FromBackend = typeof(ProcFileSystem),
                Description = $"Virtual proc file: {file}"
            });
        }

        return entries;
    }

    public Stream HandleOpenRead(VPath path)
    {
        if (fileProviders.TryGetValue(path, out Func<string>? contentProvider) == false)
            throw new FileNotFoundException($"File not found: {path}");

        string content = contentProvider();
        byte[] data = System.Text.Encoding.UTF8.GetBytes(content);

        return new MemoryStream(data);
    }

    public Stream HandleOpenWrite(VPath path, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite)
        => throw new NotSupportedException("ProcFileSystem is read-only");

    static string GetMemInfo()
    {
        GCMemoryInfo memoryStatus = GC.GetGCMemoryInfo();
        long totalMemoryBytes = memoryStatus.TotalAvailableMemoryBytes;
        long totalMemoryKb = totalMemoryBytes / 1024;

        System.Text.StringBuilder sb = new System.Text.StringBuilder()
            .AppendLine($"MemTotal:      {totalMemoryKb} kB")
            .AppendLine($"MemFree:       {totalMemoryKb / 2} kB")
            .AppendLine($"SwapTotal:     0 kB")
            .AppendLine($"SwapFree:      0 kB");

        return sb.ToString();
    }
}
