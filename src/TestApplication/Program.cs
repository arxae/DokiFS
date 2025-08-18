using DokiFS.Backends.Archive;

namespace TestApplication;

public static class Program
{
    public static void Main()
    {
        ZipArchiveFileSystemBackend backend = new("test.zip", System.IO.Compression.ZipArchiveMode.Update, true);

        backend.CreateDirectory("randomdir/");
        backend.CreateFile("randomdir/test.txt", 1024 * 1024);
    }
}
