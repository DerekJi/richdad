using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trading.Backtest.Data.Infrastructure;

/// <summary>
/// Decimal JSON 转换器，限制小数位数避免Cosmos DB序列化问题
/// </summary>
public class DecimalJsonConverter : JsonConverter<decimal>
{
    private readonly int _decimalPlaces;

    public DecimalJsonConverter(int decimalPlaces = 8)
    {
        _decimalPlaces = decimalPlaces;
    }

    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDecimal();
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        // 四舍五入到指定小数位，避免过多无意义的0
        writer.WriteNumberValue(Math.Round(value, _decimalPlaces));
    }
}

/// <summary>
/// 可空 Decimal JSON 转换器
/// </summary>
public class NullableDecimalJsonConverter : JsonConverter<decimal?>
{
    private readonly int _decimalPlaces;

    public NullableDecimalJsonConverter(int decimalPlaces = 8)
    {
        _decimalPlaces = decimalPlaces;
    }

    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.Null ? null : reader.GetDecimal();
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(Math.Round(value.Value, _decimalPlaces));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
