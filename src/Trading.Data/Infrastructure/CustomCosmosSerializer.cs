using Microsoft.Azure.Cosmos;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trading.Data.Infrastructure;

/// <summary>
/// 自定义 Cosmos DB 序列化器，使用 System.Text.Json 并限制 decimal 精度
/// </summary>
public class CustomCosmosSerializer : CosmosSerializer
{
    private readonly JsonSerializerOptions _serializerOptions;

    public CustomCosmosSerializer()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Converters =
            {
                new DecimalJsonConverter(8),
                new NullableDecimalJsonConverter(8),
                new TimeSpanConverter()
            }
        };
    }

    public override T FromStream<T>(Stream stream)
    {
        using (stream)
        {
            if (stream.CanSeek && stream.Length == 0)
            {
                return default!;
            }

            return JsonSerializer.Deserialize<T>(stream, _serializerOptions)!;
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var streamPayload = new MemoryStream();
        JsonSerializer.Serialize(streamPayload, input, _serializerOptions);
        streamPayload.Position = 0;
        return streamPayload;
    }
}
