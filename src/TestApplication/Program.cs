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
        fs.Mount("/", null);
    }
}
