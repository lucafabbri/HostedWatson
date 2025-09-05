using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Watson.Extensions.Hosting.Commons;
using Watson.Extensions.Hosting.Controllers;
using Watson.Extensions.Hosting.Core;
using Watson.Extensions.Hosting.Routing;
using WatsonWebserver.Core;
using HttpMethod = WatsonWebserver.Core.HttpMethod;

namespace Watson.Extensions.Hosting
{
    /// <summary>
    /// Configuration options for Watson Webserver.
    /// </summary>
    public class WatsonWebserverOptions
    {
        public string Hostname { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 8080;
        public bool Ssl { get; set; } = false;

        internal ConcurrentDictionary<(HttpMethod Method, string Template), RouteEntry> Routes { get; } = new();
        internal List<Type> MiddlewareTypes { get; } = new();
        internal HashSet<Type> ControllerTypes { get; } = new();

        public void UseMiddleware<T>() where T : IMiddleware => MiddlewareTypes.Add(typeof(T));

        public void MapGet(string template, Delegate handler) => MapVerb(HttpMethod.GET, template, handler);
        public void MapPost(string template, Delegate handler) => MapVerb(HttpMethod.POST, template, handler);
        public void MapPut(string template, Delegate handler) => MapVerb(HttpMethod.PUT, template, handler);
        public void MapDelete(string template, Delegate handler) => MapVerb(HttpMethod.DELETE, template, handler);
        public void MapPatch(string template, Delegate handler) => MapVerb(HttpMethod.PATCH, template, handler);

        public void MapControllers(params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                assemblies = [Assembly.GetCallingAssembly()];
            }

            var discoveredControllerTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !t.IsAbstract);

            foreach (var controllerType in discoveredControllerTypes)
            {
                // Register the controller type for DI registration later.
                ControllerTypes.Add(controllerType);

                var routeAttr = controllerType.GetCustomAttribute<RouteAttribute>();
                var baseRoute = routeAttr?.Template ?? controllerType.Name.Replace("Controller", "").ToLower();
                if (!baseRoute.StartsWith("/")) baseRoute = "/" + baseRoute;

                var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<HttpMethodAttribute>() != null);

