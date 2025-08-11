using DokiFS.Backends.Physical;

namespace Test;

public class Program
{
    public static void Main()
    {
        string path = AppDomain.CurrentDomain.BaseDirectory;
        PhysicalFileSystemBackend backend = new(path);

        bool success = backend.TryGetPhysicalPath("/DokiFS.dll", out string ppath);
        Console.WriteLine(ppath);
    }
}
