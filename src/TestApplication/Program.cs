using DokiFS;
using DokiFS.Interfaces;
using DokiFS.Backends.Memory;
using DokiFS.Backends.Physical;

namespace Test;

public class Program
{
    public static void Main()
    {
        VirtualFileSystem fs = new();
        fs.Mount("/", new PhysicalFileSystemBackend(AppContext.BaseDirectory));
        string p = "/Users/amaes1/Projects/DokiFS/src/TestApplication/bin/Debug/test";
        fs.Mount("/t2", new PhysicalFileSystemBackend(p));

        fs.CopyDirectory("/dir1/subdir1", "/t2");

        fs.ListDirectory("/t2")
            .ToList()
            .ForEach(Console.WriteLine);
    }
}
