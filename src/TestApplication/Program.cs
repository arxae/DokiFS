using DokiFS;
using DokiFS.Backends.Journal;
using DokiFS.Backends.Memory;

namespace TestApplication;

public static class Program
{
    public static void Main()
    {
        string ppath = AppDomain.CurrentDomain.BaseDirectory;

        VirtualFileSystem fs = new();
        MemoryFileSystemBackend mem = new();
        fs.Mount("/", mem);
        fs.Mount("/jr", new JournalFileSystemBackend());

        // Record journal entries
        // fs.CreateFile("/jr/testtest.txt", 1024);
        using (Stream stream = fs.OpenWrite("/jr/test.txt"))
        using (StreamWriter writer = new StreamWriter(stream))
        {
            writer.WriteLine("Hello");
            writer.WriteLine("World");
        }

        Console.WriteLine(fs.GetInfo("/jr/test.txt"));

        using (Stream stream = fs.OpenRead("/jr/test.txt"))
        using (StreamReader reader = new(stream))
        {
            Console.WriteLine(reader.ReadToEnd());
        }

        Console.WriteLine(fs.GetInfo("/jr/test.txt"));
    }
}
