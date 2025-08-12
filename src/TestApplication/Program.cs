using DokiFS.Backends.Memory;

namespace Test;

public class Program
{
    public static void Main()
    {
        MemoryFileSystemBackend backend = new();

        backend.CreateDirectory("/test");
        backend.CreateFile("/test/test.txt");

        backend.DeleteDirectory("/test", true);

        backend.ListDirectory("/")
            .ToList()
            .ForEach(Console.WriteLine);
    }
}
