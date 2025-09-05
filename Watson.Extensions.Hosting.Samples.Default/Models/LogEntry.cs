namespace Watson.Extensions.Hosting.Samples.Default.Models;

/// <summary>
/// Modello di dati per un log proveniente da un sistema esterno.
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = "Unknown";
}
