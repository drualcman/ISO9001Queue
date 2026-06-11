using System.Text;

namespace ISO9001Queue.Functions.QueueTriggers;

/// <summary>
/// Deserializes queue messages tolerantly: accepts the payload as raw JSON,
/// and falls back to Base64-decoding it first. This keeps the trigger working
/// regardless of whether the producer (or the host's queue message encoding)
/// sends plain text or Base64.
/// </summary>
internal static class QueueMessageSerializer
{
    public static T? Deserialize<T>(string message)
    {
        // 1. Try the message as-is (plain JSON).
        if (TryDeserialize<T>(message, out T? direct))
            return direct;

        // 2. Fall back to Base64-decoding then JSON.
        if (TryFromBase64(message, out string decoded) &&
            TryDeserialize<T>(decoded, out T? fromBase64))
            return fromBase64;

        // Neither worked: surface the original payload as invalid JSON.
        return JsonSerializer.Deserialize<T>(message);
    }

    private static bool TryDeserialize<T>(string json, out T? result)
    {
        try
        {
            result = JsonSerializer.Deserialize<T>(json);
            return result is not null;
        }
        catch (JsonException)
        {
            result = default;
            return false;
        }
    }

    private static bool TryFromBase64(string value, out string decoded)
    {
        decoded = string.Empty;
        try
        {
            byte[] bytes = Convert.FromBase64String(value.Trim());
            decoded = Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
