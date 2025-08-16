using DokiFS.Interfaces;

namespace DokiFS.Backends.VirtualResource;

public interface IVirtualResourceHandler
{
    bool CanRead { get; }
    bool CanWrite { get; }

    bool HandleExist(VPath path);
    IVfsEntry HandleGetInfo(VPath path);
    IEnumerable<IVfsEntry> HandleListDirectory(VPath path);
    IEnumerable<IVfsEntry> HandleListDirectory(VPath path, VfsEntryType[] filter);
    Stream HandleOpenRead(VPath path);
    Stream HandleOpenWrite(VPath path, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite);
}
