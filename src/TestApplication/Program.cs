using DokiFS;
using DokiFS.Backends.Memory;

namespace Test;

public class Program
{
    public static void Main()
    {
        MemoryFileSystemBackend backend = new();
        VirtualFileSystem fs = new();
        fs.Mount("/", backend);
    }
}
