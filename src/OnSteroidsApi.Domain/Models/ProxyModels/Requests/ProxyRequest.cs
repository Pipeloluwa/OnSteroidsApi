using System.IO;

namespace OnSteroidsApi.Domain.Models.ProxyModels.Requests;

public class ProxyFile
{
    public string Name { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public Stream Stream { get; set; } = Stream.Null;
}

public class ProxyRequest
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Body { get; set; }
    public bool VerifySsl { get; set; } = true;
    public List<KeyValuePair<string, string>>? FormFields { get; set; }
    public List<ProxyFile>? Files { get; set; }
}
