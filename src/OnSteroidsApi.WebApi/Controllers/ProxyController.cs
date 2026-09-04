using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnSteroidsApi.Application.Abstractions.Interfaces.IServices;
using OnSteroidsApi.Domain.Models.Common.BaseModels.Responses;
using OnSteroidsApi.Domain.Models.ProxyModels.Requests;
using OnSteroidsApi.Domain.Models.ProxyModels.Responses;

namespace OnSteroidsApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public class ProxyController(
    IProxyService _proxyService
  ) : ControllerBase
{

    /// <summary>
    /// Forwards an HTTP request to the specified target URL and returns the response.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BaseSuccessResponse<ProxyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(BaseErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Forward([FromBody] ProxyRequest request)
    {
        (int status, object data) = await _proxyService.ForwardRequestAsync(request, HttpContext.RequestAborted);
        return StatusCode(status, data);
    }
}
