namespace DokiFS.Backends.Journal;

public class JournalEntry
{
    public JournalActions JournalAction { get; set; }
    public object[] ParamStack { get; set; }
    public byte[] Data { get; set; }

    public JournalEntry(JournalActions journalAction, params object[] paramStack)
    {
        JournalAction = journalAction;
        ParamStack = paramStack;
    }

    public override string ToString()
        => $"{JournalAction}: {string.Join(", ", ParamStack)} | Data size: {Data?.Length ?? 0} bytes";
}
