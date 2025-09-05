using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Watson.Extensions.Hosting;
using Watson.Extensions.Hosting.Controllers;
using Watson.Extensions.Hosting.Core;
using WatsonWebserver.Lite;
using Watson.Extensions.Hosting.Samples.Default.Services;
using Watson.Extensions.Hosting.Samples.Default.Models;

namespace Watson.Extensions.Hosting.Samples.Default;

public class Program
{
    public static async Task Main(string[] args)
    {
        // 1. Configura l'Host e i servizi
        var builder = Host.CreateDefaultBuilder(args);

        builder.ConfigureServices((hostContext, services) =>
        {
            // Aggiungiamo il nostro servizio per il contatore come Singleton.
            // Sarà la stessa istanza per tutte le richieste.
            services.AddSingleton<CounterService>();

            // Aggiungiamo il nostro framework Watson!
            // Usiamo WebserverLite come implementazione concreta.
            services.AddWatsonWebserver<WebserverLite>(options =>
            {
                // options.Hostname = "0.0.0.0"; // Ascolta su tutte le interfacce di rete
                options.Port = 8080;

                // Mappiamo tutti i controller presenti nell'assembly corrente.
                // Il nostro CounterController verrà scoperto e registrato automaticamente.
                options.MapControllers();

                // Mappiamo un endpoint in stile Minimal API per ricevere i log.
                options.MapPost("/logs", (
                    [FromBody] LogEntry log,
                    [FromServices] ILogger<Program> logger) =>
                {
                    logger.LogInformation(
                        "Nuovo log ricevuto da {Source} [{Level}]: {Message}",
                        log.SourceSystem,
                        log.Level,
                        log.Message);

                    // Restituiamo un risultato per confermare la ricezione.
                    return Results.Ok(new { Status = "Log received successfully" });
                });
            });
        });

        var app = builder.Build();

        // 2. Avviamo l'applicazione
        Console.WriteLine("Server avviato. In ascolto sulla porta 8080.");
        Console.WriteLine("Premi Ctrl+C per terminare.");
        Console.WriteLine();
        Console.WriteLine("Endpoints disponibili:");
        Console.WriteLine("  GET  /api/counter         -> Legge il valore del contatore");
        Console.WriteLine("  POST /api/counter/increment -> Incrementa il contatore");
        Console.WriteLine("  POST /logs                 -> Invia un log (vedi esempio JSON sotto)");
        Console.WriteLine();
        Console.WriteLine("Esempio di payload per POST /logs:");
        Console.WriteLine(@"{ ""Timestamp"":""2025-09-05T10:18:00Z"", ""Level"":""Info"", ""Message"":""Test message"", ""SourceSystem"":""ExternalApp"" }");


        await app.RunAsync();
    }
}
