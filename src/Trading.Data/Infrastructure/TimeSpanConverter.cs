using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trading.Data.Infrastructure;

/// <summary>
/// TimeSpan JSON converter for Cosmos DB
/// </summary>
public class TimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return string.IsNullOrEmpty(value) ? TimeSpan.Zero : TimeSpan.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
