namespace OnSteroidsApi.Domain.Models.AppSettingsModels;

public class AppSettings
{
    public string? AllowedHosts { get; set; }
    public string? AllowedOrigins { get; set; }
    public RateLimitPolicySettings? GlobalRateLimiter { get; set; } = new();
}

public class RateLimitPolicySettings
{
    /// <summary>Maximum number of requests allowed per client within the window.</summary>
    public int PermitLimit { get; set; } = 300;

    /// <summary>Length of the fixed window in minutes.</summary>
    public int WindowMinutes { get; set; } = 1;

    /// <summary>Maximum number of queued requests allowed.</summary>
    public int QueueLimit { get; set; } = 2;

    /// <summary>Message returned when the limit is exceeded. Supports the {retryAfterMinutes} placeholder.</summary>
    public string? ErrorMessage { get; set; }
}
