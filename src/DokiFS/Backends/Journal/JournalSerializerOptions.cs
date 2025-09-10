using System.Text.Json;
using DokiFS.Internal;

namespace DokiFS.Backends.Journal;

public static class JournalSerializerOptions
{
    static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = {
            new System.Text.Json.Serialization.JsonStringEnumConverter(),
            new VPathJsonConverter()
        },
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
    };

    public static JsonSerializerOptions GetDefault() => SerializerOptions;
}
