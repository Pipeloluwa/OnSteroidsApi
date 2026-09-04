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
            try
            {
                await _next(context);
            }
            catch (ProxyRequestException ex)
            {
                await HandleProxyRequestException(context, ex);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }


        private async Task HandleProxyRequestException(HttpContext context, ProxyRequestException ex)
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

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            if (!context.Response.HasStarted)
            {
                _logger.LogError(ex, "Something went wrong");

                (int statusCode, object error) = BaseResponseHelpers.ReturnServerErrorData("Something went wrong", null);
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsync(JsonSerializer.Serialize(error));
            }
        }
    }

}
