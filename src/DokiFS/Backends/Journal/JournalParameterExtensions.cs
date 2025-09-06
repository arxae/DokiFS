#nullable enable

namespace DokiFS.Backends.Journal;

public static class JournalParameterExtensions
{
    // File operation parameters
    public static long GetFileSize(this JournalParameters parameters)
        => parameters.Get<long>("FileSize");

    public static void SetFileSize(this JournalParameters parameters, long size)
        => parameters.Set("FileSize", size);

    public static bool GetOverwrite(this JournalParameters parameters)
        => parameters.Get<bool>("Overwrite");

    public static void SetOverwrite(this JournalParameters parameters, bool overwrite)
        => parameters.Set("Overwrite", overwrite);

    public static bool GetRecursive(this JournalParameters parameters)
        => parameters.Get<bool>("Recursive");

    public static void SetRecursive(this JournalParameters parameters, bool recursive)
        => parameters.Set("Recursive", recursive);

    public static VPath GetSourcePath(this JournalParameters parameters)
        => parameters.Get<VPath>("SourcePath");

    public static void SetSourcePath(this JournalParameters parameters, VPath path)
        => parameters.Set("SourcePath", path);

    public static VPath GetDestinationPath(this JournalParameters parameters)
        => parameters.Get<VPath>("DestinationPath");

    public static void SetDestinationPath(this JournalParameters parameters, VPath path)
        => parameters.Set("DestinationPath", path);

    // Stream operation parameters
    public static FileMode GetFileMode(this JournalParameters parameters)
        => parameters.Get<FileMode>("FileMode");

    public static void SetFileMode(this JournalParameters parameters, FileMode mode)
        => parameters.Set("FileMode", mode);

    public static FileAccess GetFileAccess(this JournalParameters parameters)
        => parameters.Get<FileAccess>("FileAccess");

    public static void SetFileAccess(this JournalParameters parameters, FileAccess access)
        => parameters.Set("FileAccess", access);

    public static FileShare GetFileShare(this JournalParameters parameters)
        => parameters.Get<FileShare>("FileShare");

    public static void SetFileShare(this JournalParameters parameters, FileShare share)
        => parameters.Set("FileShare", share);

    public static long GetStreamPosition(this JournalParameters parameters)
        => parameters.Get<long>("StreamPosition");

    public static void SetStreamPosition(this JournalParameters parameters, long position)
        => parameters.Set("StreamPosition", position);

}
