using System.Collections.ObjectModel;
using DokiFS.Interfaces;
using DokiFS.Logging;
using Microsoft.Extensions.Logging;

namespace DokiFS.Backends.Journal;

public class JournalPlayer : IDisposable
{
    public int CurrentPosition { get; private set; }
    public int EntryCount => journal.Count;
    public bool CanMoveForward => CurrentPosition < journal.Count;
    public bool CanMoveBackward => CurrentPosition > 0;
    public JournalEntry CurrentEntry => CurrentPosition >= 0 && CurrentPosition < journal.Count ? journal[CurrentPosition] : null;

    readonly ReadOnlyCollection<JournalEntry> journal;
    readonly IFileSystemBackend backend;
    readonly Dictionary<int, byte[]> originalFileContents = [];
    readonly bool recordUndo;

    bool disposed;

    readonly ILogger log = DokiFSLogger.CreateLogger<JournalPlayer>();

    /// <summary>
    /// The JournalPlayer can be used to replay previously recorded journals on a different backend. This can either be
    /// done fully, or step by step
    /// </summary>
    /// <remarks>
    /// When recordUndo is set to true, the playback will record extra data to undo the steps as well. This will
    /// consume more memory (depending on the amount of data, especially using OpenWrite). But this will allow the
    /// player to also move backwards in the journal.
    /// <br />
    /// When the CurrentEntry is mentioned, this refers to the first unplayed entry in the journal. Not the entry that
    /// has just been played
    /// </remarks>
    /// <param name="journal">The journal to playback</param>
    /// <param name="backend">What backend to apply the journal to</param>
    /// <param name="recordUndo">When set to true, record data to also allow backwards movement</param>
    public JournalPlayer(ReadOnlyCollection<JournalEntry> journal, IFileSystemBackend backend, bool recordUndo)
    {
        ArgumentNullException.ThrowIfNull(backend);

        this.journal = journal;
        this.backend = backend;
        this.recordUndo = recordUndo;

        CurrentPosition = 0;
    }

    /// <summary>
    /// Replays the journal from the current position to the end
    /// </summary>
    public void ReplayJournal()
    {
        while (CanMoveForward)
        {
            MoveForward();
        }
    }

    /// <summary>
    /// Moves x amount of steps forwards in the journal
    /// </summary>
    /// <param name="steps">The amount of stepds to move forward</param>
    /// <returns>The current JournalEntry</returns>
    public JournalEntry MoveForward(int steps = 1)
    {
        for (int i = 0; i < steps; i++)
        {
            if (CanMoveForward == false)
                return null;

            JournalEntry entry = journal[CurrentPosition];
            log.LogTrace("Applying: {JournalEntry}", entry);
            ApplySingleEntry(entry);

            CurrentPosition++;
        }

        return CurrentEntry;
    }

    /// <summary>
    /// Move x amount of steps backwards, if able. If recordUndo is set to false, this will throw
    /// </summary>
    /// <param name="steps">
    /// The amount of steps to move backwards. If stepds is higher then the amount of records
    /// remaining, then playback will stop
    /// </param>
    /// <returns>The current JournalEntry</returns>
    public JournalEntry MoveBackward(int steps = 1)
    {
        if (recordUndo == false)
        {
            throw new InvalidOperationException("No undo history was recorded for this playback");
        }

        for (int i = 0; i < steps; i++)
        {
            if (CanMoveBackward == false)
                return null;

            CurrentPosition--;

            JournalEntry entry = journal[CurrentPosition];
            log.LogTrace("Undoing: {JournalEntry}", entry);
            UndoSingleEntry(entry);
        }

        return CurrentEntry;
    }

    /// <summary>
    /// Applies a single journal entry to the backend.
    /// </summary>
    /// <remarks>
    /// Keep in mind that when traversing the journal manually, MoveForward and MoveBackwards should be used.
    /// Calling this method by itself will not move the current journal, but will still affect the journal normally.
    /// Undoing will be unreliable
    /// </remarks>
    /// <param name="entry">The JournalEntry to execute</param>
    /// <exception cref="JournalInterruptedException">Thrown when a action and paramstack cannot be mapped to a backend method</exception>
    public void ApplySingleEntry(JournalEntry entry)
    {
        switch (entry.JournalAction)
        {
            case JournalActions.CreateFile:
                ApplySingleEntryMethods.CreateFile(entry, backend);
                break;

            case JournalActions.DeleteFile:
                ApplySingleEntryMethods.DeleteFile(entry, backend, recordUndo, originalFileContents);
                break;

            case JournalActions.MoveFile:
                ApplySingleEntryMethods.MoveFile(entry, backend, recordUndo, originalFileContents);
                break;

            case JournalActions.CopyFile:
                ApplySingleEntryMethods.CopyFile(entry, backend, recordUndo, originalFileContents);
                break;

            case JournalActions.OpenWrite:
                ApplySingleEntryMethods.OpenWrite(entry, backend, recordUndo, originalFileContents);
                break;

            case JournalActions.CreateDirectory:
                ApplySingleEntryMethods.CreateDirectory(entry, backend);
                break;

            case JournalActions.DeleteDirectory:
                ApplySingleEntryMethods.DeleteDirectory(entry, backend);
                break;

            case JournalActions.MoveDirectory:
                ApplySingleEntryMethods.MoveDirectory(entry, backend);
                break;

            case JournalActions.CopyDirectory:
                ApplySingleEntryMethods.CopyDirectory(entry, backend);
                break;
            default:
                // Something went wrong, cancel entire journal application
                throw new JournalInterruptedException(entry);
        }
    }

