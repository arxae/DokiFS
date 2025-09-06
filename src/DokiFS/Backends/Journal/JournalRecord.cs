#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DokiFS.Backends.Journal;

public class JournalRecord
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
    public string? Description { get; init; }
    public JournalOperations Operation { get; }
    public ContentReference? Content { get; init; }
    public JournalParameters Parameters { get;  }

    [JsonConstructor]
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
    public object? this[string key]
    {
        get => propertyStorage.GetValueOrDefault(key);
        set
        {
            if (value == null)
            {
                propertyStorage.Remove(key);
            }
            else
            {
                propertyStorage[key] = value;
            }
        }
    }

    public T? Get<T>(string key)
    {
        if (propertyStorage.TryGetValue(key, out object? value) == false)
        {
            return default;
        }

        return value switch
        {
            T typedValue => typedValue,
            JsonElement jsonElement => jsonElement.Deserialize<T>(),
            _ => (T?)Convert.ChangeType(value, typeof(T))
        };
    }

    public void Set<T>(string key, T value) => this[key] = value;
    public bool Contains(string key) => propertyStorage.ContainsKey(key);

    [JsonExtensionData]
    Dictionary<string, object> propertyStorage { get; set; } = [];
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
