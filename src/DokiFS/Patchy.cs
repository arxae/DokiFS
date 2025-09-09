/*
Patch file fomrmat:
    type: binary|string
    operations:
    <operations>

Patch operations:
  > N           Skip N lines/bytes
  + data        Add line (for strings), Add bytes in hex (for bytes)
  - N           Delete N lines/bytes
  ~ N data      Replace N lines with the given lines/bytes.
                Essentially - N, followed by + data
*/

namespace DokiFS.Patchy;

/// <summary>
/// Class to generate a diff between two arrays of strings or bytes, producing a minimal set of patch operations.
/// </summary>
public static class PatchyDiff
{
    private const char OP_SKIP = '>';
    private const char OP_ADD = '+';
    private const char OP_DELETE = '-';
    private const char OP_REPLACE = '~';

    public enum LineAction { Equal, Add, Delete }

    public static List<string> Generate(string[] original, string[] modified)
    {
        List<(LineAction Action, string Item)> rawDiff = GenerateRawDiff(original, modified);
        return Minify(rawDiff);
    }

    public static List<string> Generate(byte[] original, byte[] modified)
    {
        List<(LineAction Action, byte Item)> rawDiff = GenerateRawDiff(original, modified);
        return Minify(rawDiff);
    }

    static List<(LineAction Action, T Item)> GenerateRawDiff<T>(
        IReadOnlyList<T> original, IReadOnlyList<T> modified)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(modified);

