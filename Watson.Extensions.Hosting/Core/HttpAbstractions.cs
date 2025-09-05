using System.Text.Json;
using WatsonWebserver.Core;

namespace Watson.Extensions.Hosting.Core
{
    /// <summary>
    /// Represents the execution context for a single request within the Watson Webserver.
    /// </summary>
    public class WatsonHttpContext
    {
        public IServiceProvider Services { get; }
        public HttpContextBase HttpContextBase { get; }
        public IDictionary<string, object> RouteParameters { get; }

        public WatsonHttpContext(IServiceProvider services, HttpContextBase httpContextBase, IDictionary<string, object> routeParameters)
        {
            Services = services;
            HttpContextBase = httpContextBase;
            RouteParameters = routeParameters;
        }
    }

    /// <summary>
    /// Defines a middleware component for the HTTP request pipeline.
    /// </summary>
    public interface IMiddleware
    {
        /// <summary>
        /// Executes the middleware.
        /// </summary>
        /// <param name="context">The <see cref="WatsonHttpContext"/> for the current request.</param>
        /// <param name="next">The next middleware in the pipeline.</param>
        Task InvokeAsync(WatsonHttpContext context, Func<HttpContextBase, Task> next);
    }

    /// <summary>
    /// Defines a contract for a result of an action method.
    /// </summary>
    public interface IActionResult
    {
        Task ExecuteAsync(HttpContextBase context);
    }

    /// <summary>
    /// Provides static methods for creating <see cref="IActionResult"/> objects.
    /// </summary>
    public static class Results
    {
        public static IActionResult Json(object? data, int statusCode = 200) => new JsonResult(data, statusCode);
        public static IActionResult Ok(object? data = null) => new JsonResult(data, 200);
        public static IActionResult NotFound() => new StatusCodeResult(404);
        public static IActionResult BadRequest(object? error = null) => new JsonResult(error, 400);
        public static IActionResult InternalServerError(object? error = null) => new JsonResult(error, 500);
    }

    internal class JsonResult : IActionResult
    {
        private readonly object? _data;
        private readonly int _statusCode;

        public JsonResult(object? data, int statusCode)
        {
            _data = data;
            _statusCode = statusCode;
        }

        public async Task ExecuteAsync(HttpContextBase context)
        {
            context.Response.StatusCode = _statusCode;
            if (_data != null)
            {
                context.Response.ContentType = "application/json";
                await context.Response.Send(JsonSerializer.Serialize(_data));
            }
            else
            {
                await context.Response.Send();
            }
        }
    }

    internal class StatusCodeResult : IActionResult
    {
        private readonly int _statusCode;
        public StatusCodeResult(int statusCode) => _statusCode = statusCode;

        public async Task ExecuteAsync(HttpContextBase context)
        {
            context.Response.StatusCode = _statusCode;
            await context.Response.Send();
        }
    }
}
