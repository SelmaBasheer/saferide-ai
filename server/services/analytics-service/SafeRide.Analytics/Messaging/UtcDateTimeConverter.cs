using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SafeRide.Analytics.Messaging;

/// Java's Instant serialises with nanosecond precision — nine fractional
/// digits. System.Text.Json accepts at most seven and refuses the rest, so
/// every timestamp from a Java service would fail without this.
/// DateTime.Parse truncates instead, which is the right answer here:
/// sub-microsecond precision means nothing to a daily report.
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TryGetDateTime(out var value))
            return value.ToUniversalTime();

        var text =
            reader.GetString()
            ?? throw new JsonException("Expected a date-time string, found null.");

        return DateTime.Parse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal
        );
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options
    ) => writer.WriteStringValue(value);
}
