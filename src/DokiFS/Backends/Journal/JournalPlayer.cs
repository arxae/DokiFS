using System.Text.Json;
using DokiFS.Interfaces;

namespace DokiFS.Backends.Journal;

/// <summary>
/// Replays journal records on a target backend
/// </summary>
public class JournalPlayer
{
    readonly IFileSystemBackend targetBackend;

    public JournalPlayer(IFileSystemBackend targetBackend)
    {
        this.targetBackend = targetBackend
            ?? throw new ArgumentNullException(nameof(targetBackend));
    }

    /// <summary>
    /// Replays all journal records on the target backend
    /// </summary>
    /// <param name="journalRecords">The journal records to replay</param>
    /// <exception cref="InvalidOperationException">When an operation cannot be replayed</exception>
    public void Replay(IEnumerable<JournalRecord> journalRecords)
    {
        List<JournalRecord> records = [.. journalRecords.OrderBy(r => r.Timestamp)];

        for (int i = 0; i < records.Count; i++)
        {
            try
            {
                ReplayRecord(records[i]);
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
    public void Replay(JournalFileSystemBackend journalBackend) => Replay(journalBackend.JournalRecords);

    public void Replay(string filePath, IFileSystemBackend targetBackend)
    {
        if (File.Exists(filePath) == false)
        {
            throw new FileNotFoundException("Journal file not found", filePath);
        }

        string json = File.ReadAllText(filePath);
        List<JournalRecord> records = JsonSerializer.Deserialize<List<JournalRecord>>(json)
            ?? throw new InvalidOperationException("Failed to deserialize journal records");

        JournalPlayer player = new(targetBackend);
        player.Replay(records);
    }

    void ReplayRecord(JournalRecord record)
    {
        switch (record.Operation)
        {
            case JournalOperations.CreateFile:
                ReplayCreateFile(record);
                break;

            case JournalOperations.DeleteFile:
                ReplayDeleteFile(record);
                break;

            case JournalOperations.MoveFile:
                ReplayMoveFile(record);
                break;

            case JournalOperations.CopyFile:
                ReplayCopyFile(record);
                break;

            case JournalOperations.CreateDirectory:
                ReplayCreateDirectory(record);
                break;

            case JournalOperations.DeleteDirectory:
                ReplayDeleteDirectory(record);
                break;

            case JournalOperations.MoveDirectory:
                ReplayMoveDirectory(record);
                break;

            case JournalOperations.CopyDirectory:
                ReplayCopyDirectory(record);
                break;

            case JournalOperations.OpenWrite:
                // Stream opening doesn't need replay - we'll handle the actual writes
                break;

            case JournalOperations.StreamWrite:
                ReplayStreamWrite(record);
                break;

            case JournalOperations.CloseWriteStream:
                // Stream closing doesn't need special handling
                break;
            default:
                throw new NotSupportedException($"Unknown journal operation: {record.Operation}");
        }
    }

    void ReplayCreateFile(JournalRecord record)
    {
        long size = record.Parameters.GetFileSize();
        targetBackend.CreateFile(record.Path, size);
    }

    void ReplayDeleteFile(JournalRecord record) => targetBackend.DeleteFile(record.Path);

    void ReplayMoveFile(JournalRecord record)
    {
        if (record.SecondaryPath == null)
            throw new InvalidOperationException("Move operation requires a secondary path");

        bool overwrite = record.Parameters.GetOverwrite();
        targetBackend.MoveFile(record.Path, record.SecondaryPath, overwrite);
    }

    void ReplayCopyFile(JournalRecord record)
    {
        if (record.SecondaryPath == null)
            throw new InvalidOperationException("Copy operation requires a secondary path");

        bool overwrite = record.Parameters.GetOverwrite();
        targetBackend.CopyFile(record.Path, record.SecondaryPath, overwrite);
    }

    void ReplayCreateDirectory(JournalRecord record) => targetBackend.CreateDirectory(record.Path);

    void ReplayDeleteDirectory(JournalRecord record)
    {
        bool recursive = record.Parameters.GetRecursive();
        targetBackend.DeleteDirectory(record.Path, recursive);
    }

    void ReplayMoveDirectory(JournalRecord record)
    {
        if (record.SecondaryPath == null)
            throw new InvalidOperationException("Move directory operation requires a secondary path");

        targetBackend.MoveDirectory(record.Path, record.SecondaryPath);
    }

    void ReplayCopyDirectory(JournalRecord record)
    {
        if (record.SecondaryPath == null)
            throw new InvalidOperationException("Copy directory operation requires a secondary path");

        targetBackend.CopyDirectory(record.Path, record.SecondaryPath);
    }

    void ReplayStreamWrite(JournalRecord record)
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
        using Stream stream = targetBackend.OpenWrite(record.Path, fileMode, fileAccess, fileShare);

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
    {
        JournalPlayer player = new(targetBackend);
        player.Replay(journalBackend);
    }
}
