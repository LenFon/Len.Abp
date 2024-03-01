namespace Len.Abp.Wpf;

public interface IParameters
{
    static IParameters Create() => new Parameters();

    bool ContainsKey(string key);

    int Count { get; }

    IEnumerable<string> Keys { get; }

    void Add<T>(string key, T value);

    object? GetValue(string key);

    T? GetValue<T>(string key);

    IEnumerable<object?> GetValues(string key);

    IEnumerable<T?> GetValues<T>(string key);
}