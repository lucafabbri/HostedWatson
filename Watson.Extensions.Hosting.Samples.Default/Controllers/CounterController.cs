using Watson.Extensions.Hosting.Controllers;
using Watson.Extensions.Hosting.Core;
using Watson.Extensions.Hosting.Samples.Default.Services;

namespace Watson.Extensions.Hosting.Samples.Default.Controllers;

[Route("api/counter")]
public class CounterController : ControllerBase
{
    private readonly CounterService _counterService;

    // Il CounterService viene iniettato tramite DI.
    // Poiché è registrato come Singleton, sarà sempre la stessa istanza.
    public CounterController(CounterService counterService)
    {
        _counterService = counterService;
    }

    [HttpGet("")]
    public IActionResult GetCurrentValue()
    {
        var value = _counterService.GetValue();
        return Results.Ok(new { count = value });
    }

    [HttpPost("increment")]
    public IActionResult IncrementValue()
    {
        var newValue = _counterService.Increment();
        return Results.Ok(new { newCount = newValue });
    }

    [HttpPost("reset")]
    public IActionResult ResetValue()
    {
        _counterService.Reset();
        return Results.Ok(new { message = "Counter reset to 0." });
    }
}
