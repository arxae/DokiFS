using System.Text;

namespace DokiFS.Backends.Journal;

public class JournalEntry
{
    public int Id { get; }
    public JournalActions JournalAction { get; internal set; }
    public object[] ParamStack { get; internal set; }
    public byte[] Data { get; internal set; }
    public byte[] UndoData { get; internal set; }
    public string Description { get; internal set; }

    public JournalEntry(int id, JournalActions journalAction, params object[] paramStack)
        : this(id, journalAction, null, paramStack)
    {
    }

    public JournalEntry(int id, JournalActions journalAction, string description, params object[] paramStack)
    {
        Id = id;
        JournalAction = journalAction;
        ParamStack = paramStack;

        if (description != null)
        {
            Description = description;
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new($"{Id} - {JournalAction}: ");
        sb.Append(string.Join(", ", ParamStack));

        if (Description != null)
        {
            sb.Append(Description != null ? $" | {Description}" : string.Empty);
        }

        sb.Append($" | Data size: {Data?.Length ?? 0} bytes");

        if (Data != null)
        {
            int previewLength = Math.Min(Data.Length, 25);
            string textPreview = Encoding.UTF8.GetString(Data, 0, previewLength)
                .Replace("\r\n", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty);
            sb.Append($" | Data: {textPreview}");
        }

        return sb.ToString();
    }
}
