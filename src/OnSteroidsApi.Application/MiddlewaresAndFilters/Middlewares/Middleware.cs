using OnSteroidsApi.Application.Features.Helpers;
using OnSteroidsApi.Domain.Models.Common.ExceptionModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Mime;
using System.Text.Json;

namespace OnSteroidsApi.Application.MiddlewaresAndFilters.Middlewares
{
    public class Middleware(
            RequestDelegate next,
            ILogger<Middleware> logger
        )
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<Middleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = context.Request.Headers["X-Request-ID"].FirstOrDefault()
                ?? context.Request.Headers["Request-Id"].FirstOrDefault()
                ?? context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            context.TraceIdentifier = requestId;
            context.Response.Headers["X-Request-ID"] = requestId;

            using (_logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId }))
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var requestHeaders = JsonSerializer.Serialize(context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));
                
                context.Request.EnableBuffering();
                var requestBodyText = string.Empty;
                if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
                {
                    using (var reader = new StreamReader(context.Request.Body, System.Text.Encoding.UTF8, leaveOpen: true))
                    {
                        requestBodyText = await reader.ReadToEndAsync();
                        context.Request.Body.Position = 0;
                    }
                }
                
                _logger.LogInformation("Incoming HTTP {Method} {Path} \nHeaders: {Headers} \nBody: {Body}", 
                    context.Request.Method, context.Request.Path, requestHeaders, requestBodyText);

                var originalResponseBodyStream = context.Response.Body;
                using var responseBodyMemoryStream = new MemoryStream();
                context.Response.Body = responseBodyMemoryStream;

                try
                {
                    await _next(context);
                }
                catch (ProxyRequestException ex)
                {
                    await HandleProxyRequestException(context, ex, requestId);
                }
                catch (Exception ex)
                {
                    await HandleExceptionAsync(context, ex, requestId);
                }
                finally
                {
                    var responseBodyText = string.Empty;
                    if (responseBodyMemoryStream.CanRead)
                    {
                        responseBodyMemoryStream.Position = 0;
                        using (var reader = new StreamReader(responseBodyMemoryStream, System.Text.Encoding.UTF8, leaveOpen: true))
                        {
                            responseBodyText = await reader.ReadToEndAsync();
                        }
                        responseBodyMemoryStream.Position = 0;
                        _logger.LogInformation("Outgoing Response Body: {Body}", responseBodyText);
                        
                        await responseBodyMemoryStream.CopyToAsync(originalResponseBodyStream);
                    }
                    else
                    {
                        var bytes = responseBodyMemoryStream.ToArray();
                        responseBodyText = System.Text.Encoding.UTF8.GetString(bytes);
                        _logger.LogInformation("Outgoing Response Body: {Body}", responseBodyText);
                        
                        await originalResponseBodyStream.WriteAsync(bytes, 0, bytes.Length);
                    }

                    context.Response.Body = originalResponseBodyStream;

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "Completed HTTP {Method} {Path} with status {StatusCode} in {ElapsedMilliseconds}ms",
                        context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds
                    );
                }
            }
        }


        private async Task HandleProxyRequestException(HttpContext context, ProxyRequestException ex, string requestId)
        {
            if (!context.Response.HasStarted)
            {
                _logger.LogError(ex, "Proxy request failed: {Message}", ex.Message);

                (int statusCode, object error) = BaseResponseHelpers.ReturnValidationSemanticErrorData(ex.Message, null);
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsync(JsonSerializer.Serialize(error));
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex, string requestId)
        {
            if (!context.Response.HasStarted)
            {
                _logger.LogError(ex, "Something went wrong while handling {Path}", context.Request.Path);

                (int statusCode, object error) = BaseResponseHelpers.ReturnServerErrorData("Something went wrong", null);
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsync(JsonSerializer.Serialize(error));
            }
        }
    }

}
