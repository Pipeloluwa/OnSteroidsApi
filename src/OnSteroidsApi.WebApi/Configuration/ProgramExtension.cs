using OnSteroidsApi.Application.Abstractions.Interfaces.IRepositories;
using OnSteroidsApi.Application.Abstractions.Interfaces.IServices;
using OnSteroidsApi.Application.Features.Helpers;
using OnSteroidsApi.Application.Services;
using OnSteroidsApi.Domain.Constants.Enums;
using OnSteroidsApi.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;

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
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    nameof(CorsEnum._allowFrontend),
                    policy => policy
                        .SetIsOriginAllowed(_ => true)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithExposedHeaders("*")
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
            services.AddHttpClient<IProxyRepository, ProxyRepository>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                });
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
    }
}