    /// <summary>
    /// Undoes a single journal entry to the backend.
    /// </summary>
    /// <remarks>
    /// Keep in mind that when traversing the journal manually, MoveForward and MoveBackwards should be used.
    /// Calling this method by itself will not move the current journal, but will still affect the journal normally.
    /// Since this undo is not expected, it will behave unreliably
    /// </remarks>
    /// <param name="entry">The JournalEntry to execute</param>
    /// <exception cref="JournalInterruptedException">Thrown when a action and paramstack cannot be mapped to a backend method</exception>
    public void UndoSingleEntry(JournalEntry entry)
    {
        switch (entry.JournalAction)
        {
            case JournalActions.CreateFile:
            {
                VPath path = (VPath)entry.ParamStack[0];
                backend.DeleteFile(path);
            }
            break;

            case JournalActions.DeleteFile:
            {
                VPath path = (VPath)entry.ParamStack[0];
                backend.CreateFile(path);

                // Restore file content if we stored it
                if (originalFileContents.TryGetValue(entry.Id, out byte[] content))
                {
                    using Stream stream = backend.OpenWrite(path);
                    stream.Write(content, 0, content.Length);
                    stream.Flush();
                }
            }
            break;

            case JournalActions.MoveFile:
            {
                // Move back to original location
                VPath originalSourcePath = (VPath)entry.ParamStack[0];
                VPath originalDestinationPath = (VPath)entry.ParamStack[1];
                backend.MoveFile(originalDestinationPath, originalSourcePath, true);

                // If we had stored content from an overwritten file, restore it
                if (originalFileContents.TryGetValue(entry.Id, out byte[] content))
                {
                    using Stream stream = backend.OpenWrite(originalDestinationPath);
                    stream.Write(content, 0, content.Length);
                    stream.Flush();
                }
            }
            break;

            case JournalActions.CopyFile:
            {
                // Delete the copied file
                VPath destinationPath = (VPath)entry.ParamStack[1];
                backend.DeleteFile(destinationPath);

                // If we had stored content from an overwritten file, restore it
                if (originalFileContents.TryGetValue(entry.Id, out byte[] content))
                {
                    backend.CreateFile(destinationPath);
                    using Stream stream = backend.OpenWrite(destinationPath);
                    stream.Write(content, 0, content.Length);
                    stream.Flush();
                }
            }
            break;

            case JournalActions.OpenWrite:
            {
                VPath path = (VPath)entry.ParamStack[0];
                // Restore original content if available
                if (originalFileContents.TryGetValue(entry.Id, out byte[] content))
                {
                    using Stream stream = backend.OpenWrite(path, FileMode.Create, FileAccess.Write, FileShare.None);
                    stream.Write(content, 0, content.Length);
                    stream.Flush();
                }
            }
            break;

            case JournalActions.CreateDirectory:
            {
                VPath path = (VPath)entry.ParamStack[0];
                backend.DeleteDirectory(path, false);
            }
            break;

            case JournalActions.DeleteDirectory:
            {
                VPath path = (VPath)entry.ParamStack[0];
                // Simply recreate the directory, but content is lost unless stored
                backend.CreateDirectory(path);
            }
            break;

            case JournalActions.MoveDirectory:
            {
                // Move back to original location
                VPath originalSourcePath = (VPath)entry.ParamStack[0];
                VPath originalDestinationPath = (VPath)entry.ParamStack[1];
                backend.MoveDirectory(originalDestinationPath, originalSourcePath);
            }
            break;

            case JournalActions.CopyDirectory:
            {
                // Delete the copied directory
                VPath originalDestinationPath = (VPath)entry.ParamStack[1];
                backend.DeleteDirectory(originalDestinationPath, true);
            }
            break;
            default:
                throw new JournalInterruptedException(entry);
        }
    }

    public override string ToString()
        => $"JournalPlayer: {CurrentPosition}/{EntryCount}, CurrentEntry: {CurrentEntry}";

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed != false) return;

        if (disposing)
        {
            (backend as IDisposable)?.Dispose();
            originalFileContents.Clear();
        }

        disposed = true;
    }
}
