#nullable enable

using System.Text.Json;
using DokiFS.Interfaces;

namespace DokiFS.Backends.Journal;

/// <summary>
/// A backend that uses a journal to track changes before being applied.
/// </summary>
/// <remarks>
/// If the application quites without commiting the changes, then they will be lost. If changes are not committed,
/// the backend will refuse to be unmounted. In case this backend is only used to record a journal
/// (when the targetBackend is omitted), no commit will be needed
/// Query operations (Exists, GetInfo and ListDirectory) are not supported.
/// </remarks>
public class JournalFileSystemBackend : IFileSystemBackend, ICommit
{
    readonly List<JournalRecord> journalRecords = [];
    readonly Lock journalLock = new();

    readonly IFileSystemBackend? targetBackend;

    /// <summary>
    /// Gets a read-only list of the recorded journal entries.
    /// </summary>
    public IReadOnlyList<JournalRecord> JournalRecords
    {
        get
        {
            lock (journalLock)
            {
                return [.. journalRecords];
            }
        }
    }

    /// <inheritdoc />
    public BackendProperties BackendProperties => BackendProperties.Transient | BackendProperties.RequiresCommit;

    /// <summary>
    /// Initializes a new instance of the <see cref="JournalFileSystemBackend"/> class without a target backend.
    /// </summary>
    /// <remarks>
    /// This constructor creates a journal backend that only records file system operations.
    /// The recorded journal can be retrieved via the <see cref="JournalRecords"/> property or serialized.
    /// Committing changes is not possible with this configuration, and will throw an exception.
    /// </remarks>
    public JournalFileSystemBackend()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JournalFileSystemBackend"/> class with a target backend.
    /// </summary>
    /// <param name="targetBackend">The backend to which journaled operations will be committed to.</param>
    /// <remarks>
    /// This constructor creates a journal backend that records file system operations. The recorded operations can be
    /// applied to the specified <paramref name="targetBackend"/> by calling the <see cref="Commit"/> method.
    /// The backend will prevent unmounting if there are uncommitted changes.
    /// </remarks>
    public JournalFileSystemBackend(IFileSystemBackend targetBackend)
    {
        this.targetBackend = targetBackend;
    }

    /// <inheritdoc />
    public MountResult OnMount(VPath mountPoint) => MountResult.Accepted;

    /// <inheritdoc />
    /// <remarks>
    /// This backend will return <see cref="UnmountResult.UncommittedChanges"/> if there are pending journal records
    /// and a target backend has been configured.
    /// </remarks>
    public UnmountResult OnUnmount() => journalRecords.Count > 0 && targetBackend != null
            ? UnmountResult.UncommittedChanges
            : UnmountResult.Accepted;

    /// <summary>
    /// This operation is not supported by the <see cref="JournalFileSystemBackend"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    public bool Exists(VPath path)
        => throw new NotSupportedException("Query operations not supported on journal backend");

    /// <summary>
    /// This operation is not supported by the <see cref="JournalFileSystemBackend"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    public IVfsEntry GetInfo(VPath path)
        => throw new NotSupportedException("Query operations not supported on journal backend");
    /// <summary>
    /// This operation is not supported by the <see cref="JournalFileSystemBackend"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    public IEnumerable<IVfsEntry> ListDirectory(VPath path)
        => throw new NotSupportedException("Query operations not supported on journal backend");

    /// <summary>
    /// Records the creation of a file in the journal.
    /// </summary>
    /// <param name="path">The path of the file to create.</param>
    /// <param name="size">The initial size of the file in bytes.</param>
    public void CreateFile(VPath path, long size = 0)
    {
        if (targetBackend != null && targetBackend.Exists(path))
            throw new IOException($"A file or directory with the name '{path}' already exists.");

        RecordEntry(new JournalRecord(JournalOperations.CreateFile, p =>
        {
            p.SetSourcePath(path);
            p.SetFileSize(size);
        })
        {
            Description = $"Create file {path} with size {size}"
        });
    }

    /// <summary>
    /// Records the deletion of a file in the journal.
    /// </summary>
    /// <param name="path">The path of the file to delete.</param>
    public void DeleteFile(VPath path)
    {
        if (targetBackend != null)
        {
            if (targetBackend.Exists(path) == false)
            {
                throw new FileNotFoundException($"File not found: '{path}'");
            }

            if (targetBackend.GetInfo(path).EntryType == VfsEntryType.Directory)
            {
                throw new IOException($"Path points to a directory: '{path}'");
            }
        }

        RecordEntry(new JournalRecord(JournalOperations.DeleteFile, p => p.SetSourcePath(path))
        {
            Description = $"Delete file {path}"
        });
    }

