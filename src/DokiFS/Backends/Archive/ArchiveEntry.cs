using DokiFS.Interfaces;

namespace DokiFS.Backends.Archive;

public class ArchiveEntry : VfsEntry
{
    public long CompressedSize { get; set; }
    public double CompressionRatio => Size == 0 ? 1d : (double)CompressedSize / Size;

    public ArchiveEntry(VPath path, VfsEntryType type, VfsEntryProperties properties = VfsEntryProperties.None)
        : base(path, type, properties)
    {

    }
}
