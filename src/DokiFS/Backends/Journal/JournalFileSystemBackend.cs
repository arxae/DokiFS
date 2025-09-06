using System.Text.Json;
using DokiFS.Interfaces;

namespace DokiFS.Backends.Journal;

public class JournalFileSystemBackend : IFileSystemBackend
{
    readonly List<JournalRecord> journalRecords = [];
    readonly Dictionary<string, Stream> openStreams = [];
    readonly Lock journalLock = new();

    readonly JsonSerializerOptions serializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public BackendProperties BackendProperties => BackendProperties.Transient | BackendProperties.RequiresCommit;

    public JournalFileSystemBackend() { }

    public MountResult OnMount(VPath mountPoint) => MountResult.Accepted;
    public UnmountResult OnUnmount() => UnmountResult.Accepted;

    public bool Exists(VPath path) => throw new NotSupportedException("Query operations not supported on journal backend");
    public IVfsEntry GetInfo(VPath path) => throw new NotSupportedException("Query operations not supported on journal backend");
    public IEnumerable<IVfsEntry> ListDirectory(VPath path) => throw new NotSupportedException("Query operations not supported on journal backend");

    public void CreateFile(VPath path, long size = 0)
    {
        JournalParameters parameters = new();
        parameters.SetFileSize(size);

        RecordEntry(new JournalRecord(JournalOperations.CreateFile, path)
        {
            Parameters = parameters,
            Description = $"Create file {path} with size {size}"
        });
    }

    public void DeleteFile(VPath path)
    {
        RecordEntry(new JournalRecord(JournalOperations.DeleteFile, path)
        {
            Description = $"Delete file {path}"
        });
    }

    public void MoveFile(VPath sourcePath, VPath destinationPath)
        => MoveFile(sourcePath, destinationPath, true);

    public void MoveFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        JournalParameters parameters = new();
        parameters.SetOverwrite(overwrite);

