using System.Security.Cryptography.X509Certificates;

namespace DokiFS;

/// <summary>
/// Provides a normalized path inside the VFS. This should only be used when accessing a pat inside the VFS.
/// When requiring a path that falls outside the VFS, use a string
/// </summary>
/// <param name="path"></param>
public readonly struct VPath : IEquatable<VPath>
{
    public static readonly VPath Empty = new(string.Empty);
    public static readonly VPath Root = new("/");

    public const char DirectorySeparator = '/';
    public const string DirectorySeparatorString = "/";
    public const StringComparison PathComparison = StringComparison.Ordinal;

    public string FullPath { get; }
    public int Length => FullPath.Length;

    /// <summary>
    /// Returns true if the path is empty (null or length 0)
    /// </summary>
    public bool IsEmpty => FullPath is { Length: 0 } or null;

    /// <summary>
    /// Returns true if the path is absolute (starts with a directory separator)
    /// </summary>
    public bool IsAbsolute => FullPath is { Length: > 0 } && FullPath[0] == DirectorySeparator;

    /// <summary>
    /// Returns true if the path is the root path (a single directory separator)
    /// </summary>
    public bool IsRoot => FullPath?.Length == 1 && FullPath[0] == DirectorySeparator;

    /// <summary>
    /// Returns true if the path is a directory (ends with a directory separator)
    /// </summary>
    public bool IsDirectory => FullPath?.EndsWith(DirectorySeparatorString, PathComparison) ?? false;

    /// <summary>
    /// Returns true if the path points to a hidden entry (final segment starts with a dot)
    /// </summary>
    public bool IsHidden => (FullPath is not { Length: > 0 } || FullPath[^1] != DirectorySeparator)
        && FullPath.AsSpan().TrimEnd(DirectorySeparator).EndsWith('.');

    public VPath(string path)
    {
        FullPath = Normalize(path);
    }

    /// <summary>
    /// Normalizes the given path by converting backslashes to forward slashes and removing duplicate slashes.
    /// </summary>
    /// <param name="path"></param>
    /// <returns>The normalizes path</returns>
    public static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;

        int length = path.Length;
        bool needsNormalization = false;
        ReadOnlySpan<char> span = path.AsSpan();

        // Check if there's a trailing separator (unless it's the root path)
        if (length > 1 && span[length - 1] == DirectorySeparator)
        {
            needsNormalization = true;
        }
        else
        {
            for (int i = 0; i < length; i++)
            {
                char c = span[i];
                if (c == '\\' || (c == DirectorySeparator && i < length - 1 && span[i + 1] == DirectorySeparator))
                {
                    needsNormalization = true;
                    break;
                }
            }
        }

        if (needsNormalization == false)
        {
            return path;
        }

        return NormalizeString(length, span);
    }

    // This actually normalizes the string
    static string NormalizeString(int length, ReadOnlySpan<char> span)
    {
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

            // Don't add trailing separator unless it's the root path
            if (c == DirectorySeparator && i == span.Length - 1 && i > 0) continue;

            lastWasSeparator = c == DirectorySeparator;
            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Appends a path to the current one and return the combined path
    /// </summary>
    /// <param name="path">The path to append to the current one</param>
    /// <returns>A new VPath instance representing the combined path</returns>
    public VPath Append(VPath path)
    {
        if (path.IsEmpty) return this;
        if (IsEmpty) return path;

        if (IsRoot)
        {
            // No need to add separator since root already has one
            return new VPath(DirectorySeparator + path.FullPath);
        }

        int estimatedLength = FullPath.Length + 1 + path.FullPath.Length;
        Span<char> buffer = estimatedLength <= 256
            ? stackalloc char[estimatedLength]
            : new char[estimatedLength];

        ValueStringBuilder sb = new(buffer);

        // Copy the first path
        foreach (char t in FullPath)
            sb.Append(t);

        // Add separator if needed
        if (FullPath[^1] != DirectorySeparator)
            sb.Append(DirectorySeparator);

        // Copy the second path
        foreach (char t in path.FullPath)
            sb.Append(t);

        return new VPath(sb.ToString());
    }

    /// <summary>
    /// Checks if the current VPath starts with another one
    /// </summary>
    /// <param name="path">The path to check against</param>
    /// <returns>True if the current VPath starts with the given path, false otherwise</returns>
    public bool StartsWith(VPath path)
    {
        if (path.IsEmpty) return true;
        if (IsEmpty) return false;

        return FullPath.StartsWith(path.FullPath, PathComparison);
    }

    /// <summary>
    /// Gets the directory of the current path. If the current path points to a file, it will return the directory
    /// (eg: /a/b/file.txt -> /a/b/). If the current path is a directory itself
    /// </summary>
    /// <returns>The parent path of the current directory</returns>
    public VPath GetDirectory()
    {
        if (IsEmpty || IsRoot) return Empty;

        ReadOnlySpan<char> pathSpan = FullPath.AsSpan();
        int lastIndex = pathSpan.LastIndexOf(DirectorySeparator);

        if (lastIndex < 0) return Empty; // No directory separator
        if (lastIndex == 0) return Root; // Root directory
        if (lastIndex == 1 && pathSpan[0] == DirectorySeparator) return Root;

        // Use Span for slicing to avoid allocating string prematurely
        return new VPath(pathSpan[..lastIndex].ToString());
    }

    /// <summary>
    /// Gets the leaf/final segment of the current path.
    /// </summary>
    /// <returns>The leaf name of the current path</returns>
    public string GetLeaf()
    {
        if (IsEmpty) return string.Empty;

        if (IsRoot)
        {
            return "/";
        }

        int lastIndex = FullPath.LastIndexOf(DirectorySeparator);

        return FullPath[(lastIndex + 1)..];
    }

    /// <summary>
    /// Get the first segment of the current path. eg: /a/b/c -> /a/
    /// </summary>
    /// <returns>The root name of the current path</returns>
    public string GetRoot()
    {
        if (IsEmpty || IsRoot) return string.Empty;

        ReadOnlySpan<char> pathSpan = FullPath.AsSpan();
        int firstIndex = pathSpan.IndexOf(DirectorySeparator);

        if (firstIndex < 0) return FullPath;

        return pathSpan[..firstIndex].ToString();
    }

    /// <summary>
    /// Gets the filename of the path. This asumes a file.ext format
    /// </summary>
    /// <returns>The filename if file.ext pattern is present, otherwise string.empty</returns>
    public string GetFileName()
    {
        // Either no path, or is a directory
        if (IsEmpty || IsRoot) return string.Empty;

        ReadOnlySpan<char> pathSpan = FullPath.AsSpan();

        // Check if the path ends with a file (assume file.ext)
        // Take the last segment
        int lastSegmentIndex = pathSpan.LastIndexOf(DirectorySeparator);
        if (lastSegmentIndex <= 0) return string.Empty;

        ReadOnlySpan<char> finalSegment = FullPath.AsSpan(lastSegmentIndex + 1);

        // Check if the final segment resembles a file
        int extensionIndex = finalSegment.LastIndexOf('.');
        if (extensionIndex >= 0)
        {
            return finalSegment.ToString();
        }

        return string.Empty;
    }

    /// <summary>
    /// Splits the path into it's segments. If the path is empty, null or the root,
    /// this will return an empty array.
    /// </summary>
    /// <returns>An array of segmemnts, or an empty array (if null, empty or root)</returns>
    public string[] Split()
    {
        if (IsEmpty || IsRoot) return [];

        ReadOnlySpan<char> pathSpan = FullPath.AsSpan();
        MemoryExtensions.SpanSplitEnumerator<char> segmentRanges = pathSpan.Split(DirectorySeparator);
        int segmentCount = 0;
        foreach (Range segmentRange in segmentRanges)
        {
            // Skip empty segments
            if (segmentRange.End.Value > segmentRange.Start.Value)
            {
                segmentCount++;
            }
        }

        // Return early if there are no segments
        if (segmentCount == 0) return [];

        string[] segments = new string[segmentCount];
        int index = 0;
        foreach (Range range in segmentRanges)
        {
            if (range.End.Value > range.Start.Value)
            {
                segments[index++] = pathSpan[range].ToString();
            }
        }

        return segments;
    }

    /// <summary>
    /// Navigates one directory "up" to the parent directory. If the file ends on an extension, then move to the
    /// parent directory of its directory (eg: /a/b/file.txt -> /a/)
    /// </summary>
    /// <returns>A VPath of the parent directory</returns>
    public VPath Up()
    {
        // Can't navigate up from root
        if (IsRoot) return this;
        if (IsEmpty && IsAbsolute) return Root;
        if (IsEmpty && IsAbsolute == false) return Empty;

        ReadOnlySpan<char> pathSpan = FullPath.AsSpan();

        // Check if the path ends with a file (assume file.ext)
        // Take the last segment
        int lastSegmentIndex = pathSpan.LastIndexOf(DirectorySeparator);
        if (lastSegmentIndex < 0 && IsAbsolute) return Root;

        ReadOnlySpan<char> finalSegment = FullPath.AsSpan(lastSegmentIndex + 1);

        // Check if the final segment resembles a file
        int extensionIndex = finalSegment.LastIndexOf('.');
        if (extensionIndex >= 0)
        {
            // If it has an extension, ommit this segment
            pathSpan = FullPath.AsSpan(0, lastSegmentIndex);
        }

        lastSegmentIndex = pathSpan.LastIndexOf(DirectorySeparator);

        if (lastSegmentIndex < 0)
        {
            // If there's no directory separator, return root
            if (IsAbsolute) return Root;
            return Empty; // No parent for relative paths without separators
        }

        VPath p = new(pathSpan[..lastSegmentIndex].ToString());

        if (IsAbsolute && p.IsEmpty)
        {
            return Root;
        }

        return p;
    }

    /// <summary>
    /// Removes a part of the path, starting from the front
    /// </summary>
    /// <param name="reduction">The part of the path to reduce</param>
    /// <param name="additional">Extra characters to reduce</param>
    /// <returns>A new path that has the reduction removed</returns>
    public VPath ReduceStart(VPath reduction, int additional = 0)
    {
        if (reduction.IsEmpty || reduction.IsRoot) return this;

        ReadOnlySpan<char> pathSpan = FullPath.AsSpan();
        ReadOnlySpan<char> reductionSpan = reduction.FullPath.AsSpan();

        // Check if the reduction is a prefix of the path
        if (pathSpan.StartsWith(reductionSpan))
        {
            // If it is, remove it
            int l = reductionSpan.Length + additional;
            pathSpan = pathSpan[l..];
        }

        return new VPath(pathSpan.ToString());
    }

    public VPath ReduceEnd(VPath reduction)
    {
        if (reduction.IsEmpty || reduction.IsRoot) return this;

        ReadOnlySpan<char> pathSpan = FullPath.AsSpan();
        ReadOnlySpan<char> reductionSpan = reduction.FullPath.AsSpan();

        // Check if the reduction is a suffix of the path
        if (pathSpan.EndsWith(reductionSpan))
        {
            // If it is, remove it
            pathSpan = pathSpan[..^reductionSpan.Length];
        }

        return new VPath(pathSpan.ToString());
    }

    public override string ToString() => FullPath;

    public bool Equals(VPath other) => string.Equals(FullPath, other.FullPath, PathComparison);
    public override bool Equals(object obj) => obj is VPath path1 && Equals(path1);
    public override int GetHashCode() => FullPath?.GetHashCode(PathComparison) ?? 0;

    public static bool operator ==(VPath left, VPath right) => left.Equals(right);
    public static bool operator !=(VPath left, VPath right) => left.Equals(right) == false;

    public static VPath operator +(VPath left, VPath right) => left.Append(right);
    public static VPath operator +(VPath left, string right) => left.FullPath + right;
    public static VPath operator /(VPath left, VPath right) => left.Append(right);

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
