using DokiFS.Interfaces;

namespace DokiFS.Backends.AssemblyResource;

public class AssemblyFile : VfsEntry
{
    public string ResourcePath { get; set; }

    public AssemblyFile(VPath path, string resourcePath)
        : base(path, VfsEntryType.File, VfsEntryProperties.Readonly)
    {
        ResourcePath = resourcePath;
    }
}
