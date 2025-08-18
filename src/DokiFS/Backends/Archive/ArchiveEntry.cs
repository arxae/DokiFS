using System.IO.Compression;
using DokiFS.Interfaces;

namespace DokiFS.Backends.Archive;

public class ArchiveEntry : VfsEntry
{
    public long CompressedSize { get; set; }

    public ArchiveEntry(VPath path, VfsEntryType type, VfsEntryProperties properties = VfsEntryProperties.Default)
        : base(path, type, properties)
    {

    }
}
