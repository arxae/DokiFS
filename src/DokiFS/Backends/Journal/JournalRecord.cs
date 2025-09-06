#nullable enable

using System.Text.Json.Serialization;

namespace DokiFS.Backends.Journal;

public class JournalRecord
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? Description { get; init; }
    public JournalOperations Operation { get; init; }
    public VPath Path { get; init; }
    public VPath SecondaryPath { get; init; }
    public ContentReference? Content { get; init; } = new();
    public JournalParameters Parameters { get; init; } = new();

    public JournalRecord(JournalOperations operation, VPath path)
    {
        Operation = operation;
        Path = path;
        SecondaryPath = VPath.Empty;
    }

    public override string ToString()
        => $"{Operation} {Path}" + (SecondaryPath != null ? $" -> {SecondaryPath}" : "");
}

public class JournalParameters
{
    public object? this[string key]
    {
        get => Raw.TryGetValue(key, out object? value) ? value : null;
        set
        {
            if (value == null)
            {
                Raw.Remove(key);
            }
            else
            {
                Raw[key] = value;
            }
        }
    }

    [JsonIgnore]
    public IEnumerable<string> Keys => Raw.Keys;

    public T? Get<T>(string key) => this[key] is T value ? value : default;
    public void Set<T>(string key, T value) => this[key] = value;
    public bool Contains(string key) => Raw.ContainsKey(key);
    public Dictionary<string, object> Raw { get; } = [];
}

public sealed class ContentReference
{
    public string ContentId { get; init; } = string.Empty;
    public int Version { get; init; }
    public ContentType Type { get; init; }
    public long Size { get; init; }
    public string? ContentHash { get; init; }
    public long? StreamOffset { get; init; }
    public long? Length { get; init; }
    public byte[]? Data { get; init; }
}

public enum ContentType
{
    BaseContent,
    Patch,
    Reference,
    Partial
}

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

    // Mount operation parameters
    public static bool GetForceMount(this JournalParameters parameters)
        => parameters.Get<bool>("Force");

    public static void SetForceMount(this JournalParameters parameters, bool force)
        => parameters.Set("Force", force);

    public static string? GetBackendType(this JournalParameters parameters)
        => parameters.Get<string>("BackendType");

    public static void SetBackendType(this JournalParameters parameters, string backendType)
        => parameters.Set("BackendType", backendType);
}