        int[,] lengths = CalculateLengthMatrix(original, modified);
        List<(LineAction Action, T Item)> rawDiff = BuildDiffList(original, modified, lengths);
        rawDiff.Reverse();
        return rawDiff;
    }

    static int[,] CalculateLengthMatrix<T>(IReadOnlyList<T> original, IReadOnlyList<T> modified)
    {
        int[,] lengths = new int[original.Count + 1, modified.Count + 1];
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        for (int i = 0; i < original.Count; i++)
        {
            for (int j = 0; j < modified.Count; j++)
            {
                lengths[i + 1, j + 1] = comparer.Equals(original[i], modified[j])
                    ? lengths[i, j] + 1
                    : Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
            }
        }

        return lengths;
    }

    static List<(LineAction Action, T Item)> BuildDiffList<T>(
        IReadOnlyList<T> original,
        IReadOnlyList<T> modified,
        int[,] lengths)
    {
        List<(LineAction Action, T Item)> rawDiff = [];
        int x = original.Count;
        int y = modified.Count;
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        while (x > 0 || y > 0)
        {
            if (MatchesOriginalAndModified(x, y, original, modified, comparer))
            {
                rawDiff.Add((LineAction.Equal, original[x - 1]));
                x--; y--;
                continue;
            }

            if (ShouldTakeFromModified(x, y, lengths))
            {
                rawDiff.Add((LineAction.Add, modified[y - 1]));
                y--;
                continue;
            }

            rawDiff.Add((LineAction.Delete, original[x - 1]));
            x--;
        }

        return rawDiff;
    }

    static bool MatchesOriginalAndModified<T>(
        int x,
        int y,
        IReadOnlyList<T> original,
        IReadOnlyList<T> modified,
        EqualityComparer<T> comparer)
        => x > 0 && y > 0 && comparer.Equals(original[x - 1], modified[y - 1]);

    static bool ShouldTakeFromModified(int x, int y, int[,] lengths)
        => y > 0 && (x == 0 || lengths[x, y - 1] >= lengths[x - 1, y]);

    public static List<string> Minify<T>(List<(LineAction Action, T Item)> rawDiff)
    {
        List<string> operations = [];
        int position = 0;

        while (position < rawDiff.Count)
        {
            LineAction action = rawDiff[position].Action;

            position = action switch
            {
                LineAction.Delete => HandleDelete(rawDiff, position, operations),
                LineAction.Add => HandleAdd(rawDiff, position, operations),
                LineAction.Equal => HandleEqual(rawDiff, position, operations),
                _ => throw new InvalidOperationException($"Unknown action: {action}"),
            };
        }

        return operations;
    }

    static int HandleDelete<T>(List<(LineAction Action, T Item)> rawDiff, int position, List<string> operations)
    {
        int deleteStart = position;
        while (position < rawDiff.Count && rawDiff[position].Action == LineAction.Delete)
        {
            position++;
        }
        int deleteCount = position - deleteStart;

        // Check if the delete is followed by an add, which makes it a 'replace' operation
        if (position < rawDiff.Count && rawDiff[position].Action == LineAction.Add)
        {
            List<T> items = [];
            while (position < rawDiff.Count && rawDiff[position].Action == LineAction.Add)
            {
                items.Add(rawDiff[position].Item);
                position++;
            }

            if (typeof(T) == typeof(string))
            {
                operations.Add($"{OP_REPLACE} {deleteCount} {string.Join("\\n", items.Cast<string>())}");
            }
            else
            {
                operations.Add($"{OP_REPLACE} {deleteCount} {Convert.ToHexString([.. items.Cast<byte>()])}");
            }
        }
        else
        {
            operations.Add($"{OP_DELETE} {deleteCount}");
        }

        return position;
    }

    static int HandleAdd<T>(List<(LineAction Action, T Item)> rawDiff, int position, List<string> operations)
    {
        List<T> addItems = [];
        while (position < rawDiff.Count && rawDiff[position].Action == LineAction.Add)
        {
            addItems.Add(rawDiff[position].Item);
            position++;
        }

        if (typeof(T) == typeof(string))
        {
            foreach (string line in addItems.Cast<string>())
            {
                operations.Add($"{OP_ADD} {line}");
            }
        }
        else
        {
            operations.Add($"{OP_ADD} {Convert.ToHexString([.. addItems.Cast<byte>()])}");
        }

        return position;
    }

    static int HandleEqual<T>(List<(LineAction Action, T Item)> rawDiff, int position, List<string> operations)
    {
        int equalStart = position;
        while (position < rawDiff.Count && rawDiff[position].Action == LineAction.Equal)
        {
            position++;
        }
        operations.Add($"{OP_SKIP} {position - equalStart}");

        return position;
    }

    public static T[] ApplyPatch<T>(T[] original, List<string> patchOperations)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(patchOperations);

        List<T> result = [.. original];
        int index = 0;

        foreach (string operation in patchOperations)
        {
            if (string.IsNullOrWhiteSpace(operation)) continue;

            char opType = operation[0];
            string opData = operation[2..];

            switch (opType)
            {
                case '>' when int.TryParse(opData, out int skipCount):
                    index += skipCount;
                    break;

                case '+' when typeof(T) == typeof(string):
                    result.Insert(index, (T)(object)opData);
                    index++;
                    break;

                case '+' when typeof(T) == typeof(byte):
                    byte[] bytesToAdd = Convert.FromHexString(opData);
                    result.InsertRange(index, bytesToAdd.Cast<T>());
                    index += bytesToAdd.Length;
                    break;

                case '-' when int.TryParse(opData, out int deleteCount):
                    result.RemoveRange(index, deleteCount);
                    break;

                case '~':
                    string[] parts = opData.Split(' ', 2);
                    if (parts.Length != 2 || !int.TryParse(parts[0], out int replaceCount))
                        throw new FormatException($"Invalid replace operation format: {operation}");

                    if (typeof(T) == typeof(string))
                    {
                        string[] newLines = parts[1].Split("\\n");
                        result.RemoveRange(index, replaceCount);
                        result.InsertRange(index, newLines.Cast<T>());
                        index += newLines.Length;
                    }
                    else if (typeof(T) == typeof(byte))
                    {
                        byte[] newBytes = Convert.FromHexString(parts[1]);
                        result.RemoveRange(index, replaceCount);
                        result.InsertRange(index, newBytes.Cast<T>());
                        index += newBytes.Length;
                    }
                    else
                    {
                        throw new InvalidOperationException("Unsupported type for patching.");
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unknown operation type: {opType}");
            }
        }

        return [.. result];
    }
}
