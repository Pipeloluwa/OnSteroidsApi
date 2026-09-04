using OnSteroidsApi.Application.Abstractions.Interfaces.IRepositories;
using OnSteroidsApi.Application.Abstractions.Interfaces.IServices;
using OnSteroidsApi.Application.Features.Helpers;
using OnSteroidsApi.Domain.Models.ProxyModels.Requests;
using OnSteroidsApi.Domain.Models.ProxyModels.Responses;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace OnSteroidsApi.Application.Services
{
    public class ProxyService(
            IProxyRepository _proxyRepository,
            IValidator<ProxyRequest> _proxyRequestValidator,
            ILogger<ProxyService> _logger
        ) : IProxyService
    {
        public async Task<(int, object)> ForwardRequestAsync(ProxyRequest proxyRequest, CancellationToken cancellationToken = default)
        {
            var validationResult = await _proxyRequestValidator.ValidateAsync(proxyRequest, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Proxy request validation failed: {Errors}",
                    validationResult.Errors
                );
                var errorMessages = validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
                return BaseResponseHelpers.ReturnValidationSyntaxError("Invalid proxy request", errorMessages);
            }

            _logger.LogInformation("Forwarding {Method} request to {Url}", proxyRequest.Method, proxyRequest.Url);

            var response = await _proxyRepository.ForwardRequestAsync(proxyRequest, cancellationToken);

            _logger.LogInformation(
                "Received response from {Url} — Status: {StatusCode}, ResponseTime: {ResponseTimeMs}ms",
                proxyRequest.Url, response.StatusCode, response.ResponseTimeMs
            );

            return BaseResponseHelpers.ReturnSuccess<ProxyResponse>("Request forwarded successfully", response);
        }
    }
}
