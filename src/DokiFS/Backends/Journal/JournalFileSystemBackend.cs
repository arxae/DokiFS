using System.Collections.ObjectModel;
using System.Runtime.InteropServices.ComTypes;
using DokiFS.Interfaces;

namespace DokiFS.Backends.Journal;

public class JournalFileSystemBackend : IFileSystemBackend
{
    public BackendProperties BackendProperties => BackendProperties.Transient;

    readonly List<JournalEntry> journal = [];

    public MountResult OnMount(VPath mountPoint) => MountResult.Accepted;
    public UnmountResult OnUnmount() => UnmountResult.Accepted;

    public bool Exists(VPath path)
    {
        IEnumerable<JournalEntry> results = ReadJournal(path)
            .Where(e => e.ParamStack.Any(p =>
            {
                if (p is VPath vPath)
                {
                    return vPath == path;
                }

                return false;
            }))
            .Where(e => e.JournalAction is not JournalActions.DeleteFile);

        JournalEntry last = results.LastOrDefault();

        return last != null;
    }

    public IVfsEntry GetInfo(VPath path)
    {
        // First check if the path exists according to the journal
        if (Exists(path) == false)
        {
            throw new FileNotFoundException($"Path not found within journal: '{path}'");
        }

        // Find the last create entry for this path to determine its type
        JournalEntry createEntry = journal.LastOrDefault(e =>
            e.JournalAction is JournalActions.CreateFile or JournalActions.CopyDirectory or JournalActions.OpenWrite
            && e.ParamStack.Contains(path));

        if (createEntry == null)
        {
            throw new FileNotFoundException($"Path not found within journal: '{path}'");
        }

        // Determine if it's a file or directory
        VfsEntryType entryType;
        long size = 0;
        if (createEntry.JournalAction is JournalActions.CreateFile or JournalActions.OpenWrite)
        {
            entryType = VfsEntryType.File;
            // CreateFile can potentially have a parameter that includes the size. The size of the file will only
            // be set once it has actually been created, so we are going to nab it here for display purpose
            if (createEntry.JournalAction is JournalActions.CreateFile && createEntry.ParamStack.Length > 1)
                size = (long)createEntry.ParamStack[1];
        }
        else
        {
            entryType = VfsEntryType.Directory;
            size = createEntry.Data.Length;
        }

        // Create a basic VfsEntry with information from the journal
        return new VfsEntry(path, entryType, VfsEntryProperties.Virtual)
        {
            FullPath = path,
            Description = "Journal Entry",
            FromBackend = typeof(JournalFileSystemBackend),
            LastWriteTime = DateTime.UtcNow,
            Size = size
        };
    }

    public IEnumerable<IVfsEntry> ListDirectory(VPath path)
        => ReadJournal(path)
            .Select(e =>
            {
                VfsEntryType entryType;
                long size = 0;
                if (e.JournalAction == JournalActions.CreateFile)
                {
                    entryType = VfsEntryType.File;
                    if (e.ParamStack.Length > 1) size = (long)e.ParamStack[1];
                }
                else
                {
                    entryType = VfsEntryType.Directory;
                    size = 0;
                }

                // Create a basic VfsEntry with information from the journal
                return new VfsEntry(path, entryType, VfsEntryProperties.Virtual)
                {
                    FullPath = path,
                    Description = "Journal Entry",
                    FromBackend = typeof(JournalFileSystemBackend),
                    LastWriteTime = DateTime.UtcNow,
                    Size = size
                };
            });

    public void CreateFile(VPath path, long size = 0)
        => journal.Add(new JournalEntry(JournalActions.CreateFile, path, size));

    public void DeleteFile(VPath path)
        => journal.Add(new JournalEntry(JournalActions.DeleteFile, path));

    public void MoveFile(VPath sourcePath, VPath destinationPath)
        => MoveFile(sourcePath, destinationPath, false);

    public void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite)
        => journal.Add(new JournalEntry(JournalActions.MoveFile, sourcePath, destinationPath, overwrite));

    public void CopyFile(VPath sourcePath, VPath destinationPath)
        => journal.Add(new JournalEntry(JournalActions.CopyFile, sourcePath, destinationPath));

    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite)
        => journal.Add(new JournalEntry(JournalActions.CopyFile, sourcePath, destinationPath, overwrite));

    public Stream OpenRead(VPath path)
    {
        // Find the last journal entry for this path, prioritizing OpenWrite operations
        JournalEntry entry = ReadJournal(path)
            .LastOrDefault(e => e.ParamStack.Contains(path) &&
                (e.JournalAction == JournalActions.OpenWrite || e.JournalAction == JournalActions.CreateFile))
            ?? throw new FileNotFoundException($"File not found in journal: {path.FullPath}");

        // If the entry has data (from a MemoryCapturingStream), return it as a stream
        if (entry.Data != null && entry.Data.Length > 0)
        {
            return new MemoryStream(entry.Data, false);
        }

        // If the entry exists but has no data, return an empty stream
        return new MemoryStream();
    }

    public Stream OpenWrite(VPath path)
    {
        JournalEntry journalEntry = new(JournalActions.OpenWrite, path);
        journal.Add(journalEntry);

        return new MemoryCapturingStream(journalEntry);
    }

    public Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share)
    {
        JournalEntry journalEntry = new(JournalActions.OpenWrite, path, mode, access, share);
        journal.Add(journalEntry);

        return new MemoryCapturingStream(journalEntry);
    }

    public void CreateDirectory(VPath path)
        => journal.Add(new JournalEntry(JournalActions.CreateDirectory, path));

    public void DeleteDirectory(VPath path)
        => DeleteDirectory(path, false);

    public void DeleteDirectory(VPath path, bool recursive)
        => journal.Add(new JournalEntry(JournalActions.DeleteDirectory, path, recursive));

    public void MoveDirectory(VPath sourcePath, VPath destinationPath)
        => journal.Add(new JournalEntry(JournalActions.MoveDirectory, sourcePath, destinationPath));

    public void CopyDirectory(VPath sourcePath, VPath destinationPath)
        => journal.Add(new JournalEntry(JournalActions.CopyDirectory, sourcePath, destinationPath));

    public void ResetJournal() => journal.Clear();

    /// <summary>
    /// Gets a readonly list of all the journal entries
    /// </summary>
    /// <returns>An IEnumerable of all the journal entries</returns>
    public ReadOnlyCollection<JournalEntry> ListJournal() => journal.AsReadOnly();

    /// <summary>
    /// Returns all the journal entries that contain a specific path as a parameter
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>An IEnumerable with all the journal entries</returns>
    public IEnumerable<JournalEntry> ReadJournal(VPath path)
        => journal.Where(e => e.ParamStack.Contains(path));
}
