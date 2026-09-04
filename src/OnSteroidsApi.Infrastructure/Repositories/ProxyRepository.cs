using System.Diagnostics;
using System.Text;
using OnSteroidsApi.Application.Abstractions.Interfaces.IRepositories;
using OnSteroidsApi.Domain.Models.ProxyModels.Requests;
using OnSteroidsApi.Domain.Models.ProxyModels.Responses;
using Microsoft.Extensions.Logging;

namespace OnSteroidsApi.Infrastructure.Repositories;

public class ProxyRepository(
        HttpClient httpClient,
        ILogger<ProxyRepository> logger
    ) : IProxyRepository
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<ProxyRepository> _logger = logger;


    public async Task<ProxyResponse> ForwardRequestAsync(ProxyRequest proxyRequest, CancellationToken cancellationToken = default)
    {
        var requestMessage = new HttpRequestMessage(new HttpMethod(proxyRequest.Method), proxyRequest.Url);

        if (!string.IsNullOrEmpty(proxyRequest.Body) && (proxyRequest.Method == "POST" || proxyRequest.Method == "PUT" || proxyRequest.Method == "PATCH"))
        {
            var contentType = proxyRequest.Headers.TryGetValue("Content-Type", out var ct) ? ct : "application/json";
            requestMessage.Content = new StringContent(proxyRequest.Body, Encoding.UTF8, contentType);
        }

        foreach (var header in proxyRequest.Headers)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
            if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
            if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;

            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        _logger.LogInformation("Sending {Method} request to {Url}", proxyRequest.Method, proxyRequest.Url);

        // Measure ONLY the target API response time — start stopwatch just before SendAsync
        var stopwatch = Stopwatch.StartNew();
        var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        stopwatch.Stop();

        var responseTimeMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "Target API responded in {ResponseTimeMs}ms with status {StatusCode}",
            responseTimeMs, (int)response.StatusCode
        );

        var proxyResponse = new ProxyResponse
        {
            StatusCode = (int)response.StatusCode,
            StatusText = response.ReasonPhrase,
            Headers = new Dictionary<string, string>(),
            Body = await response.Content.ReadAsStringAsync(cancellationToken),
            ResponseTimeMs = responseTimeMs
        };

        foreach (var header in response.Headers)
        {
            proxyResponse.Headers[header.Key] = string.Join(", ", header.Value);
        }
        foreach (var header in response.Content.Headers)
        {
            proxyResponse.Headers[header.Key] = string.Join(", ", header.Value);
        }

        return proxyResponse;
    }
}
