using DokiFS.Backends.Physical;

namespace Test;

public class Program
{
    public static void Main()
    {
        string path = AppDomain.CurrentDomain.BaseDirectory;
        PhysicalFileSystemBackend backend = new(path);

        List<DokiFS.Interfaces.IVfsEntry> dirs = [.. backend.ListDirectory("/")];

        dirs.ForEach(Console.WriteLine);
    }
}
