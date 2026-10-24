namespace OnSteroidsApi.Domain.Models.AppSettingsModels;

public class AppSettings
{
    public string? AllowedHosts { get; set; }
    public string? AllowedOrigins { get; set; }
    public SqlSettings? SqlSettings { get; set; } = new();
    public RateLimitPolicySettings? GlobalRateLimiter { get; set; } = new();
    public LogSettings? LogSettings { get; set; } = new();
}

public class SqlSettings
{
    public string? ConnectionString { get; set; }
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

public class LogSettings
{
    public string? MinimumLevel { get; set; } = "Information";
    public string LogDirectory { get; set; } = "Logs";
    public ConsoleLogSettings Console { get; set; } = new();
    public StructuredLogSettings Structured { get; set; } = new();
    public UnstructuredLogSettings Unstructured { get; set; } = new();
    public DatabaseLogSettings Database { get; set; } = new();
}

public class ConsoleLogSettings
{
    public bool Enabled { get; set; } = true;
    public string OutputTemplate { get; set; } = "[{Timestamp:HH:mm:ss} {Level:u3}] [{RequestId}] {Message:lj}{NewLine}{Exception}";
}

public class StructuredLogSettings
{
    public bool Enabled { get; set; } = true;
    public string Folder { get; set; } = "Structured";
    public string FileName { get; set; } = "log-.json";
    public string RollingInterval { get; set; } = "Day";
    public int? RetainedFileCountLimit { get; set; } = 31;
    public long? FileSizeLimitBytes { get; set; } = 104857600; // 100MB
    public bool RollOnFileSizeLimit { get; set; } = true;
}

public class UnstructuredLogSettings
{
    public bool Enabled { get; set; } = true;
    public string Folder { get; set; } = "Unstructured";
    public string FileName { get; set; } = "log-.txt";
    public string RollingInterval { get; set; } = "Day";
    public int? RetainedFileCountLimit { get; set; } = 31;
    public long? FileSizeLimitBytes { get; set; } = 104857600; // 100MB
    public bool RollOnFileSizeLimit { get; set; } = true;
    public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{RequestId}] {Message:lj}{NewLine}{Exception}";
}

public class DatabaseLogSettings
{
    public bool Enabled { get; set; } = true;
    public string StoredProcedure { get; set; } = "spApiLogs_Insert";
}

