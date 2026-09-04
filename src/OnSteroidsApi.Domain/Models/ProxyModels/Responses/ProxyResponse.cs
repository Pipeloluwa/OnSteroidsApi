namespace OnSteroidsApi.Domain.Models.ProxyModels.Responses;

public class ProxyResponse
{
    public int StatusCode { get; set; }
    public string? StatusText { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }

    /// <summary>
    /// The elapsed milliseconds of the target API call (not the proxy overhead).
    /// </summary>
    public long ResponseTimeMs { get; set; }
}
