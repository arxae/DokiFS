using System.Collections.ObjectModel;
using DokiFS.Interfaces;

namespace DokiFS.Backends.Journal;

/// <summary>
/// A backend that uses a journal to track changes before being applied.
/// </summary>
/// <remarks>
/// The id of the journal entries is an int, and not tracked between sessions. If the application quites without
/// commiting the changes, then they will be lost. If changes are not committed, the backend will refuse to be unmounted.
/// In case this backend is only used to record a journal (when the targetBackend is omitted), no commit will be needed,
/// although it's recommended to use this backend directly
/// </remarks>
public class JournalFileSystemBackend : IFileSystemBackend, ICommit
{
    public BackendProperties BackendProperties => BackendProperties.Transient | BackendProperties.RequiresCommit;

    readonly List<JournalEntry> journal = [];
    readonly IFileSystemBackend targetBackend;

    int currentEntryId;

    /// <summary>
    /// Instantiate without a target backend. Unable to commit changes to a backend. You can extract a journal from
    /// this backend to use with the JournalPlayer and apply it later.
    /// </summary>
    public JournalFileSystemBackend() { }

    /// <summary>
    /// Instantiate with a target backend. The backend will not allow itself to be unmounted without commiting the
    /// changes of the journal
    /// </summary>
    /// <param name="targetBackend"></param>
    public JournalFileSystemBackend(IFileSystemBackend targetBackend)
    {
        this.targetBackend = targetBackend;
    }

    public MountResult OnMount(VPath mountPoint) => MountResult.Accepted;

    // Refuse to be unmounted when there are still pending journal entries. If the targetBackend is null, then this
    // instance was probably just used to record a journal, allow unmounting.
    public UnmountResult OnUnmount()
        => journal.Count > 0 && targetBackend != null
            ? UnmountResult.UncommittedChanges
            : UnmountResult.Accepted;

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
            && e.ParamStack.Contains(path))
        ?? throw new FileNotFoundException($"Path not found within journal: '{path}'");

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
        => journal.Add(new JournalEntry(NextId(), JournalActions.CreateFile, path, size));

    public void DeleteFile(VPath path)
    {
        JournalEntry entry = new(NextId(), JournalActions.DeleteFile, path);

        // Capture file contents for undo
        if (Exists(path))
        {
            using Stream stream = OpenRead(path);
            if (stream.Length > 0)
            {
                entry.UndoData = new byte[stream.Length];
                stream.ReadExactly(entry.UndoData, 0, (int)stream.Length);
            }
        }

        journal.Add(entry);
    }

    public void MoveFile(VPath sourcePath, VPath destinationPath)
        => MoveFile(sourcePath, destinationPath, false);

    public void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        JournalEntry entry = new(NextId(), JournalActions.MoveFile, sourcePath, destinationPath, overwrite);

        // If overwriting, capture destination file content for undo
        if (overwrite && Exists(destinationPath))
        {
            using Stream stream = OpenRead(destinationPath);
            if (stream.Length > 0)
            {
                entry.UndoData = new byte[stream.Length];
                stream.ReadExactly(entry.UndoData, 0, (int)stream.Length);
            }
        }

        journal.Add(entry);
    }

    public void CopyFile(VPath sourcePath, VPath destinationPath)
        => CopyFile(sourcePath, destinationPath, false);

    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        JournalEntry entry = new(NextId(), JournalActions.CopyFile, sourcePath, destinationPath, overwrite);

        // If overwriting, capture destination file content for undo
        if (overwrite && Exists(destinationPath))
        {
            using Stream stream = OpenRead(destinationPath);
            if (stream.Length > 0)
            {
                entry.UndoData = new byte[stream.Length];
                stream.ReadExactly(entry.UndoData, 0, (int)stream.Length);
            }
        }

        journal.Add(entry);
    }

    public Stream OpenRead(VPath path)
    {
        // Find the last journal entry for this path, prioritizing OpenWrite operations
        JournalEntry entry = ReadJournal(path)
            .LastOrDefault(e => e.ParamStack.Contains(path) &&
                (e.JournalAction == JournalActions.OpenWrite || e.JournalAction == JournalActions.CreateFile))
            ?? throw new FileNotFoundException($"File not found in journal: {path.FullPath}");

        // If the entry has data (from a MemoryCapturingStream), return it as a stream
        if (entry.Data is { Length: > 0 })
        {
            return new MemoryStream(entry.Data, false);
        }

        // If the entry exists but has no data, return an empty stream
        return new MemoryStream();
    }

    public Stream OpenWrite(VPath path)
        => OpenWrite(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

    public Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share)
    {
        JournalEntry journalEntry = new(NextId(), JournalActions.OpenWrite, path, mode, access, share);

        // Capture original file content for undo
        if (Exists(path))
        {
            using Stream stream = OpenRead(path);
            if (stream.Length > 0)
            {
                journalEntry.UndoData = new byte[stream.Length];
                stream.ReadExactly(journalEntry.UndoData, 0, (int)stream.Length);
            }
        }

        journal.Add(journalEntry);
        return new MemoryCapturingStream(journalEntry);
    }

    public void CreateDirectory(VPath path)
        => journal.Add(new JournalEntry(NextId(), JournalActions.CreateDirectory, path));

    public void DeleteDirectory(VPath path)
        => DeleteDirectory(path, false);

    public void DeleteDirectory(VPath path, bool recursive)
    {
        // For directories, capturing the content for undo is more complex
        // This would ideally capture the entire directory structure
        JournalEntry entry = new(NextId(), JournalActions.DeleteDirectory, path, recursive);
        journal.Add(entry);
    }

    public void MoveDirectory(VPath sourcePath, VPath destinationPath)
        => journal.Add(new JournalEntry(NextId(), JournalActions.MoveDirectory, sourcePath, destinationPath));

    public void CopyDirectory(VPath sourcePath, VPath destinationPath)
        => journal.Add(new JournalEntry(NextId(), JournalActions.CopyDirectory, sourcePath, destinationPath));

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

    /// <summary>
    /// Creates and returns the next unique journal entry ID.
    /// </summary>
    /// <returns></returns>
    int NextId() => currentEntryId++;

    public void SetLastEntryDescription(string description)
    {
        if (journal.Count > 0)
        {
            journal[^1].Description = description;
        }
    }

    /// <summary>
    /// Commits the current journal to the filesystem
    /// </summary>
    public void Commit()
    {
        if (targetBackend == null)
        {
            throw new InvalidOperationException("No target backend specified for committing the journal");
        }

        using JournalPlayer player = new(ListJournal(), targetBackend, false);
        player.ReplayJournal();
    }

    public void Discard() => journal.Clear();
}
