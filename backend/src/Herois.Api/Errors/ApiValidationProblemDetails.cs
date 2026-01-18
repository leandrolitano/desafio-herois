using System.Text.Json.Serialization;

namespace Herois.Api.Errors;

/// <summary>
/// RFC7807-compatible validation problem payload (application/problem+json)
/// with an explicit, settable <c>errors</c> field.
/// </summary>
public sealed class ApiValidationProblemDetails
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    [JsonPropertyName("errors")]
    public Dictionary<string, string[]> Errors { get; set; } = new();

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
}
