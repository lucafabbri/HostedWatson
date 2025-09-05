using System.Collections.Concurrent;
using Watson.Extensions.Hosting.Core;

namespace Watson.Extensions.Hosting.Routing
{
    internal class RouteEntry
    {
        public Func<WatsonHttpContext, Task> Handler { get; set; } = null!;
    }

    internal static class RouteMatcher
    {
        public static (RouteEntry? entry, Dictionary<string, object> values) Match(
            ConcurrentDictionary<(WatsonWebserver.Core.HttpMethod Method, string Template), RouteEntry> routes,
            WatsonWebserver.Core.HttpMethod requestMethod,
            string requestPath)
        {
            // Sanitize the request path to remove any query string.
            var pathOnly = requestPath.Split('?')[0];

            foreach (var route in routes)
            {
                if (route.Key.Method != requestMethod)
                    continue;

                var templateParts = route.Key.Template.Split('/');
                var pathParts = pathOnly.Split('/');

                if (templateParts.Length != pathParts.Length)
                    continue;

                var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                var isMatch = true;

                for (var i = 0; i < templateParts.Length; i++)
                {
                    if (templateParts[i].StartsWith("{") && templateParts[i].EndsWith("}"))
                    {
                        var paramName = templateParts[i].Trim('{', '}');
                        values[paramName] = pathParts[i];
                    }
                    else if (!string.Equals(templateParts[i], pathParts[i], StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    return (route.Value, values);
                }
            }
            return (null, new Dictionary<string, object>());
        }
    }
}

