using System.Diagnostics;
using DokiFS;
using DokiFS.Backends.Memory;
using DokiFS.Backends.Physical;
using DokiFS.Extensions;

namespace TestApplication;

public static class Program
{
    public static void Main()
    {
        string ppath = AppDomain.CurrentDomain.BaseDirectory;

        VirtualFileSystem fs = new();
        fs.Mount("/", new PhysicalFileSystemBackend(ppath));
        fs.Mount("/mem", new MemoryFileSystemBackend());

        VPath tempFile = fs.GetTempFile("/mem/temp");

        Console.WriteLine(tempFile);

        fs.ListDirectory("/mem/temp")
            .ToList()
            .ForEach(Console.WriteLine);


        using (Stream stream = fs.OpenWrite(tempFile))
        using (StreamWriter writer = new(stream))
        {
            writer.Write("this is a testline");
        }

        using (Stream stream = fs.OpenRead(tempFile))
        using (StreamReader reader = new(stream))
        {
            Console.Write(reader.ReadToEnd());
        }
    }
}
