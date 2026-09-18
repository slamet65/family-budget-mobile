using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FamilyBudget.Mobile.Common;

/// <summary>
/// D1 aggregate expressions can be surfaced by its driver as an integral JSON
/// decimal (for example 200000.0) or as a numeric string. Money is integral in
/// this application, so accept those representations only when no precision is
/// lost and keep the in-app contract as Int64.
/// </summary>
public sealed class FlexibleInt64JsonConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt64(out var integer))
            {
                return integer;
            }

            if (reader.TryGetDecimal(out var number) &&
                number == decimal.Truncate(number) &&
                number is >= long.MinValue and <= long.MaxValue)
            {
                return decimal.ToInt64(number);
            }
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
            {
                return integer;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) &&
                number == decimal.Truncate(number) &&
                number is >= long.MinValue and <= long.MaxValue)
            {
                return decimal.ToInt64(number);
            }
        }

        throw new JsonException("Expected an integer-compatible JSON value.");
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value);
}
