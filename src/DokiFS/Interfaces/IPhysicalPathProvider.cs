namespace DokiFS.Interfaces;

public interface IPhysicalPathProvider
{
    string RootPhysicalPath { get; }

    bool TryGetPhysicalPath(VPath path, out string physicalPath);
}
