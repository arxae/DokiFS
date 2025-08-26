using DokiFS.Interfaces;

namespace DokiFS.Backends.Journal;

internal static class ApplySingleEntryMethods
{
    internal static void CreateFile(JournalEntry entry, IFileSystemBackend backend)
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

    internal static void DeleteFile(JournalEntry entry, IFileSystemBackend backend, bool recordUndo, Dictionary<int, byte[]> originalFileContents)
    {
        VPath path = (VPath)entry.ParamStack[0];
        // Store the file content before deletion for potential undo
        if (recordUndo && backend.Exists(path))
        {
            using Stream stream = backend.OpenRead(path);
            if (stream.Length > 0)
            {
                byte[] content = new byte[stream.Length];
                stream.ReadExactly(content);
                originalFileContents[entry.Id] = content;
            }
        }
        backend.DeleteFile(path);
    }

    internal static void MoveFile(JournalEntry entry, IFileSystemBackend backend, bool recordUndo, Dictionary<int, byte[]> originalFileContents)
    {
        VPath sourcePath = (VPath)entry.ParamStack[0];
        VPath destinationPath = (VPath)entry.ParamStack[1];
        bool overwrite = (bool)entry.ParamStack[2];

        // Store the destination file content if it exists and will be overwritten
        if (recordUndo && overwrite && backend.Exists(destinationPath))
        {
            using Stream stream = backend.OpenRead(destinationPath);
            if (stream.Length > 0)
            {
                byte[] content = new byte[stream.Length];
                stream.ReadExactly(content);
                originalFileContents[entry.Id] = content;
            }
        }

        backend.MoveFile(sourcePath, destinationPath, overwrite);
    }

    internal static void CopyFile(JournalEntry entry, IFileSystemBackend backend, bool recordUndo, Dictionary<int, byte[]> originalFileContents)
    {
        VPath sourcePath = (VPath)entry.ParamStack[0];
        VPath destinationPath = (VPath)entry.ParamStack[1];
        bool overwrite = (bool)entry.ParamStack[2];

        // Store the destination file content if it exists and will be overwritten
        if (recordUndo && overwrite && backend.Exists(destinationPath))
        {
            using Stream stream = backend.OpenRead(destinationPath);
            if (stream.Length > 0)
            {
                byte[] content = new byte[stream.Length];
                stream.ReadExactly(content);
                originalFileContents[entry.Id] = content;
            }
        }

        backend.CopyFile(sourcePath, destinationPath, overwrite);
    }

    internal static void OpenWrite(JournalEntry entry, IFileSystemBackend backend, bool recordUndo, Dictionary<int, byte[]> originalFileContents)
    {
        VPath path = (VPath)entry.ParamStack[0];
        FileMode mode = (FileMode)entry.ParamStack[1];
        FileAccess access = (FileAccess)entry.ParamStack[2];
        FileShare share = (FileShare)entry.ParamStack[3];

        // Store the original file content if it exists
        if (recordUndo && backend.Exists(path))
        {
            using Stream stream = backend.OpenRead(path);
            if (stream.Length > 0)
            {
                byte[] content = new byte[stream.Length];
                stream.ReadExactly(content);
                originalFileContents[entry.Id] = content;
            }
        }

        Stream targetStream = backend.OpenWrite(path, mode, access, share);

        using (targetStream)
        {
            if (entry.Data is { Length: > 0 })
            {
                targetStream.Write(entry.Data, 0, entry.Data.Length);
                targetStream.Flush();
            }
        }
    }

    internal static void CreateDirectory(JournalEntry entry, IFileSystemBackend backend)
    {
        VPath path = (VPath)entry.ParamStack[0];
        backend.CreateDirectory(path);
    }

    internal static void DeleteDirectory(JournalEntry entry, IFileSystemBackend backend)
    {
        VPath path = (VPath)entry.ParamStack[0];
        bool recursive = (bool)entry.ParamStack[1];
        backend.DeleteDirectory(path, recursive);
    }

    internal static void MoveDirectory(JournalEntry entry, IFileSystemBackend backend)
    {
        VPath sourcePath = (VPath)entry.ParamStack[0];
        VPath destinationPath = (VPath)entry.ParamStack[1];
        backend.MoveDirectory(sourcePath, destinationPath);
    }

    internal static void CopyDirectory(JournalEntry entry, IFileSystemBackend backend)
    {
        VPath sourcePath = (VPath)entry.ParamStack[0];
        VPath destinationPath = (VPath)entry.ParamStack[1];
        backend.CopyDirectory(sourcePath, destinationPath);
    }

}
