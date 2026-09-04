using OnSteroidsApi.Application.MiddlewaresAndFilters.Middlewares;

namespace OnSteroidsApi.WebApi.Configuration
{
    public static class MiddlewareExtension
    {
        /// <summary>
        /// Registers the global exception-handling middleware into the request pipeline.
        /// </summary>
        public static void UseMiddlewareExtensions(this WebApplication app)
        {
            app.UseMiddleware<Middleware>();
        }
    }
}
