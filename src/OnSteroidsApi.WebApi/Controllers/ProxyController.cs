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

    [HttpPost("multipart")]
    [ProducesResponseType(typeof(BaseSuccessResponse<ProxyResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForwardMultipart([FromForm] IFormCollection form)
    {
        var request = new ProxyRequest
        {
            Url = form["url"].ToString(),
            Method = form["method"].ToString(),
            VerifySsl = bool.TryParse(form["verifySsl"], out var vSsl) ? vSsl : true,
            FormFields = new List<KeyValuePair<string, string>>(),
            Files = new List<ProxyFile>()
        };

        if (form.Files != null)
        {
            foreach (var f in form.Files)
            {
                request.Files.Add(new ProxyFile 
                {
                    Name = f.Name,
                    FileName = f.FileName,
                    ContentType = f.ContentType ?? "",
                    Stream = f.OpenReadStream()
                });
            }
        }

        if (form.TryGetValue("headers", out var headersString) && !string.IsNullOrWhiteSpace(headersString))
        {
            try {
                request.Headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(headersString!) ?? new();
            } catch { }
        }

        foreach (var key in form.Keys)
        {
            if (key != "url" && key != "method" && key != "verifySsl" && key != "headers" && key != "bodyType")
            {
                request.FormFields.Add(new KeyValuePair<string, string>(key, form[key].ToString()));
            }
        }

        (int status, object data) = await _proxyService.ForwardRequestAsync(request, HttpContext.RequestAborted);
        return StatusCode(status, data);
    }
}
