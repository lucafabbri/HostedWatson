using System.Collections.Specialized;

namespace Watson.Extensions.Hosting.Commons;

public static class NameValueCollectionExtensions
{
    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="collection">The NameValueCollection.</param>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key,
    /// if the key is found; otherwise, null. The value is a comma-separated list if the key has multiple values.</param>
    /// <returns>true if the collection contains an element with the specified key; otherwise, false.</returns>
    public static bool TryGetValue(this NameValueCollection collection, string key, out string value)
    {
        value = collection.Get(key);
        return value != null;
    }
}
