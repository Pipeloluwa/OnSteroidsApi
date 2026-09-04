namespace OnSteroidsApi.Domain.Models.Common.ExceptionModels
{
    /// <summary>
    /// Exception thrown when the proxy request fails due to connectivity or target API issues.
    /// </summary>
    public class ProxyRequestException(string message, Exception? innerException = null)
        : Exception(message, innerException)
    {
        public string? PropertyName { get; set; }
    }
}
