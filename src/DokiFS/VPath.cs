namespace DokiFS;

/// <summary>
/// Provides a normalized path inside the VFS. This should only be used when accessing a pat inside the VFS.
/// When requiring a path that falls outside the VFS, use a string
/// </summary>
/// <param name="path"></param>
public readonly struct VPath(string path) : IEquatable<VPath>
{
    public static readonly VPath Empty = new(string.Empty);
    public static readonly VPath Root = new("/");

    public const char DirectorySeparator = '/';
    public const string DirectorySeparatorString = "/";
    public const StringComparison PathComparison = StringComparison.Ordinal;

    public string FullPath { get; } = string.IsNullOrEmpty(path) ? string.Empty : Normalize(path);

    public bool IsNull => FullPath is null;
    public bool IsEmpty => FullPath is { Length: 0 };
    public bool IsAbsolute => FullPath is { Length: > 0 } && FullPath[0] == DirectorySeparator;
    public bool IsRoot => FullPath?.Length == 1 && FullPath[0] == DirectorySeparator;

    static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;

        int length = path.Length;
        bool needsNormalization = false;
        ReadOnlySpan<char> span = path.AsSpan();

        for (int i = 0; i < length; i++)
        {
            char c = span[i];
            if (c == '\\' || (c == DirectorySeparator && i < length - 1 && span[i + 1] == DirectorySeparator))
            {
                needsNormalization = true;
                break;
            }
        }

        if (needsNormalization == false)
        {
            return path;
        }

        // Allocate only what we need (or slightly more)
        // Most paths won't grow during normalization, they'll shrink or stay the same
        Span<char> buffer = length <= 256 ? stackalloc char[length] : new char[length];
        ValueStringBuilder sb = new(buffer);
        bool lastWasSeparator = false;

        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];

            // Convert backslash to forward slash
            if (c == '\\') c = DirectorySeparator;

            // Skip duplicate separators
            if (c == DirectorySeparator && lastWasSeparator) continue;

            lastWasSeparator = c == DirectorySeparator;
            sb.Append(c);
        }

        return sb.ToString();
    }

    public VPath Combine(VPath path)
    {
        // Single-check optimizations
        if (path.IsAbsolute) return path;

        // Empty checks combined
        if (path.IsNull || path.IsEmpty) return this;
        if (IsNull || IsEmpty) return path;

        if (IsRoot)
        {
            // No need to add separator since root already has one
            return new VPath(DirectorySeparator + path.FullPath);
        }

        // Use ValueStringBuilder implementation from optimization #2
        int estimatedLength = FullPath.Length + 1 + path.FullPath.Length;
        Span<char> buffer = estimatedLength <= 256
            ? stackalloc char[estimatedLength]
            : new char[estimatedLength];

        ValueStringBuilder sb = new(buffer);

        // Copy the first path
        for (int i = 0; i < FullPath.Length; i++)
            sb.Append(FullPath[i]);

        // Add separator if needed
        if (FullPath[^1] != DirectorySeparator)
            sb.Append(DirectorySeparator);

        // Copy the second path
        for (int i = 0; i < path.FullPath.Length; i++)
            sb.Append(path.FullPath[i]);

        return new VPath(sb.ToString());
    }

    public bool StartsWith(VPath path)
    {
        if (path.IsNull || path.IsEmpty) return true;
        if (IsNull || IsEmpty) return false;

        return FullPath.StartsWith(path.FullPath, PathComparison) &&
                (FullPath.Length == path.FullPath.Length ||
                (FullPath.Length > path.FullPath.Length && FullPath[path.FullPath.Length] == DirectorySeparator));
    }

    public VPath GetDirectory()
    {
        if (IsNull || IsEmpty || IsRoot) return Empty;

        ReadOnlySpan<char> pathSpan = FullPath.AsSpan();
        int lastIndex = pathSpan.LastIndexOf(DirectorySeparator);

        if (lastIndex < 0) return Empty; // No directory separator
        if (lastIndex == 0) return Root; // Root directory
        if (lastIndex == 1 && pathSpan[0] == DirectorySeparator) return Root;

        // Use Span for slicing to avoid allocating string prematurely
        return new VPath(pathSpan[..lastIndex].ToString());
    }

    public string GetFileName()
    {
        if (IsNull || IsEmpty) return string.Empty;

        int lastIndex = FullPath.LastIndexOf(DirectorySeparator);
        if (lastIndex < 0) return FullPath;
        if (lastIndex == FullPath.Length - 1) return string.Empty;

        return FullPath[(lastIndex + 1)..];
    }

    public override string ToString() => FullPath;

    public bool Equals(VPath other) => string.Equals(FullPath, other.FullPath, PathComparison);
    public override bool Equals(object obj) => obj is VPath path1 && Equals(path1);
    public override int GetHashCode() => FullPath?.GetHashCode(PathComparison) ?? 0;

    public static bool operator ==(VPath left, VPath right) => left.Equals(right);
    public static bool operator !=(VPath left, VPath right) => left.Equals(right) == false;

    public static implicit operator VPath(string path) => new(path);
    public static explicit operator string(VPath path) => path.FullPath;
}

internal ref struct ValueStringBuilder
{
    Span<char> buffer;
    int pos;

    public ValueStringBuilder(Span<char> initialBuffer)
    {
        buffer = initialBuffer;
        pos = 0;
    }

    public void Append(char c)
    {
        if (pos >= buffer.Length)
        {
            Grow();
        }
        buffer[pos++] = c;
    }

    void Grow()
    {
        char[] newBuffer = new char[buffer.Length * 2];
        buffer.CopyTo(newBuffer);
        buffer = newBuffer;
    }

    public override readonly string ToString() => new(buffer[..pos]);
}
