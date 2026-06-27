using System.Text.Json;

namespace Nodlyn.CustomComponent;

/// <summary>
/// Shared utility for extracting fields from pipeline input objects.
///
/// Pipeline input can arrive in several forms depending on what the
/// upstream node produced:
///   - Dictionary&lt;string, object?&gt; — the most common case
///   - A JSON string — parsed automatically
///   - A plain string — returned as-is when the field name doesn't apply
///   - null — when there is no upstream connection
///
/// Use this helper in your executors instead of duplicating extraction logic.
/// </summary>
internal static class PipelineHelper
{
    /// <summary>
    /// Extracts a named field from the pipeline input object.
    /// Returns null if the input is null or the field is not found.
    /// </summary>
    /// <param name="input">The raw input from <c>ResolveInput(node, context)</c>.</param>
    /// <param name="fieldName">The name of the field to extract.</param>
    public static string? ExtractField(object? input, string fieldName)
    {
        if (input is null) return null;

        // Most common: upstream node returned a dictionary
        if (input is Dictionary<string, object?> dict)
        {
            dict.TryGetValue(fieldName, out var val);
            return val?.ToString();
        }

        // Upstream returned a JSON string — try to parse and extract the field
        if (input is string str)
        {
            try
            {
                using var doc = JsonDocument.Parse(str);
                if (doc.RootElement.TryGetProperty(fieldName, out var prop))
                    return prop.ToString();
            }
            catch (JsonException)
            {
                // Not valid JSON — treat the entire string as the value
                return str;
            }
        }

        return input.ToString();
    }
}
