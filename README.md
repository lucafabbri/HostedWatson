# Watson Extensions for Modern .NET Hosting

[![NuGet Version](https://img.shields.io/nuget/v/Watson.Extensions.Hosting.svg)](https://www.nuget.org/packages/Watson.Extensions.Hosting/)
[![Build Status](https://img.shields.io/github/actions/workflow/status/your-username/your-repo/dotnet.yml?branch=main)](https://github.com/your-username/your-repo/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

`Watson.Extensions.Hosting` brings the power and flexibility of `Microsoft.Extensions.Hosting` (the .NET Generic Host) to the lightweight and high-performance [WatsonWebserver](https://github.com/dotnet/WatsonWebserver). This library empowers you to build web servers using modern, familiar patterns like Dependency Injection, Middleware, MVC-style Controllers, and Minimal APIs, just as you would with ASP.NET Core.

It's the ideal solution for creating lightweight microservices, internal APIs, or self-hosted applications without the overhead of a full-fledged framework, but with all the benefits of the .NET hosting ecosystem.

## 🤔 Why Watson Extensions for Hosting?

While [WatsonWebserver](https://github.com/dotnet/WatsonWebserver) is an excellent standalone server, integrating it into a modern .NET application often requires boilerplate code to manage its lifecycle, configuration, and dependencies. This library bridges that gap by providing a seamless, out-of-the-box integration with the .NET Generic Host, allowing you to focus on your application's logic instead of its infrastructure.

## ✨ Core Features

* **Generic Host Integration**: Manage your Watson server's lifecycle (`StartAsync`, `StopAsync`) automatically within the .NET `IHost`.
* **Full Dependency Injection (DI) Support**: Leverage the built-in DI container to manage your services with configurable lifetimes (`Singleton`, `Scoped`, `Transient`).
* **ASP.NET Core Style Routing**:
    * **Controllers**: Define your APIs in clean, organized classes using familiar attributes like `[Route]`, `[HttpGet]`, and `[HttpPost]`.
    * **Minimal APIs**: Quickly map endpoints to simple delegate handlers using `MapGet`, `MapPost`, etc., for small-scale APIs or prototyping.
* **Advanced Model Binding**: Automatically populate your action method parameters from the request's route, query string, headers, and body.
* **Extensible Middleware Pipeline**: Inject custom logic into the request processing pipeline for tasks like logging, authentication, or error handling.
* **Flexible Implementation**: Works with both the full `WatsonWebserver` and the dependency-free `WatsonWebserver.Lite`.

---

## 🚀 Getting Started

Let's build your first application in just a few minutes.

### 1. Prerequisites

* [.NET 6 SDK or later](https://dotnet.microsoft.com/download)

### 2. Installation

First, install our main package via the .NET CLI.

```shell
dotnet add package Watson.Extensions.Hosting
```

Next, you need to choose which `WatsonWebserver` implementation to use. Both are part of the excellent [WatsonWebserver project](https://github.com/dotnet/WatsonWebserver). `WatsonWebserver.Lite` is a great choice for simple, dependency-free projects.

```shell
dotnet add package WatsonWebserver.Lite
# OR for the full version
dotnet add package WatsonWebserver
```

### 3. Configure Your Server

Now, let's set up a simple Console Application. Replace the content of your `Program.cs` with the following code. We'll define a controller and a minimal API endpoint.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Watson.Extensions.Hosting;
using Watson.Extensions.Hosting.Controllers;
using Watson.Extensions.Hosting.Core;
using WatsonWebserver.Lite; // Using the 'Lite' version for this example

// --- 1. Define a Controller ---
// This is a class-based approach to organizing your endpoints.
[Route("api/greeting")]
public class GreetingController : ControllerBase
{
    // This action will handle GET requests to /api/greeting/{name}
    [HttpGet("{name}")]
    public IActionResult SayHello([FromRoute] string name)
    {
        // The Results helper creates standard HTTP responses.
        // Here, we return a 200 OK with a JSON payload.
        return Results.Ok(new { message = $"Hello, {name}!" });
    }
}

// --- 2. Configure the .NET Host ---
var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((_, services) =>
{
    // This is where you add the Watson Webserver to the service collection.
    services.AddWatsonWebserver<WebserverLite>(options =>
    {
        options.Port = 8080;
        options.Hostname = "localhost";

        // This method scans the current assembly for any class inheriting
        // from ControllerBase and automatically registers its routes.
        options.MapControllers();

        // You can also define simple endpoints directly, Minimal API style.
        options.MapGet("/", () => Results.Ok("Welcome to Watson.Extensions.Hosting!"));
    });
});

// --- 3. Build and Run the Application ---
var app = builder.Build();

Console.WriteLine("Server is listening on http://localhost:8080");
Console.WriteLine("Press CTRL+C to exit.");
await app.RunAsync();
```

### 4. Run and Test

Execute your application from the terminal:

```shell
dotnet run
```

You can now test your endpoints using a web browser or a tool like cURL or Postman:

* **`GET http://localhost:8080/`**
    * Will respond with the plain text: `"Welcome to Watson.Extensions.Hosting!"`
* **`GET http://localhost:8080/api/greeting/World`**
    * Will respond with the JSON: `{"message":"Hello, World!"}`

---

## 📚 In-Depth Guide

### Dependency Injection

Register your custom services in the DI container and inject them anywhere you need—in your controllers, middleware, or Minimal API handlers.

**1. Define and Register a Service**
Let's create a simple service to manage some state.

```csharp
// MyAwesomeService.cs
public class MyAwesomeService
{
    private int _requestCount = 0;
    public int GetRequestCount() => Interlocked.Increment(ref _requestCount);
}
```

Now, register it as a singleton in `Program.cs`:
```csharp
// In the builder.ConfigureServices block:
services.AddSingleton<MyAwesomeService>();
```

**2. Inject the Service into a Controller**
The service is automatically provided to the controller's constructor.

```csharp
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly MyAwesomeService _service;

    // The DI container injects an instance of MyAwesomeService here.
    public StatsController(MyAwesomeService service)
    {
        _service = service;
    }

    [HttpGet("requests")]
    public IActionResult GetRequestCount()
    {
        var count = _service.GetRequestCount();
        return Results.Ok(new { totalRequests = count });
    }
}
```

**3. Inject the Service into a Minimal API Handler**
Just add the service as a parameter to your handler delegate and mark it with `[FromServices]`.

```csharp
// In services.AddWatsonWebserver:
options.MapGet("/stats", ([FromServices] MyAwesomeService service) =>
{
    var count = service.GetRequestCount();
    return Results.Ok(new { totalRequests = count });
});
```

### Model Binding

This library automatically maps data from an incoming HTTP request to the parameters of your action methods.

Here is a comprehensive example showing all available sources:

```csharp
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpPost("{id}")]
    public IActionResult CreateProduct(
        [FromRoute] int id,                     // From the URL path: /api/products/123
        [FromQuery] string? category,          // From the query string: ?category=electronics
        [FromHeader("X-Api-Key")] string apiKey, // From a request header
        [FromBody] ProductDto product,          // Deserialized from the JSON request body
        [FromServices] ILogger<ProductsController> logger) // Injected from DI
    {
        logger.LogInformation("Creating product {ProductId} in category {Category} with API key {ApiKey}", id, category, apiKey);
        // ... process product data ...
        return Results.Ok(new { status = "Product created", id = id, name = product.Name });
    }
}

public class ProductDto
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

### Middleware

Build a custom request processing pipeline to handle cross-cutting concerns. Middleware is executed in the order it is registered.

**1. Create a Middleware Class**
A middleware is any class that implements the `IMiddleware` interface.

```csharp
public class RequestLoggingMiddleware : IMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    // This method is the core of the middleware.
    public async Task InvokeAsync(WatsonHttpContext context, Func<HttpContextBase, Task> next)
    {
        var request = context.HttpContextBase.Request;
        _logger.LogInformation("Request received: {Method} {Path}", request.Method, request.Url.Raw);

        // Call the 'next' delegate to pass control to the next middleware in the pipeline.
        // If this is the last middleware, it calls the final endpoint handler.
        await next(context.HttpContextBase);
    }
}
```

**2. Register the Middleware**
Add your middleware to the pipeline within the server configuration. The order matters!

```csharp
services.AddWatsonWebserver<WebserverLite>(options =>
{
    // ... other options
    
    // Requests will pass through RequestLoggingMiddleware first.
    options.UseMiddleware<RequestLoggingMiddleware>();
});
```

---

## 🤝 How to Contribute

We welcome contributions from the community! Whether it's a bug fix, a new feature, or a documentation improvement, your help is greatly appreciated.

1.  **Report Bugs**: If you find an issue, please [open an issue](https://github.com/your-username/your-repo/issues) and provide detailed steps to reproduce it.
2.  **Suggest Features**: Have a great idea? [Open an issue](https://github.com/your-username/your-repo/issues) to start a discussion.
3.  **Submit Pull Requests**: Feel free to fork the repository, create a new branch for your changes, and open a Pull Request when you're ready.

## ❤️ Support the Project

The easiest way to show your support is by **starring the repository** on GitHub! ⭐

This helps raise the project's visibility and motivates us to keep improving it.

## 🙏 Acknowledgements

This project is built upon and extends the fantastic work of the **[WatsonWebserver](https://github.com/dotnet/WatsonWebserver)** library. A big thank you to its author and contributors for creating such a reliable and performant foundation.

## 📜 License

This project is licensed under the [MIT License](https://opensource.org/licenses/MIT).

