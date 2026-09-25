using System.Diagnostics;
using System.Text;
using OnSteroidsApi.Application.Abstractions.Interfaces.IRepositories;
using OnSteroidsApi.Domain.Models.ProxyModels.Requests;
using OnSteroidsApi.Domain.Models.ProxyModels.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OnSteroidsApi.Infrastructure.Repositories;

public class ProxyRepository(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ProxyRepository> logger
    ) : IProxyRepository
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<ProxyRepository> _logger = logger;


    public async Task<ProxyResponse> ForwardRequestAsync(ProxyRequest proxyRequest, CancellationToken cancellationToken = default)
    {
        var requestId = _httpContextAccessor.HttpContext?.TraceIdentifier
            ?? proxyRequest.Headers.FirstOrDefault(h => h.Key.Equals("X-Request-ID", StringComparison.OrdinalIgnoreCase)).Value
            ?? "N/A";

        var method = proxyRequest.Method?.Trim().ToUpperInvariant() ?? "GET";
        var requestMessage = new HttpRequestMessage(new HttpMethod(method), proxyRequest.Url);

        var requiresOrAllowsBody = method is "POST" or "PUT" or "PATCH" or "DELETE";

        if (proxyRequest.Files != null && proxyRequest.Files.Count > 0 || proxyRequest.FormFields != null && proxyRequest.FormFields.Count > 0)
        {
            var multipartContent = new MultipartFormDataContent();
            if (proxyRequest.FormFields != null)
            {
                foreach (var field in proxyRequest.FormFields)
                {
                    multipartContent.Add(new StringContent(field.Value), field.Key);
                }
            }
            if (proxyRequest.Files != null)
            {
                foreach (var file in proxyRequest.Files)
                {
                    var streamContent = new StreamContent(file.Stream);
                    if (!string.IsNullOrEmpty(file.ContentType))
                    {
                        streamContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(file.ContentType);
                    }
                    multipartContent.Add(streamContent, file.Name, file.FileName);
                }
            }
            requestMessage.Content = multipartContent;
            
            // Remove Content-Type from headers so it doesn't overwrite multipart boundary
            proxyRequest.Headers.Remove("Content-Type");
            proxyRequest.Headers.Remove("content-type");
        }
        else if (!string.IsNullOrEmpty(proxyRequest.Body))
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

        if (!requestMessage.Headers.Contains("X-Request-ID") && requestId != "N/A")
        {
            requestMessage.Headers.TryAddWithoutValidation("X-Request-ID", requestId);
        }

        _logger.LogInformation("[{RequestId}] Sending {Method} request to {Url}", requestId, proxyRequest.Method, proxyRequest.Url);

        // Measure ONLY the target API response time — start stopwatch just before SendAsync
        var stopwatch = Stopwatch.StartNew();
        var clientName = proxyRequest.VerifySsl ? "ProxyClient_Strict" : "ProxyClient_Bypass";
        var client = _httpClientFactory.CreateClient(clientName);
        var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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

