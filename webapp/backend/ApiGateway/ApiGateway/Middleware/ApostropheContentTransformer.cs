using System.Text;
using System.Text.Json;

namespace ApiGateway.Middleware;

public static class ApostropheContentTransformer
{
    private const string ApostropheEntity = "&#39;";

    public static bool CanTransform(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var mediaType = contentType.Split(';', 2)[0].Trim();
        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase)
            || mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                && !mediaType.Equals("text/event-stream", StringComparison.OrdinalIgnoreCase);
    }

    public static byte[] EncodeForBackend(ReadOnlySpan<byte> body, string? contentType) =>
        Transform(body, contentType, value => value.Replace("'", ApostropheEntity, StringComparison.Ordinal));

    public static byte[] DecodeForClient(ReadOnlySpan<byte> body, string? contentType) =>
        Transform(body, contentType, value => value.Replace(ApostropheEntity, "'", StringComparison.Ordinal));

    private static byte[] Transform(
        ReadOnlySpan<byte> body,
        string? contentType,
        Func<string, string> transformString)
    {
        if (body.IsEmpty || !CanTransform(contentType))
        {
            return body.ToArray();
        }

        var mediaType = contentType!.Split(';', 2)[0].Trim();
        if (!mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            && !mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
        {
            var text = Encoding.UTF8.GetString(body);
            return Encoding.UTF8.GetBytes(transformString(text));
        }

        try
        {
            using var document = JsonDocument.Parse(body.ToArray());
            using var output = new MemoryStream();
            using (var writer = new Utf8JsonWriter(output))
            {
                WriteElement(writer, document.RootElement, transformString);
            }

            return output.ToArray();
        }
        catch (JsonException)
        {
            // Preserve malformed JSON so the downstream service can return its normal validation response.
            return body.ToArray();
        }
    }

    private static void WriteElement(
        Utf8JsonWriter writer,
        JsonElement element,
        Func<string, string> transformString)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteElement(writer, property.Value, transformString);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteElement(writer, item, transformString);
                }
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(transformString(element.GetString() ?? string.Empty));
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}
