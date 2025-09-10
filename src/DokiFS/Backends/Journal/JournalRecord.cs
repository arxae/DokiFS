#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DokiFS.Backends.Journal;

public class JournalRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? Description { get; init; }
    public JournalOperations Operation { get; init; }
    public ContentReference? Content { get; init; }
    public JournalParameters Parameters { get; init; }

    [JsonConstructor]
    public JournalRecord(Guid id,
                        DateTimeOffset timestamp,
                        string? description,
                        JournalOperations operation,
                        ContentReference? content,
                        JournalParameters? parameters)
    {
        Id = id;
        Timestamp = timestamp;
        Description = description;
        Operation = operation;
        Content = content;
        Parameters = parameters ?? new JournalParameters();
    }

    public JournalRecord(JournalOperations operation)
    {
        Operation = operation;
        Parameters = new JournalParameters();
    }

    public JournalRecord(JournalOperations operation, JournalParameters parameters)
        : this(operation)
    {
        Parameters = parameters;
    }

    public JournalRecord(JournalOperations operation, Action<JournalParameters> configureParameters)
        : this(operation)
    {
        ArgumentNullException.ThrowIfNull(configureParameters);
        configureParameters(Parameters);
    }

    public override string ToString()
        => $"{Operation}" + (Description != null ? $" -> {Description}" : "");
}

public class JournalParameters
{
    static readonly JsonSerializerOptions EnumJsonOptions = JournalSerializerOptions.GetDefault();

    public object? this[string key]
    {
        get => PropertyStorage.GetValueOrDefault(key);
        set
        {
            if (value == null)
                PropertyStorage.Remove(key);
            else
                PropertyStorage[key] = value;
        }
    }

    public T? Get<T>(string key)
    {
        if (PropertyStorage.TryGetValue(key, out object? value) == false)
        {
            return default;
        }

        // Already correct type
        if (value is T typed)
        {
            return typed;
        }

        Type targetType = typeof(T);

        if (value is JsonElement je)
        {
            return ConvertFromJsonElement<T>(je);
        }

        // Enum conversions (string / numeric / boxed)
        if (targetType.IsEnum)
        {
            return (T?)ConvertEnum(targetType, value);
        }

        // Fallback
        return (T?)Convert.ChangeType(value, targetType);
    }

    public void Set<T>(string key, T value) => this[key] = value;
    public bool Contains(string key) => PropertyStorage.ContainsKey(key);

    [JsonExtensionData]
    public Dictionary<string, object> PropertyStorage { get; set; } = [];

    static T? ConvertFromJsonElement<T>(JsonElement je)
    {
        Type targetType = typeof(T);

        if (targetType.IsEnum)
        {
            if (je.ValueKind == JsonValueKind.String)
            {
                return (T)Enum.Parse(targetType, je.GetString()!, ignoreCase: true);
            }

            if (je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out int enumInt))
            {
                return (T)Enum.ToObject(targetType, enumInt);
            }
        }

        return je.Deserialize<T>(EnumJsonOptions);
    }

    static object ConvertEnum(Type enumType, object value)
    {
        switch (value)
        {
            case string s:
                return Enum.Parse(enumType, s, ignoreCase: true);
            case JsonElement je:
                // (Normally handled earlier, but kept for completeness)
                if (je.ValueKind == JsonValueKind.String)
                    return Enum.Parse(enumType, je.GetString()!, ignoreCase: true);
                if (je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out int enumInt))
                    return Enum.ToObject(enumType, enumInt);
                break;
            case IConvertible conv:
                return Enum.ToObject(enumType, conv.ToInt32(null));
        }

        // Fallback: try direct
        if (Enum.IsDefined(enumType, value))
        {
            return value;
        }

        throw new InvalidCastException($"Cannot convert value '{value}' to enum {enumType.Name}.");
    }
}

public sealed class ContentReference
{
    public string? ContentId { get; init; }
    public int Version { get; init; }
    public ContentType Type { get; init; }
    public long Size { get; init; }
    public string? ContentHash { get; init; }
    public long? StreamOffset { get; init; }
    public long? Length { get; init; }
    public byte[]? Data { get; init; }
}

public enum ContentType
{
    BaseContent,
    Patch,
    Reference,
    Partial
}
