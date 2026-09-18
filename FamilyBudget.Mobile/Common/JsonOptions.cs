using System.Text.Json;

namespace FamilyBudget.Mobile.Common;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = CreateDefault();

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            // The API's Zod schemas use `.optional()` (field absent) for nullable request fields
            // like `note`, not `.nullable()` -- an explicit `null` fails validation ("Expected
            // string, received null"), so omit null properties entirely rather than serializing them.
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new FlexibleInt64JsonConverter());
        return options;
    }
}
