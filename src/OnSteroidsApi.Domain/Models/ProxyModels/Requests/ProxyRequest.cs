namespace OnSteroidsApi.Domain.Models.ProxyModels.Requests;

public class ProxyRequest
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
}
