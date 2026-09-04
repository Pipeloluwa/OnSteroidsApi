using OnSteroidsApi.Domain.Models.ProxyModels.Requests;

namespace OnSteroidsApi.Application.Abstractions.Interfaces.IServices
{
    public interface IProxyService
    {
        Task<(int, object)> ForwardRequestAsync(ProxyRequest proxyRequest, CancellationToken cancellationToken = default);
    }
}
