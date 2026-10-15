using System.Text.Json;
using System.Text.Json.Serialization;

namespace WakaBar.Core;

/// <summary>Saves the latest snapshot between launches, so the app shows data right away.</summary>
public static class SnapshotJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new EquatableArrayConverterFactory() },
    };

    public static string Serialize(Snapshot snapshot) => JsonSerializer.Serialize(snapshot, Options);

    /// <summary>The saved snapshot, or null if the text is missing, corrupt or from an incompatible version.</summary>
    public static Snapshot? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Snapshot>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Writes <see cref="EquatableArray{T}"/> as a plain JSON array.</summary>
    private sealed class EquatableArrayConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(EquatableArray<>);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(EquatableArrayConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
    }

    private sealed class EquatableArrayConverter<T> : JsonConverter<EquatableArray<T>>
    {
        public override EquatableArray<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(JsonSerializer.Deserialize<List<T>>(ref reader, options) ?? throw new JsonException("Expected an array."));

        public override void Write(Utf8JsonWriter writer, EquatableArray<T> value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.ToList(), options);
    }
}
