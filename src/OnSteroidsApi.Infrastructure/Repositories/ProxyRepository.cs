using System.Diagnostics;
using System.Text;
using OnSteroidsApi.Application.Abstractions.Interfaces.IRepositories;
using OnSteroidsApi.Domain.Models.ProxyModels.Requests;
using OnSteroidsApi.Domain.Models.ProxyModels.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OnSteroidsApi.Infrastructure.Repositories;

public class ProxyRepository(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProxyRepository> logger
    ) : IProxyRepository
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<ProxyRepository> _logger = logger;


    public async Task<ProxyResponse> ForwardRequestAsync(ProxyRequest proxyRequest, CancellationToken cancellationToken = default)
    {
        var requestId = _httpContextAccessor.HttpContext?.TraceIdentifier
            ?? proxyRequest.Headers.FirstOrDefault(h => h.Key.Equals("X-Request-ID", StringComparison.OrdinalIgnoreCase)).Value
            ?? "N/A";

        var method = proxyRequest.Method?.Trim().ToUpperInvariant() ?? "GET";
        var requestMessage = new HttpRequestMessage(new HttpMethod(method), proxyRequest.Url)
        {
            Version = new Version(1, 1)
        };

        var requiresOrAllowsBody = method is "POST" or "PUT" or "PATCH" or "DELETE";

        if (!string.IsNullOrEmpty(proxyRequest.Body))
        {
            var contentType = proxyRequest.Headers.TryGetValue("Content-Type", out var ct) && !string.IsNullOrWhiteSpace(ct)
                ? ct
                : "application/json";

            var stringContent = new StringContent(proxyRequest.Body, Encoding.UTF8);
            try
            {
                stringContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
            }
            catch
            {
                stringContent.Headers.TryAddWithoutValidation("Content-Type", contentType);
            }
            requestMessage.Content = stringContent;
        }
        else if (requiresOrAllowsBody)
        {
            // Send an empty content payload with Content-Length: 0 for POST, PUT, PATCH, DELETE to eliminate the HTTP 411 Length Required error on servers requiring chunked or content length.
            var emptyContent = new ByteArrayContent(Array.Empty<byte>());
            emptyContent.Headers.ContentLength = 0;
            if (proxyRequest.Headers.TryGetValue("Content-Type", out var ct) && !string.IsNullOrWhiteSpace(ct))
            {
                emptyContent.Headers.TryAddWithoutValidation("Content-Type", ct);
            }
            requestMessage.Content = emptyContent;
        }

        foreach (var header in proxyRequest.Headers)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
            if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
            if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;

            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value))
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (!requestMessage.Headers.Contains("User-Agent"))
        {
            requestMessage.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        }
        if (!requestMessage.Headers.Contains("Accept"))
        {
            requestMessage.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");
        }
        if (!requestMessage.Headers.Contains("Accept-Language"))
        {
            requestMessage.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        }

        if (!requestMessage.Headers.Contains("sec-ch-ua"))
        {
            requestMessage.Headers.TryAddWithoutValidation("sec-ch-ua", "\"Chromium\";v=\"122\", \"Not(A:Brand\";v=\"24\", \"Google Chrome\";v=\"122\"");
            requestMessage.Headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
            requestMessage.Headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
            requestMessage.Headers.TryAddWithoutValidation("sec-fetch-dest", "empty");
            requestMessage.Headers.TryAddWithoutValidation("sec-fetch-mode", "cors");
            requestMessage.Headers.TryAddWithoutValidation("sec-fetch-site", "cross-site");
        }

        if (!requestMessage.Headers.Contains("X-Request-ID") && requestId != "N/A")
        {
            requestMessage.Headers.TryAddWithoutValidation("X-Request-ID", requestId);
        }

        _logger.LogInformation("[{RequestId}] Sending {Method} request to {Url}", requestId, proxyRequest.Method, proxyRequest.Url);

        // Measure ONLY the target API response time — start stopwatch just before SendAsync
        var stopwatch = Stopwatch.StartNew();
        var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        stopwatch.Stop();

        var responseTimeMs = stopwatch.ElapsedMilliseconds;

        _logger.LogInformation(
            "[{RequestId}] Target API responded in {ResponseTimeMs}ms with status {StatusCode}",
            requestId, responseTimeMs, (int)response.StatusCode
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
