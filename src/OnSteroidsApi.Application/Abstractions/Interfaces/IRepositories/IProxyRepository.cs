using OnSteroidsApi.Domain.Models.ProxyModels.Requests;
using OnSteroidsApi.Domain.Models.ProxyModels.Responses;

namespace OnSteroidsApi.Application.Abstractions.Interfaces.IRepositories
{
    public interface IProxyRepository
    {
        Task<ProxyResponse> ForwardRequestAsync(ProxyRequest request, CancellationToken cancellationToken = default);
    }
}
