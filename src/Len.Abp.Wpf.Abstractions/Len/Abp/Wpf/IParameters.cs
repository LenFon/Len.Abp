using System.Collections;

namespace Len.Abp.Wpf;

public interface IParameters : IEnumerable<KeyValuePair<string, object>>
{
    int Count { get; }

    IEnumerable<string> Keys { get; }

    object? this[string key] { get; }

    static IParameters Create(string? query = null) => new Parameters(query);

    IParameters Add(string key, object value);

    bool ContainsKey(string key);

    T? GetValue<T>(string key);

    IEnumerable<T?> GetValues<T>(string key);

    bool TryGetValue<T>(string key, out T? value);
}

file class Parameters : IParameters
{
    private readonly List<KeyValuePair<string, object>> _entries = [];

    public Parameters(string? query = null)
    {
        if (!string.IsNullOrWhiteSpace(query))
        {
            int num = query.Length;
            for (int i = ((query.Length > 0) && (query[0] == '?')) ? 1 : 0; i < num; i++)
            {
                int startIndex = i;
                int num4 = -1;
                while (i < num)
                {
                    char ch = query[i];
                    if (ch == '=')
                    {
                        if (num4 < 0)
                        {
                            num4 = i;
                        }
                    }
                    else if (ch == '&')
                    {
                        break;
                    }
                    i++;
                }
                string? key = null;
                string value;
                if (num4 >= 0)
                {
                    key = query[startIndex..num4];
                    value = query.Substring(num4 + 1, (i - num4) - 1);
                }
                else
                {
                    value = query[startIndex..i];
                }

                if (key != null)
                    Add(Uri.UnescapeDataString(key), Uri.UnescapeDataString(value));
            }
        }
    }

    public int Count => _entries.Count;

    public IEnumerable<string> Keys => _entries.Select(s => s.Key);

    public object? this[string key] => _entries.FirstOrDefault(w => string.Compare(w.Key, key, StringComparison.Ordinal) == 0).Value;

    public IParameters Add(string key, object value)
    {
        _entries.Add(new(key, value));

        return this;
    }

    public bool ContainsKey(string key) => _entries.Any(w => string.Compare(w.Key, key, StringComparison.Ordinal) == 0);

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();

    public T? GetValue<T>(string key) => (T?)GetValue(key, typeof(T));

    public IEnumerable<T?> GetValues<T>(string key)
    {
        var values = new List<T?>();
        var type = typeof(T);

        foreach (var kvp in _entries)
        {
            if (string.Compare(kvp.Key, key, StringComparison.Ordinal) == 0)
            {
                TryGetValueInternal(kvp, type, out var value);
                values.Add((T?)value);
            }
        }

        return values.AsEnumerable();
    }

    public bool TryGetValue<T>(string key, out T? value)
    {
        foreach (var kvp in _entries)
        {
            if (string.Compare(kvp.Key, key, StringComparison.Ordinal) == 0)
            {
                var success = TryGetValueInternal(kvp, typeof(T), out object? valueAsObject);
                value = (T?)valueAsObject;
                return success;
            }
        }

        value = default;
        return false;
    }

    private static object? GetDefault(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

    private static bool TryGetValueInternal(KeyValuePair<string, object> kvp, Type type, out object? value)
    {
        value = GetDefault(type);
        var success = false;
        if (kvp.Value == null)
        {
            success = true;
        }
        else if (kvp.Value.GetType() == type)
        {
            success = true;
            value = kvp.Value;
        }
        else if (type.IsAssignableFrom(kvp.Value.GetType()))
        {
            success = true;
            value = kvp.Value;
        }
        else if (type.IsEnum)
        {
            var valueAsString = kvp.Value.ToString()!;
            if (Enum.IsDefined(type, valueAsString))
            {
                success = true;
                value = Enum.Parse(type, valueAsString);
            }
            else if (int.TryParse(valueAsString, out var numericValue))
            {
                success = true;
                value = Enum.ToObject(type, numericValue);
            }
        }

        if (!success && type.GetInterface("System.IConvertible") != null)
        {
            success = true;
            value = Convert.ChangeType(kvp.Value, type);
        }

        return success;
    }

    private object? GetValue(string key, Type type)
    {
        foreach (var kvp in _entries)
        {
            if (string.Compare(kvp.Key, key, StringComparison.Ordinal) == 0)
            {
                if (TryGetValueInternal(kvp, type, out var value))
                    return value;

                throw new InvalidCastException(
                    $"Unable to convert the value of Type '{kvp.Value.GetType().FullName}' to '{type.FullName}' for the key '{key}' "
                );
            }
        }

        return GetDefault(type);
    }
}