using System.Collections.ObjectModel;
using DokiFS.Interfaces;

namespace DokiFS.Backends.Journal;

public class JournalPlayer
{
    readonly ReadOnlyCollection<JournalEntry> journal;

    public JournalPlayer(List<JournalEntry> journal)
    {
        this.journal = journal.AsReadOnly();
    }

    /// <summary>
    /// Applies a journal to a backend.
    /// </summary>
    /// <remarks>
    /// It will map the journal methods to the backend method. So 2 journals on different backends might
    /// not produce the same outcome
    /// </remarks>
    /// <param name="journal"></param>
    /// <param name="backend"></param>
    /// <exception cref="JournalInterruptedException"></exception>
    public void ReplayJournalOn(IFileSystemBackend backend)
    {
        foreach (JournalEntry entry in journal)
        {
            switch (entry.JournalAction)
            {
                case JournalActions.CreateFile:
                {
                    VPath path = (VPath)entry.ParamStack[0];
                    if (entry.ParamStack.Length > 1)
                    {
                        long size = (long)entry.ParamStack[1];
                        backend.CreateFile(path, size);
                    }
                    else
                    {
                        backend.CreateFile(path);
                    }
                }
                break;

                case JournalActions.DeleteFile:
                {
                    VPath path = (VPath)entry.ParamStack[0];
                    backend.DeleteFile(path);
                }
                break;

                case JournalActions.MoveFile:
                {
                    VPath sourcePath = (VPath)entry.ParamStack[0];
                    VPath destinationPath = (VPath)entry.ParamStack[1];
                    if (entry.ParamStack.Length > 2)
                    {
                        bool overwrite = (bool)entry.ParamStack[2];
                        backend.MoveFile(sourcePath, destinationPath, overwrite);
                    }
                    else
                    {
                        backend.MoveFile(sourcePath, destinationPath);
                    }
                }
                break;

                case JournalActions.CopyFile:
                {
                    VPath sourcePath = (VPath)entry.ParamStack[0];
                    VPath destinationPath = (VPath)entry.ParamStack[1];
                    if (entry.ParamStack.Length > 2)
                    {
                        bool overwrite = (bool)entry.ParamStack[2];
                        backend.CopyFile(sourcePath, destinationPath, overwrite);
                    }
                    else
                    {
                        backend.CopyFile(sourcePath, destinationPath);
                    }
                }
                break;

                case JournalActions.OpenWrite:
                {
                    VPath path = (VPath)entry.ParamStack[0];
                    Stream targetStream;

                    if (entry.ParamStack.Length > 1)
                    {
                        FileMode mode = (FileMode)entry.ParamStack[1];
                        FileAccess access = (FileAccess)entry.ParamStack[2];
                        FileShare share = (FileShare)entry.ParamStack[3];
                        targetStream = backend.OpenWrite(path, mode, access, share);
                    }
                    else
                    {
                        targetStream = backend.OpenWrite(path);
                    }

                    using (targetStream)
                    {
                        if (entry.Data is { Length: > 0 })
                        {
                            targetStream.Write(entry.Data, 0, entry.Data.Length);
                            targetStream.Flush();
                        }
                    }
                }
                break;

                case JournalActions.CreateDirectory:
                {
                    VPath path = (VPath)entry.ParamStack[0];
                    backend.CreateDirectory(path);
                }
                break;

                case JournalActions.DeleteDirectory:
                {
                    VPath path = (VPath)entry.ParamStack[0];
                    if (entry.ParamStack.Length > 1)
                    {
                        bool recursive = (bool)entry.ParamStack[1];
                        backend.DeleteDirectory(path, recursive);
                    }
                    else
                    {
                        backend.DeleteDirectory(path);
                    }
                }
                break;

                case JournalActions.MoveDirectory:
                {
                    VPath sourcePath = (VPath)entry.ParamStack[0];
                    VPath destinationPath = (VPath)entry.ParamStack[1];
                    backend.MoveDirectory(sourcePath, destinationPath);
                }
                break;

                case JournalActions.CopyDirectory:
                {
                    VPath sourcePath = (VPath)entry.ParamStack[0];
                    VPath destinationPath = (VPath)entry.ParamStack[1];
                    backend.CopyDirectory(sourcePath, destinationPath);
                }
                break;
                default:
                    // Something went wrong, cancel entire journal application
                    throw new JournalInterruptedException(entry);
            }
        }
    }
}