    /// <summary>
    /// Records the move of a file in the journal.
    /// </summary>
    /// <param name="sourcePath">The original path of the file.</param>
    /// <param name="destinationPath">The new path of the file.</param>
    public void MoveFile(VPath sourcePath, VPath destinationPath)
        => MoveFile(sourcePath, destinationPath, true);

    /// <summary>
    /// Records the move of a file in the journal.
    /// </summary>
    /// <param name="sourcePath">The original path of the file.</param>
    /// <param name="destinationPath">The new path of the file.</param>
    /// <param name="overwrite">Whether to overwrite the destination file if it exists.</param>
    public void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        if (targetBackend != null)
        {
            if (targetBackend.Exists(sourcePath) == false)
            {
                throw new FileNotFoundException($"Source file not found: '{sourcePath}'");
            }

            if (targetBackend.GetInfo(sourcePath).EntryType == VfsEntryType.Directory)
            {
                throw new IOException($"Source path is a directory: '{sourcePath}'");
            }

            if (overwrite && targetBackend.Exists(destinationPath) == false)
            {
                throw new IOException($"Destination file already exists: '{destinationPath}'");
            }
        }

        RecordEntry(new JournalRecord(JournalOperations.MoveFile, p =>
        {
            p.SetSourcePath(sourcePath);
            p.SetDestinationPath(destinationPath);
            p.SetOverwrite(overwrite);
        })
        {
            Description = $"Move file from {sourcePath} to {destinationPath}"
        });
    }

    /// <summary>
    /// Records the copy of a file in the journal.
    /// </summary>
    /// <param name="sourcePath">The path of the file to copy.</param>
    /// <param name="destinationPath">The path of the destination file.</param>
    public void CopyFile(VPath sourcePath, VPath destinationPath)
        => CopyFile(sourcePath, destinationPath, true);

    /// <summary>
    /// Records the copy of a file in the journal.
    /// </summary>
    /// <param name="sourcePath">The path of the file to copy.</param>
    /// <param name="destinationPath">The path of the destination file.</param>
    /// <param name="overwrite">Whether to overwrite the destination file if it exists.</param>
    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        if (targetBackend != null)
        {
            if (targetBackend.Exists(sourcePath) == false)
            {
                throw new FileNotFoundException($"Source file not found: '{sourcePath}'");
            }

            if (targetBackend.GetInfo(sourcePath).EntryType == VfsEntryType.Directory)
            {
                throw new IOException($"Source path is a directory: '{sourcePath}'");
            }

            if (overwrite && targetBackend.Exists(destinationPath) == false)
            {
                throw new IOException($"Destination file already exists: '{destinationPath}'");
            }
        }

        RecordEntry(new JournalRecord(JournalOperations.CopyFile, p =>
        {
            p.SetSourcePath(sourcePath);
            p.SetDestinationPath(destinationPath);
            p.SetOverwrite(overwrite);
        })
        {
            Description = $"Copy file from {sourcePath} to {destinationPath}"
        });
    }

    /// <summary>
    /// This operation is not supported by the <see cref="JournalFileSystemBackend"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    public Stream OpenRead(VPath path) => throw new NotSupportedException("Read operations not supported on journal backend");

    /// <summary>
    /// Records the opening of a file for writing and returns a stream that captures write operations.
    /// </summary>
    /// <param name="path">The path of the file to open.</param>
    /// <returns>A stream that records write operations to the journal.</returns>
    public Stream OpenWrite(VPath path)
        => OpenWrite(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

    /// <summary>
    /// Records the opening of a file for writing and returns a stream that captures write operations.
    /// </summary>
    /// <param name="path">The path of the file to open.</param>
    /// <param name="mode">The file mode.</param>
    /// <param name="access">The file access.</param>
    /// <param name="share">The file share.</param>
    /// <returns>A stream that records write operations to the journal.</returns>
    /// <remarks>
    /// The returned stream is a <see cref="JournalCapturingStream"/> which will record any writes as
    /// <see cref="JournalOperations.StreamWrite"/> entries in the journal.
    /// </remarks>
    public Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share)
    {
        if (targetBackend != null)
        {
            bool exists = targetBackend.Exists(path);
            if (mode == FileMode.CreateNew && exists)
                throw new IOException($"File '{path}' already exists.");
            if (mode is FileMode.Open or FileMode.Truncate && exists == false)
                throw new FileNotFoundException($"File not found: '{path}'");
        }

        // Determine baseline content (for Append/Open/OpenOrCreate) so final entry holds entire resulting content.
        byte[] baseline = GetBaselineContentForOpen(path, mode);

        return new JournalCapturingStream(path, this, mode, access, share, baseline);
    }

    /// <summary>
    /// Records the creation of a directory in the journal.
    /// </summary>
    /// <param name="path">The path of the directory to create.</param>
    public void CreateDirectory(VPath path)
    {
        if (targetBackend != null && targetBackend.Exists(path))
            throw new IOException($"A file or directory with the name '{path}' already exists.");

        RecordEntry(new JournalRecord(JournalOperations.CreateDirectory, p => p.SetSourcePath(path))
        {
            Description = $"Create directory {path}"
        });
    }

    /// <summary>
    /// Records the deletion of a directory in the journal.
    /// </summary>
    /// <param name="path">The path of the directory to delete.</param>
    public void DeleteDirectory(VPath path) => DeleteDirectory(path, false);

    /// <summary>
    /// Records the deletion of a directory in the journal.
    /// </summary>
    /// <param name="path">The path of the directory to delete.</param>
    /// <param name="recursive">Whether to delete subdirectories and files.</param>
    public void DeleteDirectory(VPath path, bool recursive)
    {
        if (targetBackend != null)
        {
            if (targetBackend.Exists(path) == false)
            {
                throw new DirectoryNotFoundException($"Directory not found: '{path}'");
            }

            IVfsEntry info = targetBackend.GetInfo(path);
            if (info.EntryType == VfsEntryType.File)
            {
                throw new IOException($"Path points to a file: '{path}'");
            }

            if (recursive == false && targetBackend.ListDirectory(path).Any())
            {
                throw new IOException($"Directory is not empty: '{path}'");
            }
        }

        RecordEntry(new JournalRecord(JournalOperations.DeleteDirectory, p =>
        {
            p.SetSourcePath(path);
            p.SetRecursive(recursive);
        })
        {
            Description = $"Delete directory {path} (recursive: {recursive})"
        });
    }

    /// <summary>
    /// Records the move of a directory in the journal.
    /// </summary>
    /// <param name="sourcePath">The original path of the directory.</param>
    /// <param name="destinationPath">The new path of the directory.</param>
    public void MoveDirectory(VPath sourcePath, VPath destinationPath)
    {
        if (targetBackend != null)
        {
            if (targetBackend.Exists(sourcePath) == false)
            {
                throw new DirectoryNotFoundException($"Source directory not found: '{sourcePath}'");
            }

            if (targetBackend.GetInfo(sourcePath).EntryType != VfsEntryType.Directory)
            {
                throw new IOException($"Source path is not a directory: '{sourcePath}'");
            }

            if (targetBackend.Exists(destinationPath))
            {
                throw new IOException($"Destination directory already exists: '{destinationPath}'");
            }

            if (destinationPath.StartsWith(sourcePath))
            {
                throw new IOException("Cannot move a directory into itself.");
            }
        }

        RecordEntry(new JournalRecord(JournalOperations.MoveDirectory, p =>
        {
            p.SetSourcePath(sourcePath);
            p.SetDestinationPath(destinationPath);
            p.SetOverwrite(true);
        })
        {
            Description = $"Move directory from {sourcePath} to {destinationPath}"
        });
    }

    /// <summary>
    /// Records the copy of a directory in the journal.
    /// </summary>
    /// <param name="sourcePath">The path of the directory to copy.</param>
    /// <param name="destinationPath">The path of the destination directory.</param>
    public void CopyDirectory(VPath sourcePath, VPath destinationPath)
    {
        if (targetBackend != null)
        {
            if (targetBackend.Exists(sourcePath) == false)
            {
                throw new DirectoryNotFoundException($"Source directory not found: '{sourcePath}'");
            }

            if (targetBackend.GetInfo(sourcePath).EntryType != VfsEntryType.Directory)
            {
                throw new IOException($"Source path is not a directory: '{sourcePath}'");
            }

            if (targetBackend.Exists(destinationPath))
            {
                throw new IOException($"Destination directory already exists: '{destinationPath}'");
            }

            if (destinationPath.StartsWith(sourcePath))
            {
                throw new IOException("Cannot copy a directory into itself.");
            }
        }

        RecordEntry(new JournalRecord(JournalOperations.CopyDirectory, p =>
        {
            p.SetSourcePath(sourcePath);
            p.SetDestinationPath(destinationPath);
            p.SetOverwrite(true);
        })
        {
            Description = $"Copy directory from {sourcePath} to {destinationPath}"
        });
    }

    /// <summary>
    /// Commits all recorded journal entries to the target backend.
    /// </summary>
    /// <remarks>
    /// This method replays all recorded operations on the <c>targetBackend</c>.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown if no target backend was specified during construction.</exception>
    public void Commit()
    {
        if (targetBackend == null)
        {
            throw new ArgumentException("No target backend specified, committing changes is not possible");
        }

        lock (journalLock)
        {
            JournalPlayer.Replay(this, this);
        }
    }

    /// <summary>
    /// Discards all recorded journal entries.
    /// </summary>
    public void Discard()
    {
        lock (journalLock)
        {
            journalRecords.Clear();
        }
    }

    internal void RecordCompletedWrite(VPath path, FileMode mode, FileAccess access, FileShare share, byte[] data)
    {
        ContentReference contentRef = new()
        {
            ContentId = Guid.NewGuid().ToString(),
            Type = ContentType.BaseContent,
            Size = data.Length,
            StreamOffset = 0,
            Length = data.Length,
            Data = data
        };

        RecordEntry(new JournalRecord(JournalOperations.OpenWrite, p =>
        {
            p.SetSourcePath(path);
            p.SetFileMode(mode);
            p.SetFileAccess(access);
            p.SetFileShare(share);
        })
        {
            Content = contentRef,
            Description = $"Write {path} ({data.Length} bytes, mode={mode})"
        });
    }

    void RecordEntry(JournalRecord entry)
    {
        lock (journalLock)
        {
            journalRecords.Add(entry);
        }
    }

    byte[] GetBaselineContentForOpen(VPath path, FileMode mode)
    {
        lock (journalLock)
        {
            bool existsInJournal = HasExistingJournalFile(path, out byte[]? journalContent);

            bool existsInBackend = false;
            byte[]? backendContent = null;

            if (targetBackend != null)
            {
                try
                {
                    existsInBackend = targetBackend.Exists(path);
                    if (existsInBackend)
                    {
                        using Stream s = targetBackend.OpenRead(path);
                        using MemoryStream ms = new();
                        s.CopyTo(ms);
                        backendContent = ms.ToArray();
                    }
                }
                catch
                {
                    // If backend read not supported, ignore.
                }
            }

            bool exists = existsInJournal || existsInBackend;

            return mode switch
            {
                FileMode.CreateNew => [],

                FileMode.Create => [], // overwrite if exists

                FileMode.Truncate => exists
                    ? []
                    : throw new FileNotFoundException($"File not found (truncate): '{path}'"),

                FileMode.Open => exists
                    ? (journalContent ?? backendContent ?? [])
                    : throw new FileNotFoundException($"File not found: '{path}'"),

                FileMode.OpenOrCreate => exists
                    ? (journalContent ?? backendContent ?? [])
                    : [],

                FileMode.Append => exists
                    ? (journalContent ?? backendContent ?? [])
                    : [],

                _ => []
            };
        }
    }

    bool HasExistingJournalFile(VPath path, out byte[]? content)
    {
        // Look for last OpenWrite entry with data
        JournalRecord? last = journalRecords
            .LastOrDefault(r => r.Operation == JournalOperations.OpenWrite &&
                        r.Parameters.GetSourcePath() == path &&
                        r.Content?.Data != null);

        if (last != null)
        {
            content = last.Content!.Data!;
            return true;
        }

        content = null;
        return false;
    }

    /// <summary>
    /// Serializes the current journal to a JSON string.
    /// </summary>
    /// <returns>A JSON string representation of the journal.</returns>
    public string SerializeJournal()
    {
        lock (journalLock)
        {
            return JsonSerializer.Serialize(journalRecords, JournalSerializerOptions.GetDefault());
        }
    }

    /// <summary>
    /// Serializes the current journal and saves it to a file.
    /// </summary>
    /// <param name="filePath">The path of the file to save the journal to.</param>
    public void SaveJournal(string filePath)
    {
        string json = SerializeJournal();
        File.WriteAllText(filePath, json);
    }
}
