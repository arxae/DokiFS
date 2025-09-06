using System.Text.Json;
using System.Text.Json.Serialization;

namespace DokiFS.Internal;

internal sealed class VPathJsonConverter : JsonConverter<VPath>
{
    public override void Write(Utf8JsonWriter writer, VPath value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.FullPath);

    // This method is called during deserialization
    public override VPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // We expect the JSON value to be a string.
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a string for VPath but got {reader.TokenType}.");
        }
        string path = reader.GetString();
        if (path == null)
        {
            return VPath.Empty;
        }

        return new VPath(path);
    }
}
