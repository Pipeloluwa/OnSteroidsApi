namespace OnSteroidsApi.Domain.Models.Common.BaseModels.Responses
{

    /// <summary>
    /// This is the parent base response for every successful response returned
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="title"></param>
    /// <param name="responseCode"></param>
    /// <param name="message"></param>
    /// <param name="data"></param>
    public class BaseSuccessResponse<T>(string title, string responseCode, string message, T? data)
    {
        public string? title { get; set; } = title;
        public string? responseCode { get; set; } = responseCode;
        public string? message { get; set; } = message;
        public T? data { get; set; } = data;
    }


    /// <summary>
    /// This is the parent base response for every unsuccessful response returned
    /// </summary>
    /// <param name="title"></param>
    /// <param name="responseCode"></param>
    /// <param name="message"></param>
    /// <param name="errors"></param>
    public class BaseErrorResponse(string title, string responseCode, string message, IEnumerable<string>? errors)
    {
        public string? title { get; set; } = title;
        public string? responseCode { get; set; } = responseCode;
        public string? message { get; set; } = message;
        public IEnumerable<string>? errors { get; set; } = errors;
    }

}
