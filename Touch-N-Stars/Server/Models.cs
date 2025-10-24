using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TouchNStars.Server;

public class NullableDoubleConverter : JsonConverter<double?>
{
    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            if (double.TryParse(stringValue, out double result))
            {
                return result;
            }
            return null;
        }
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDouble();
        }
        return null;
    }

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class FavoriteTarget
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Wird automatisch gesetzt
    public string Name { get; set; }
    public double Ra { get; set; }
    public double Dec { get; set; }
    public string RaString { get; set; }
    public string DecString { get; set; }
    [JsonConverter(typeof(NullableDoubleConverter))]
    public double? Rotation { get; set; }
}

public class Setting
{
    public string Key { get; set; }
    public string Value { get; set; }
}

public class ApiResponse
{
    public bool Success { get; set; }
    public object Response { get; set; }
    public string Error { get; set; }
    public int StatusCode { get; set; }
    public string Type { get; set; }
}
