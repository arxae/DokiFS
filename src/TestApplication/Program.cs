using System.Collections.ObjectModel;
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
        JournalFileSystemBackend jr = new();
        fs.Mount("/", mem);
        fs.Mount("/jr", jr);

        // Record journal entries
        fs.CreateFile("/jr/test.txt", 1024);
        jr.SetLastEntryDescription("Create file");
        using (Stream stream = fs.OpenWrite("/jr/test.txt"))
        using (StreamWriter writer = new(stream))
        {
            writer.WriteLine("Hello, world");
        }
        jr.SetLastEntryDescription("Write initial content to file");

        using (Stream stream = fs.OpenWrite("/jr/test.txt", FileMode.Append, FileAccess.Write, FileShare.None))
        using (StreamWriter writer = new(stream))
        {
            writer.WriteLine("Second write :D");
        }
        jr.SetLastEntryDescription("Write to it again");

        Console.WriteLine("=== Actions Start");
        jr.ListJournal()
            .ToList()
            .ForEach(Console.WriteLine);
        Console.WriteLine("=== Actions End");
        Console.WriteLine();

        ReadOnlyCollection<JournalEntry> journal = jr.ListJournal();
        JournalPlayer player = new(journal, mem, true);

        void PrintContents(VPath dir)
        {
            Console.WriteLine($"== Dir List Start ({dir})");
            mem.ListDirectory(dir)
                .ToList()
                .ForEach(Console.WriteLine);
            Console.WriteLine("== Dir List End");
        }

        player.ReplayJournal();

        using(Stream stream = fs.OpenRead("/test.txt"))
        using (StreamReader reader = new(stream))
        {
            Console.WriteLine(reader.ReadToEnd());
        }
    }
}
