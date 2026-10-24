using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OnSteroidsApi.Application.Abstractions.Interfaces.IDapper;
using OnSteroidsApi.Domain.Models.AppSettingsModels;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;

namespace OnSteroidsApi.Application.MiddlewaresAndFilters.Middlewares;

public class RequestResponseLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestResponseLoggingMiddleware> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<AppSettings>? appSettings = null
)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly AppSettings? _appSettings = appSettings?.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        var dbLoggingEnabled = _appSettings?.LogSettings?.Database?.Enabled ?? true;
        if (!dbLoggingEnabled)
        {
            await _next(context);
            return;
        }

        var requestBody = await GetRequestBodyAsync(context.Request);
        var method = context.Request.Method;
        var path = (context.Request.Path.Value ?? string.Empty) + (context.Request.QueryString.Value ?? string.Empty);
        if (path.Length > 255) path = path[..255];
        var traceId = context.TraceIdentifier;

        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        await _next(context);

        var responseBody = await GetResponseBodyAsync(context.Response);
        await responseBodyStream.CopyToAsync(originalBodyStream);
        
        var statusCode = context.Response.StatusCode;

        // Fire and forget logging task to not block response
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dapperConnection = scope.ServiceProvider.GetRequiredService<IDapperConnection>();

                var sql = _appSettings?.LogSettings?.Database?.StoredProcedure;
                if (string.IsNullOrWhiteSpace(sql))
                {
                    sql = "spApiLogs_Insert";
                }

                var parameters = new Dapper.DynamicParameters();
                parameters.Add("TraceId", traceId);
                parameters.Add("Method", method);
                parameters.Add("Path", path);
                parameters.Add("RequestBody", requestBody);
                parameters.Add("StatusCode", statusCode);
                parameters.Add("ResponseBody", responseBody);

                await dapperConnection.Execute(parameters, sql);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while inserting request or response log into database");
            }
        });
    }

    private static async Task<string> GetRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }

    private static async Task<string> GetResponseBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }
}
