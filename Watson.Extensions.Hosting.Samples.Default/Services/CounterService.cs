namespace Watson.Extensions.Hosting.Samples.Default.Services;

/// <summary>
/// Servizio Singleton per gestire lo stato di un contatore in modo thread-safe.
/// </summary>
public class CounterService
{
    private int _value = 0;

    /// <summary>
    /// Ottiene il valore corrente del contatore.
    /// </summary>
    public int GetValue() => _value;

    /// <summary>
    /// Incrementa il contatore di uno e restituisce il nuovo valore.
    /// </summary>
    public int Increment()
    {
        // Usiamo Interlocked per garantire l'atomicità dell'operazione
        // in un ambiente multi-threaded come un server web.
        return Interlocked.Increment(ref _value);
    }

    /// <summary>
    /// Resetta il contatore a zero.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _value, 0);
    }
}
