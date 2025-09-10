using System.Text.Json;
using DokiFS.Interfaces;

namespace DokiFS.Backends.Journal;

/// <summary>
/// Replays journal records on a target backend
/// </summary>
public static class JournalPlayer
{

    /// <summary>
    /// Replays all journal records on the target backend
    /// </summary>
    /// <param name="journalRecords">The journal records to replay</param>
    /// <exception cref="InvalidOperationException">When an operation cannot be replayed</exception>
    public static void Replay(IEnumerable<JournalRecord> journalRecords, IFileSystemBackend targetBackend)
    {
        List<JournalRecord> records = [.. journalRecords.OrderBy(r => r.Timestamp)];

        for (int i = 0; i < records.Count; i++)
        {
            try
            {
                ReplayRecord(records[i], targetBackend);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to replay journal record {i + 1}/{records.Count} (ID: {records[i].Id}, Operation: {records[i].Operation}): {ex.Message}",
                    ex);
            }
        }
    }

    /// <summary>
    /// Replays journal records from a journal backend
    /// </summary>
    /// <param name="journalBackend">The journal backend containing the records</param>
    public static void Replay(JournalFileSystemBackend journalBackend, IFileSystemBackend targetBackend)
        => Replay(journalBackend.JournalRecords, targetBackend);

    public static void Replay(string filePath, IFileSystemBackend targetBackend)
    {
        if (File.Exists(filePath) == false)
        {
            throw new FileNotFoundException("Journal file not found", filePath);
        }

        string json = File.ReadAllText(filePath);
        IEnumerable<JournalRecord> records = JsonSerializer.Deserialize<IEnumerable<JournalRecord>>(json, JournalSerializerOptions.GetDefault())
            ?? throw new InvalidOperationException("Failed to deserialize journal records");

        Replay(records, targetBackend);
    }

    static void ReplayRecord(JournalRecord record, IFileSystemBackend targetBackend)
    {
        switch (record.Operation)
        {
            case JournalOperations.CreateFile:
                ReplayCreateFile(record, targetBackend);
                break;

            case JournalOperations.DeleteFile:
                ReplayDeleteFile(record, targetBackend);
                break;

            case JournalOperations.MoveFile:
                ReplayMoveFile(record, targetBackend);
                break;

            case JournalOperations.CopyFile:
                ReplayCopyFile(record, targetBackend);
                break;

            case JournalOperations.CreateDirectory:
                ReplayCreateDirectory(record, targetBackend);
                break;

            case JournalOperations.DeleteDirectory:
                ReplayDeleteDirectory(record, targetBackend);
                break;

            case JournalOperations.MoveDirectory:
                ReplayMoveDirectory(record, targetBackend);
                break;

            case JournalOperations.CopyDirectory:
                ReplayCopyDirectory(record, targetBackend);
                break;

            case JournalOperations.OpenWrite:
                // New format: a single OpenWrite record with full file content (if Content present)
                if (record.Content?.Data != null)
                    ReplayFullOpenWrite(record, targetBackend);
                break;

            case JournalOperations.StreamWrite:
                // Legacy multi-record write support
                ReplayStreamWrite(record, targetBackend);
                break;

            case JournalOperations.CloseWriteStream:
                // Legacy close marker (no action)
                break;
            default:
                throw new NotSupportedException($"Unknown journal operation: {record.Operation}");
        }
    }

    static void ReplayCreateFile(JournalRecord record, IFileSystemBackend targetBackend)
    {
        long size = record.Parameters.GetFileSize();
        targetBackend.CreateFile(record.Parameters.GetSourcePath(), size);
    }

    static void ReplayDeleteFile(JournalRecord record, IFileSystemBackend targetBackend)
        => targetBackend.DeleteFile(record.Parameters.GetSourcePath());

    static void ReplayMoveFile(JournalRecord record, IFileSystemBackend targetBackend)
    {
        if (record.Parameters.GetDestinationPath() == null)
            throw new InvalidOperationException("Move operation requires a destination path");

        bool overwrite = record.Parameters.GetOverwrite();
        targetBackend.MoveFile(record.Parameters.GetSourcePath(), record.Parameters.GetDestinationPath(), overwrite);
    }

    static void ReplayCopyFile(JournalRecord record, IFileSystemBackend targetBackend)
    {
        if (record.Parameters.GetDestinationPath() == null)
            throw new InvalidOperationException("Copy operation requires a destination path");

        bool overwrite = record.Parameters.GetOverwrite();
        targetBackend.CopyFile(record.Parameters.GetSourcePath(), record.Parameters.GetDestinationPath(), overwrite);
    }

    static void ReplayCreateDirectory(JournalRecord record, IFileSystemBackend targetBackend)
        => targetBackend.CreateDirectory(record.Parameters.GetSourcePath());

    static void ReplayDeleteDirectory(JournalRecord record, IFileSystemBackend targetBackend)
    {
        bool recursive = record.Parameters.GetRecursive();
        targetBackend.DeleteDirectory(record.Parameters.GetSourcePath(), recursive);
    }

    static void ReplayMoveDirectory(JournalRecord record, IFileSystemBackend targetBackend)
    {
        if (record.Parameters.GetDestinationPath() == null)
            throw new InvalidOperationException("Move directory operation requires a destination path");

        targetBackend.MoveDirectory(record.Parameters.GetSourcePath(), record.Parameters.GetDestinationPath());
    }

    static void ReplayCopyDirectory(JournalRecord record, IFileSystemBackend targetBackend)
    {
        if (record.Parameters.GetDestinationPath() == null)
            throw new InvalidOperationException("Copy directory operation requires a destination path");

        targetBackend.CopyDirectory(record.Parameters.GetSourcePath(), record.Parameters.GetDestinationPath());
    }

    static void ReplayStreamWrite(JournalRecord record, IFileSystemBackend targetBackend)
    {
        if (record.Content?.Data == null)
            return; // Nothing to write

        FileMode fileMode = record.Parameters.GetFileMode();
        FileAccess fileAccess = record.Parameters.GetFileAccess();
        FileShare fileShare = record.Parameters.GetFileShare();

        // If any of the filemodes are 0, use the defaults
        if (fileMode == 0) fileMode = FileMode.OpenOrCreate;
        if (fileAccess == 0) fileAccess = FileAccess.Write;
        if (fileShare == 0) fileShare = FileShare.None;

        // For stream writes, we need to reconstruct the file by applying all writes
        // This is a simplified approach - open the file and write at the specified position
        using Stream stream = targetBackend.OpenWrite(record.Parameters.GetSourcePath(), fileMode, fileAccess, fileShare);

        long position = record.Parameters.GetStreamPosition();
        stream.Position = position;
        stream.Write(record.Content.Data);
    }

    // New: replay a full-content OpenWrite entry (single-entry write model)
    static void ReplayFullOpenWrite(JournalRecord record, IFileSystemBackend targetBackend)
    {
        VPath path = record.Parameters.GetSourcePath();
        byte[] data = record.Content!.Data!;

        FileMode fileMode = record.Parameters.GetFileMode();
        FileAccess fileAccess = record.Parameters.GetFileAccess();
        FileShare fileShare = record.Parameters.GetFileShare();

        if (fileMode == 0) fileMode = FileMode.OpenOrCreate;
        if (fileAccess == 0) fileAccess = FileAccess.Write;
        if (fileShare == 0) fileShare = FileShare.None;

        // Preserve CreateNew semantics strictly
        if (fileMode == FileMode.Open && !targetBackend.Exists(path))
            throw new FileNotFoundException($"Replay expected existing file (Open): {path}");

        // We have the final full content; easiest is to (re)create/truncate the file regardless of original mode
        FileMode replayMode = fileMode switch
        {
            FileMode.CreateNew => FileMode.CreateNew,
            FileMode.Create => FileMode.Create,
            FileMode.Truncate => FileMode.Create,        // final content replaces previous
            FileMode.Append => FileMode.OpenOrCreate,    // we'll truncate then write full content
            FileMode.Open => FileMode.Create,            // replace with final state
            FileMode.OpenOrCreate => FileMode.Create,    // replace with final state
            _ => FileMode.Create
        };

        using Stream stream = targetBackend.OpenWrite(path, replayMode, fileAccess, fileShare);

        // Truncate for modes that may not inherently truncate (OpenOrCreate / OpenOrCreate via mapping)
        if (replayMode is FileMode.OpenOrCreate or FileMode.Create && stream.CanSeek)
        {
            stream.SetLength(0);
        }

        stream.Position = 0;
        stream.Write(data, 0, data.Length);
        stream.Flush();
    }
}

/// <summary>
/// Extension methods for easier journal replay
/// </summary>
public static class JournalPlayerExtensions
{
    /// <summary>
    /// Replays the journal records from this backend onto a target backend
    /// </summary>
    /// <param name="journalBackend">The journal backend with recorded operations</param>
    /// <param name="targetBackend">The target backend to replay operations on</param>
    public static void ReplayTo(this JournalFileSystemBackend journalBackend, IFileSystemBackend targetBackend)
        => JournalPlayer.Replay(journalBackend, targetBackend);
}
