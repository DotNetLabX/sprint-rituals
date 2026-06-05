using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jira.Contracts;

public class JiraDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly string[] Formats =
    [
        "yyyy-MM-dd'T'HH:mm:ss.fffzzz",
        "yyyy-MM-dd'T'HH:mm:ss.fffffffzzz",
        "yyyy-MM-dd'T'HH:mm:sszzz",
        "yyyy-MM-dd'T'HH:mm:ss.fffzz",
        "yyyy-MM-dd'T'HH:mm:ss.fffffffzz",
        "yyyy-MM-dd'T'HH:mm:sszz",
    ];

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString()!;
        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            return dto.UtcDateTime;
        if (DateTimeOffset.TryParseExact(s, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dto))
            return dto.UtcDateTime;
        return DateTime.Parse(s, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK"));
}

public class JiraNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private readonly JiraDateTimeConverter _inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        return _inner.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else _inner.Write(writer, value.Value, options);
    }
}

public class JiraChangelog
{
    public int MaxResults { get; set; }
    public int Total { get; set; }
    public int StartAt { get; set; }
    public List<JiraHistory> Histories { get; set; } = [];
}

public class JiraHistory
{
    public DateTime Created { get; set; }
    public JiraAuthor Author { get; set; } = new();
    public List<JiraChangeItem> Items { get; set; } = [];
}

public class JiraAuthor
{
    public string AccountId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Dictionary<string, string> AvatarUrls { get; set; } = [];
}

public class JiraChangeItem
{
    public string Field { get; set; } = string.Empty;
    public string? FromString { get; set; }
    [JsonPropertyName("toString")]
    public string? ToStringValue { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
}
