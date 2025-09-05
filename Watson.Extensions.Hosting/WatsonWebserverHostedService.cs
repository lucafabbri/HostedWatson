using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WatsonWebserver.Core;

namespace Watson.Extensions.Hosting
{
    /// <summary>
    /// IHostedService to manage the lifecycle of the WatsonWebserver.
    /// </summary>
    internal class WatsonWebserverHostedService : IHostedService
    {
        private readonly WebserverBase _server;
        private readonly ILogger<WatsonWebserverHostedService> _logger;

        public WatsonWebserverHostedService(WebserverBase server, ILogger<WatsonWebserverHostedService> logger)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting WatsonWebserver...");
            _server.Start();
            _logger.LogInformation("WatsonWebserver started and listening on {Endpoint}", _server.Settings.Prefix);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping WatsonWebserver...");
            _server.Dispose();
            _logger.LogInformation("WatsonWebserver stopped.");
            return Task.CompletedTask;
        }
    }
}
