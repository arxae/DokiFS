namespace DokiFS.Backends.Journal;

public class JournalInterruptedException : Exception
{
    public JournalInterruptedException(JournalEntry entry)
        : base($"Something went wrong while applying the journal entry: {entry}") { }
}
