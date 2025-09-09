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
                // Stream opening doesn't need replay - we'll handle the actual writes
                break;

            case JournalOperations.StreamWrite:
                ReplayStreamWrite(record, targetBackend);
                break;

            case JournalOperations.CloseWriteStream:
                // Stream closing doesn't need special handling
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
