using FluentValidation;
using OnSteroidsApi.Domain.Models.ProxyModels.Requests;

namespace OnSteroidsApi.Application.Features.Validations
{
    public class ProxyRequestValidator : AbstractValidator<ProxyRequest>
    {
        private static readonly HashSet<string> ValidMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"
        };

        public ProxyRequestValidator()
        {
            RuleFor(x => x.Url)
                .NotEmpty().WithMessage("Url is required")
                .Must(BeAValidUri).WithMessage("Url must be a valid absolute URI");

            RuleFor(x => x.Method)
                .NotEmpty().WithMessage("Method is required")
                .Must(BeAValidHttpMethod).WithMessage("Method must be a valid HTTP method (GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS)");
        }

        private static bool BeAValidUri(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static bool BeAValidHttpMethod(string method)
        {
            return ValidMethods.Contains(method);
        }
    }
}