                foreach (var method in methods)
                {
                    var methodAttr = method.GetCustomAttribute<HttpMethodAttribute>()!;
                    var fullRoute = (baseRoute + "/" + methodAttr.Template).Replace("//", "/").TrimEnd('/');
                    if (string.IsNullOrEmpty(fullRoute)) fullRoute = "/";

                    Routes.TryAdd((methodAttr.HttpMethod, fullRoute), new RouteEntry
                    {
                        Handler = CreateControllerActionHandler(controllerType, method)
                    });
                }
            }
        }

        private void MapVerb(HttpMethod verb, string template, Delegate handler)
        {
            var entry = new RouteEntry
            {
                Handler = async (context) =>
                {
                    var parameters = await BindParameters(handler.Method, context);
                    var result = handler.DynamicInvoke(parameters);
                    await HandleResult(result, context.HttpContextBase);
                }
            };
            Routes.TryAdd((verb, template), entry);
        }

        private Func<WatsonHttpContext, Task> CreateControllerActionHandler(Type controllerType, MethodInfo method)
        {
            return async (context) =>
            {
                var controller = (ControllerBase)context.Services.GetRequiredService(controllerType);
                controller.Context = context;
                var parameters = await BindParameters(method, context);
                var result = method.Invoke(controller, parameters);
                await HandleResult(result, context.HttpContextBase);
            };
        }

        private static async Task HandleResult(object? result, HttpContextBase context)
        {
            if (result is Task task)
            {
                await task;
                result = task.GetType().GetProperty("Result")?.GetValue(task);
            }

            if (result is IActionResult actionResult)
            {
                await actionResult.ExecuteAsync(context);
            }
            else
            {
                await Results.Ok(result).ExecuteAsync(context);
            }
        }

        private async Task<object?[]> BindParameters(MethodInfo method, WatsonHttpContext context)
        {
            var parameters = method.GetParameters();
            var arguments = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                object? value = null;
                var paramName = p.Name!;

                if (p.GetCustomAttribute<FromBodyAttribute>() != null)
                {
                    var bodyBytes = context.HttpContextBase.Request.DataAsBytes;
                    if (bodyBytes?.Length > 0)
                        value = JsonSerializer.Deserialize(bodyBytes, p.ParameterType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else if (p.GetCustomAttribute<FromRouteAttribute>() != null || context.RouteParameters.ContainsKey(paramName))
                {
                    if (context.RouteParameters.TryGetValue(paramName, out var routeValue))
                        value = Convert.ChangeType(routeValue, p.ParameterType);
                }
                else if (p.GetCustomAttribute<FromQueryAttribute>() != null)
                {
                    if (context.HttpContextBase.Request.Query.Elements.TryGetValue(paramName, out var queryValue))
                        value = Convert.ChangeType(queryValue, p.ParameterType);
                }
                else if (p.GetCustomAttribute<FromHeaderAttribute>() != null)
                {
                    if (context.HttpContextBase.Request.Headers.TryGetValue(paramName, out var headerValue))
                        value = Convert.ChangeType(headerValue, p.ParameterType);
                }
                else if (p.GetCustomAttribute<FromServicesAttribute>() != null)
                {
                    value = context.Services.GetRequiredService(p.ParameterType);
                }
                else if (p.ParameterType == typeof(WatsonHttpContext)) value = context;
                else if (p.ParameterType == typeof(HttpContextBase)) value = context.HttpContextBase;

                arguments[i] = value;
            }
            return arguments;
        }
    }

    public static class WatsonWebserverServiceCollectionExtensions
    {
        public static IServiceCollection AddWatsonWebserver<TWebServer>(this IServiceCollection services, Action<WatsonWebserverOptions> configureOptions)
            where TWebServer : WebserverBase
        {
            var options = new WatsonWebserverOptions();
            configureOptions(options);
            services.AddSingleton(options);

            // Correctly register all discovered controllers.
            foreach (var controllerType in options.ControllerTypes)
            {
                services.AddScoped(controllerType);
            }

            foreach (var middlewareType in options.MiddlewareTypes)
            {
                services.AddScoped(middlewareType);
            }

            services.AddSingleton<WebserverBase>(provider =>
            {
                var serverOptions = provider.GetRequiredService<WatsonWebserverOptions>();
                var logger = provider.GetRequiredService<ILogger<TWebServer>>();
                var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

                Func<HttpContextBase, Task> defaultRoute = async (ctx) =>
                {
                    try
                    {
                        // Use the raw URL (path only) for matching.
                        var (route, values) = RouteMatcher.Match(serverOptions.Routes, ctx.Request.Method, ctx.Request.Url.RawWithQuery);
                        if (route == null)
                        {
                            await Results.NotFound().ExecuteAsync(ctx);
                            return;
                        }

                        await using var scope = scopeFactory.CreateAsyncScope();
                        var context = new WatsonHttpContext(scope.ServiceProvider, ctx, values);

                        var finalHandler = route.Handler;

                        var pipeline = serverOptions.MiddlewareTypes
                            .AsEnumerable()
                            .Reverse()
                            .Aggregate(finalHandler, (next, middlewareType) =>
                            {
                                return (Func<WatsonHttpContext, Task>)(wCtx =>
                                {
                                    var middleware = (IMiddleware)wCtx.Services.GetRequiredService(middlewareType);
                                    Func<HttpContextBase, Task> nextAdapter = _ => next(wCtx);
                                    return middleware.InvokeAsync(wCtx, nextAdapter);
                                });
                            });

                        await pipeline(context);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing request for {Path}", ctx.Request.Url.Full);
                        await Results.InternalServerError(ex.InnerException?.Message ?? ex.Message).ExecuteAsync(ctx);
                    }
                };

                var webserverSettings = new WebserverSettings(serverOptions.Hostname, serverOptions.Port, serverOptions.Ssl)
                {
                    UseMachineHostname = true // Useful for some network configurations
                };

                var server = Activator.CreateInstance(typeof(TWebServer), webserverSettings, defaultRoute) as TWebServer;

                return server!;
            });

            services.AddHostedService<WatsonWebserverHostedService>();
            return services;
        }
    }
}

