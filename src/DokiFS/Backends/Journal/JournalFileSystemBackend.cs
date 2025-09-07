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
    readonly Dictionary<string, Stream> openStreams = [];
    readonly Lock journalLock = new();

    readonly IFileSystemBackend? targetBackend;

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
    public bool Exists(VPath path) => throw new NotSupportedException("Query operations not supported on journal backend");
    /// <summary>
    /// This operation is not supported by the <see cref="JournalFileSystemBackend"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    public IVfsEntry GetInfo(VPath path) => throw new NotSupportedException("Query operations not supported on journal backend");
    /// <summary>
    /// This operation is not supported by the <see cref="JournalFileSystemBackend"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">This operation is not supported.</exception>
    public IEnumerable<IVfsEntry> ListDirectory(VPath path) => throw new NotSupportedException("Query operations not supported on journal backend");

    /// <summary>
    /// Records the creation of a file in the journal.
    /// </summary>
    /// <param name="path">The path of the file to create.</param>
    /// <param name="size">The initial size of the file in bytes.</param>
    public void CreateFile(VPath path, long size = 0)
    {
        if (targetBackend != null && targetBackend.Exists(path))
        {
            throw new IOException($"A file or directory with the name '{path}' already exists.");
        }

        RecordEntry(new JournalRecord(JournalOperations.CreateFile, parameters =>
        {
            parameters.SetSourcePath(path);
            parameters.SetFileSize(size);
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

        RecordEntry(new JournalRecord(JournalOperations.DeleteFile, parameters =>
        {
            parameters.SetSourcePath(path);
        })
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

            if (overwrite == false && targetBackend.Exists(destinationPath))
            {
                throw new IOException($"Destination file already exists: '{destinationPath}'");
            }
        }

        RecordEntry(new JournalRecord(JournalOperations.MoveFile, parameters =>
        {
            parameters.SetSourcePath(sourcePath);
            parameters.SetDestinationPath(destinationPath);
            parameters.SetOverwrite(overwrite);
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

            if (overwrite == false && targetBackend.Exists(destinationPath))
            {
                throw new IOException($"Destination file already exists: '{destinationPath}'");
            }
        }

        RecordEntry(new JournalRecord(JournalOperations.CopyFile, parameters =>
        {
            parameters.SetSourcePath(sourcePath);
            parameters.SetDestinationPath(destinationPath);
            parameters.SetOverwrite(overwrite);
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
            {
                throw new IOException($"File '{path}' already exists.");
            }

            if ((mode == FileMode.Open || mode == FileMode.Truncate) && !exists)
            {
                throw new FileNotFoundException($"File not found: '{path}'");
            }
        }

        // Record the stream opening
        RecordEntry(new JournalRecord(JournalOperations.OpenWrite, parameters =>
        {
            parameters.SetSourcePath(path);
            parameters.SetFileMode(mode);
            parameters.SetFileAccess(access);
            parameters.SetFileShare(share);
        })
        {
            Description = $"Open write stream for {path}"
        });

        // Create a capturing stream that will record all writes
        JournalCapturingStream stream = new(path, this);

        lock (journalLock)
        {
            openStreams[path.ToString()] = stream;
        }

        return stream;
    }

    /// <summary>
    /// Records the creation of a directory in the journal.
    /// </summary>
    /// <param name="path">The path of the directory to create.</param>
    public void CreateDirectory(VPath path)
    {
        if (targetBackend != null && targetBackend.Exists(path))
        {
            throw new IOException($"A file or directory with the name '{path}' already exists.");
        }

        RecordEntry(new JournalRecord(JournalOperations.CreateDirectory, parameters =>
        {
            parameters.SetSourcePath(path);
        })
        {
            Description = $"Create directory {path}"
        });
    }

    /// <summary>
    /// Records the deletion of a directory in the journal.
    /// </summary>
    /// <param name="path">The path of the directory to delete.</param>
    public void DeleteDirectory(VPath path)
        => DeleteDirectory(path, false);

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

        RecordEntry(new JournalRecord(JournalOperations.DeleteDirectory, parameters =>
        {
            parameters.SetSourcePath(path);
            parameters.SetRecursive(recursive);
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

        RecordEntry(new JournalRecord(JournalOperations.MoveDirectory, parameters =>
        {
            parameters.SetSourcePath(sourcePath);
            parameters.SetDestinationPath(destinationPath);
            parameters.SetOverwrite(true);
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

        RecordEntry(new JournalRecord(JournalOperations.CopyDirectory, parameters =>
        {
            parameters.SetSourcePath(sourcePath);
            parameters.SetDestinationPath(destinationPath);
            parameters.SetOverwrite(true);
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
            throw new ArgumentException("No target backend specified, commiting changes is not possibe");
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

    // --- Internal methods ---
    internal void RecordStreamWrite(VPath path, long position, byte[] data)
    {
        ContentReference contentRef = new()
        {
            ContentId = Guid.NewGuid().ToString(),
            Type = ContentType.Partial,
            Size = data.Length,
            StreamOffset = position,
            Length = data.Length,
            Data = data
        };

        RecordEntry(new JournalRecord(JournalOperations.StreamWrite, parameters =>
        {
            parameters.SetSourcePath(path);
            parameters.SetStreamPosition(position);
        })
        {
            Content = contentRef,
            Description = $"Stream write to {path} at position {position}, {data.Length} bytes"
        });
    }

    internal void RecordStreamClose(VPath path)
    {
        RecordEntry(new JournalRecord(JournalOperations.CloseWriteStream, parameters =>
        {
            parameters.SetSourcePath(path);
        })
        {
            Description = $"Close write stream for {path}"
        });

        lock (journalLock)
        {
            openStreams.Remove(path.ToString());
        }
    }

    void RecordEntry(JournalRecord entry)
    {
        lock (journalLock)
        {
            journalRecords.Add(entry);
        }
    }

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

    /// <summary>
    /// Reconstructs the content of a file by applying all recorded stream write operations.
    /// </summary>
    /// <param name="path">The path of the file to reconstruct.</param>
    /// <returns>A byte array representing the file's content.</returns>
    /// <remarks>
    /// This method is useful for inspecting the state of a file within the journal without committing.
    /// It only considers <see cref="JournalOperations.StreamWrite"/> operations for the given <paramref name="path"/>.
    /// Other file operations like create or delete are not considered.
    /// </remarks>
    public byte[] GetFileContent(VPath path)
    {
        lock (journalLock)
        {
            List<JournalRecord> writeOperations = [.. journalRecords
                .Where(r => r.Parameters.GetSourcePath() == path && r.Operation == JournalOperations.StreamWrite)
                .OrderBy(r => r.Parameters.GetStreamPosition())];

            if (writeOperations.Count == 0)
                return [];

            // Reconstruct file content from stream writes
            using MemoryStream memoryStream = new();
            foreach (JournalRecord op in writeOperations)
            {
                if (op.Content?.Data == null) continue;

                long position = op.Parameters.GetStreamPosition();
                memoryStream.Position = position;
                memoryStream.Write(op.Content.Data);
            }

            return memoryStream.ToArray();
        }
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
