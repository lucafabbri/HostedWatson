using Watson.Extensions.Hosting.Core;
using WatsonWebserver.Core;
using HttpMethod = WatsonWebserver.Core.HttpMethod;

namespace Watson.Extensions.Hosting.Controllers
{
    /// <summary>
    /// Base class for API controllers.
    /// </summary>
    public abstract class ControllerBase
    {
        public WatsonHttpContext Context { get; internal set; } = null!;
        protected HttpContextBase HttpContextBase => Context.HttpContextBase;
    }

    public abstract class HttpMethodAttribute : Attribute
    {
        public string Template { get; }
        public abstract HttpMethod HttpMethod { get; }
        protected HttpMethodAttribute(string template) => Template = template;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class RouteAttribute(string template) : Attribute
    {
        public string Template { get; } = template;
    }

    [AttributeUsage(AttributeTargets.Method)] public class HttpGetAttribute(string template) : HttpMethodAttribute(template) { public override HttpMethod HttpMethod => HttpMethod.GET; }
    [AttributeUsage(AttributeTargets.Method)] public class HttpPostAttribute(string template) : HttpMethodAttribute(template) { public override HttpMethod HttpMethod => HttpMethod.POST; }
    [AttributeUsage(AttributeTargets.Method)] public class HttpPutAttribute(string template) : HttpMethodAttribute(template) { public override HttpMethod HttpMethod => HttpMethod.PUT; }
    [AttributeUsage(AttributeTargets.Method)] public class HttpDeleteAttribute(string template) : HttpMethodAttribute(template) { public override HttpMethod HttpMethod => HttpMethod.DELETE; }
    [AttributeUsage(AttributeTargets.Method)] public class HttpPatchAttribute(string template) : HttpMethodAttribute(template) { public override HttpMethod HttpMethod => HttpMethod.PATCH; }

    [AttributeUsage(AttributeTargets.Parameter)] public class FromRouteAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter)] public class FromBodyAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter)] public class FromServicesAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter)] public class FromQueryAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Parameter)] public class FromHeaderAttribute : Attribute { }
}

