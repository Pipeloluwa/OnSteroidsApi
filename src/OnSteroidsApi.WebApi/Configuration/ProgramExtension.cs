using OnSteroidsApi.Application.Abstractions.Interfaces.IRepositories;
using OnSteroidsApi.Application.Abstractions.Interfaces.IServices;
using OnSteroidsApi.Application.Features.Helpers;
using OnSteroidsApi.Application.Services;
using OnSteroidsApi.Domain.Constants.Enums;
using OnSteroidsApi.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Serilog;
using OnSteroidsApi.Domain.Models.AppSettingsModels;
using OnSteroidsApi.Domain.Models.Common.BaseModels.Responses;

namespace OnSteroidsApi.WebApi.Configuration
{
    public static class ProgramExtension
    {

        /// <summary>
        /// Configures Kestrel to allow synchronous I/O (required for certain middleware scenarios).
        /// </summary>
        public static void AddKestrelConfig(this WebApplicationBuilder builder)
        {
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
        }


        /// <summary>
        /// Registers controllers with custom API behaviour
        /// BaseErrorResponse for model-binding / deserialisation errors.
        /// </summary>
        public static void AddControllerConfig(this WebApplicationBuilder builder)
        {
            builder.Services
                .AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = MiddlewareHelpers.ReturnValidationResponse;
                });
        }


        /// <summary>
        /// Registers Swagger/OpenAPI generation services.
        /// </summary>
        public static void AddSwaggerConfig(this WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
        }


        /// <summary>
        /// Registers the CORS policy
        /// Allows any origin / method / header (appropriate for a local dev proxy tool).
        /// </summary>
        public static void AddProjectCors(this WebApplicationBuilder builder)
        {
            var allowedOriginsString = builder.Configuration.GetValue<string>("AppSettings:AllowedOrigins");
            var allowedOrigins = string.IsNullOrWhiteSpace(allowedOriginsString) 
                ? Array.Empty<string>() 
                : allowedOriginsString.Split(',').Select(o => o.Trim()).ToArray();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    nameof(CorsEnum._allowFrontend),
                    policy => 
                    {
                        if (allowedOrigins.Length > 0 && allowedOrigins[0] != "*")
                        {
                            policy.WithOrigins(allowedOrigins);
                        }
                        else
                        {
                            policy.SetIsOriginAllowed(_ => true);
                        }

                        policy.AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials()
                              .WithExposedHeaders("*");
                    }
                );
            });
        }


        /// <summary>
        /// Registers application services and infrastructure repositories.
        /// </summary>
        public static void AddProjectServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            // Application services
            services.AddScoped<IProxyService, ProxyService>();

            // Infrastructure repositories — HttpClient with SSL bypass for the proxy
            services.AddScoped<IProxyRepository, ProxyRepository>();

            services.AddHttpClient("ProxyClient_Bypass")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                });

            services.AddHttpClient("ProxyClient_Strict");
        }


        /// <summary>
        /// Registers all FluentValidation validators from the Application assembly.
        /// </summary>
        public static void AddProjectValidators(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<IProxyService>();
        }


        /// <summary>
        /// Configures Serilog as the logging provider.
        /// </summary>
        public static void AddSerilogConfig(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{RequestId}] {Message:lj}{NewLine}{Exception}");
            });
        }
        public static void AddProjectRateLimiter(this WebApplicationBuilder builder)
        {
            var globalRateLimit = builder.Configuration
                .GetSection("AppSettings:GlobalRateLimiter")
                .Get<RateLimitPolicySettings>() ?? new RateLimitPolicySettings
                {
                    PermitLimit = 300,
                    WindowMinutes = 1,
                    QueueLimit = 2,
                    ErrorMessage = "Too many requests. Please try again after {retryAfterMinutes} minute(s)."
                };

            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = Math.Max(1, globalRateLimit.PermitLimit),
                            QueueLimit = Math.Max(0, globalRateLimit.QueueLimit),
                            Window = TimeSpan.FromMinutes(Math.Max(1, globalRateLimit.WindowMinutes))
                        }));

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    var http = context.HttpContext;

                    // Work out how long until the client may retry
                    TimeSpan retryAfter = TimeSpan.FromMinutes(globalRateLimit.WindowMinutes);
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var leaseRetryAfter))
                    {
                        retryAfter = leaseRetryAfter;
                    }
                    var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                    var retryAfterMinutes = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes));

                    var template = globalRateLimit.ErrorMessage
                        ?? "Too many requests. Please try again after {retryAfterMinutes} minute(s).";
                    var message = template.Replace("{retryAfterMinutes}", retryAfterMinutes.ToString());

                    http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    http.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
                    await http.Response.WriteAsJsonAsync(
                        new BaseErrorResponse("Too Many Requests", "429", message, [message]),
                        cancellationToken);
                };
            });
        }
    }
}

