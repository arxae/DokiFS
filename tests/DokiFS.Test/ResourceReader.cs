using System.Reflection;

namespace DokiFS.Tests;

public static class ResourceReader
{
    public static T GetEmbeddedResourceContent<T>(string resourceName)
    {
        Assembly assembly = typeof(ResourceReader).Assembly;
        string? baseNamespace = assembly.GetName().Name;
        string fullResourceName = $"{baseNamespace}.{resourceName}";

        using Stream stream = assembly.GetManifestResourceStream(fullResourceName)
            ?? throw new ArgumentException($"Resource '{fullResourceName}' not found in assembly '{assembly.FullName}'.");

        if (typeof(T) == typeof(string))
        {
            using StreamReader reader = new(stream);
            return (T)(object)reader.ReadToEnd();
        }
        else if (typeof(T) == typeof(byte[]))
        {
            using MemoryStream memoryStream = new();
            stream.CopyTo(memoryStream);
            return (T)(object)memoryStream.ToArray();
        }
        else if (typeof(T) == typeof(Stream))
        {
            MemoryStream memoryStream = new();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return (T)(object)memoryStream;
        }

        throw new NotSupportedException($"Type {typeof(T)} is not supported for resource reading");
    }

    public static void WriteResourceToFile(string resourceName, string fileName)
    {
        using Stream? resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new ArgumentException($"Resource '{resourceName}' not found.");
        using FileStream file = new(fileName, FileMode.Create, FileAccess.Write);
        resource.CopyTo(file);

        // Check existence
        if (File.Exists(fileName) == false)
        {
            throw new IOException("Failed to write resource file");
        }
    }
}