        RecordEntry(new JournalRecord(JournalOperations.MoveFile, sourcePath)
        {
            SecondaryPath = destinationPath,
            Parameters = parameters,
            Description = $"Move file from {sourcePath} to {destinationPath}"
        });
    }

    public void CopyFile(VPath sourcePath, VPath destinationPath)
        => CopyFile(sourcePath, destinationPath, true);

    public void CopyFile(VPath sourcePath, VPath destinationPath, bool overwrite)
    {
        JournalParameters parameters = new();
        parameters.SetOverwrite(overwrite);

        RecordEntry(new JournalRecord(JournalOperations.CopyFile, sourcePath)
        {
            SecondaryPath = destinationPath,
            Parameters = parameters,
            Description = $"Copy file from {sourcePath} to {destinationPath}"
        });
    }

    public Stream OpenRead(VPath path) => throw new NotSupportedException("Read operations not supported on journal backend");

    public Stream OpenWrite(VPath path)
        => OpenWrite(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

    public Stream OpenWrite(VPath path, FileMode mode, FileAccess access, FileShare share)
    {
        JournalParameters parameters = new();
        parameters.SetFileMode(mode);
        parameters.SetFileAccess(access);
        parameters.SetFileShare(share);

        // Record the stream opening
        RecordEntry(new JournalRecord(JournalOperations.OpenWrite, path)
        {
            Parameters = parameters,
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

    public void CreateDirectory(VPath path)
    {
        RecordEntry(new JournalRecord(JournalOperations.CreateDirectory, path)
        {
            Description = $"Create directory {path}"
        });
    }

    public void DeleteDirectory(VPath path)
        => DeleteDirectory(path, false);

    public void DeleteDirectory(VPath path, bool recursive)
    {
        JournalParameters parameters = new();
        parameters.SetRecursive(recursive);

        RecordEntry(new JournalRecord(JournalOperations.DeleteDirectory, path)
        {
            Parameters = parameters,
            Description = $"Delete directory {path} (recursive: {recursive})"
        });
    }

    public void MoveDirectory(VPath sourcePath, VPath destinationPath)
    {
        JournalParameters parameters = new();
        parameters.SetOverwrite(true); // Default to true for consistency

        RecordEntry(new JournalRecord(JournalOperations.MoveDirectory, sourcePath)
        {
            SecondaryPath = destinationPath,
            Parameters = parameters,
            Description = $"Move directory from {sourcePath} to {destinationPath}"
        });
    }

    public void CopyDirectory(VPath sourcePath, VPath destinationPath)
    {
        JournalParameters parameters = new();
        parameters.SetOverwrite(true); // Default to true for consistency

        RecordEntry(new JournalRecord(JournalOperations.CopyDirectory, sourcePath)
        {
            SecondaryPath = destinationPath,
            Parameters = parameters,
            Description = $"Copy directory from {sourcePath} to {destinationPath}"
        });
    }

    // --- Internal methods ---
    internal void RecordStreamWrite(VPath path, long position, byte[] data)
    {
        JournalParameters parameters = new();
        parameters.SetStreamPosition(position);

        // Store the actual data in the content reference
        ContentReference contentRef = new()
        {
            ContentId = Guid.NewGuid().ToString(),
            Type = ContentType.Partial,
            Size = data.Length,
            StreamOffset = position,
            Length = data.Length,
            Data = data
        };

        RecordEntry(new JournalRecord(JournalOperations.StreamWrite, path)
        {
            Parameters = parameters,
            Content = contentRef,
            Description = $"Stream write to {path} at position {position}, {data.Length} bytes"
        });
    }

    internal void RecordStreamClose(VPath path)
    {
        RecordEntry(new JournalRecord(JournalOperations.CloseWriteStream, path)
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

    // Add a public method to access journal records
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

    // Add a method to reconstruct file content
    public byte[] GetFileContent(VPath path)
    {
        lock (journalLock)
        {
            List<JournalRecord> writeOperations = [.. journalRecords
                .Where(r => r.Path == path && r.Operation == JournalOperations.StreamWrite)
                .OrderBy(r => r.Parameters.GetStreamPosition())];

            if (writeOperations.Count == 0)
                return [];

            // Reconstruct file content from stream writes
            using MemoryStream memoryStream = new();
            foreach (JournalRecord op in writeOperations)
            {
                if (op.Content?.Data != null)
                {
                    long position = op.Parameters.GetStreamPosition();
                    memoryStream.Position = position;
                    memoryStream.Write(op.Content.Data);
                }
            }

            return memoryStream.ToArray();
        }
    }

    public string SerializeJournal()
    {
        lock (journalLock)
        {
            return JsonSerializer.Serialize(journalRecords, serializerOptions);
        }
    }

    public void SaveJournal(string filePath)
    {
        string json = SerializeJournal();
        File.WriteAllText(filePath, json);
    }
}

/// <summary>
/// A stream implementation that captures all write operations and records them in the journal
/// </summary>
internal sealed class JournalCapturingStream : Stream
{
    readonly VPath path;
    readonly JournalFileSystemBackend backend;
    readonly MemoryStream buffer;
    long position;
    bool disposed;

    public JournalCapturingStream(VPath path, JournalFileSystemBackend backend)
    {
        this.path = path;
        this.backend = backend;
        buffer = new MemoryStream();
        position = 0;
    }

    public override bool CanRead => false;
    public override bool CanSeek => true;
    public override bool CanWrite => true;
    public override long Length => buffer.Length;

    public override long Position
    {
        get => position;
        set
        {
            position = value;
            if (buffer.Length < value)
            {
                buffer.SetLength(value);
            }
        }
    }

    public override void Flush() => buffer.Flush();

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("Read operations not supported on write-only journal stream");

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                Position = offset;
                break;
            case SeekOrigin.Current:
                Position += offset;
                break;
            case SeekOrigin.End:
                Position = Length + offset;
                break;
            default:
                Position = 0;
                break;
        }
        return Position;
    }

    public override void SetLength(long value) => buffer.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
    {
        // Ensure buffer is large enough
        if (position + count > this.buffer.Length)
        {
            this.buffer.SetLength(position + count);
        }

        // Write to our internal buffer
        this.buffer.Position = position;
        this.buffer.Write(buffer, offset, count);

        // Record the write operation in the journal
        byte[] data = new byte[count];
        Array.Copy(buffer, offset, data, 0, count);
        backend.RecordStreamWrite(path, position, data);

        position += count;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposed == false && disposing)
        {
            backend.RecordStreamClose(path);
            buffer.Dispose();
            disposed = true;
        }

        base.Dispose(disposing);
    }
}
