namespace ApiAggregator.Api.Models;

/// <summary>
/// Standardized error response model used across all controllers and middleware
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
