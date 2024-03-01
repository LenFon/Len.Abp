namespace Len.Abp.Wpf;

internal class Parameters : IParameters
{
    private readonly Dictionary<Type, IInternalParameters> _chched = [];

    public int Count => _chched.Select(s => s.Value).Sum(s => s.Count);

    public IEnumerable<string> Keys => _chched.Select(s => s.Value).SelectMany(s => s.Keys);

    public void Add<T>(string key, T value)
    {
        GetInternalParameters<T>().Add(key, value);
    }

    public bool ContainsKey(string key) => _chched.Select(s => s.Value).Any(w => w.ContainsKey(key));

    public T? GetValue<T>(string key) => GetInternalParameters<T>().GetValue(key);

    public IEnumerable<T?> GetValues<T>(string key) => GetInternalParameters<T>().GetValues(key);

    private InternalParameters<T> GetInternalParameters<T>()
    {
        var type = typeof(T);
        if (!_chched.TryGetValue(type, out var p) || p is not InternalParameters<T> parameters)
        {
            parameters = new InternalParameters<T>();
            _chched[type] = parameters;
        }

        return parameters;
    }

    public object? GetValue(string key)
    {
        foreach (var parameter in _chched.Values)
        {
            var val = parameter.GetValue(key);
            if (val is not null)
            {
                return val;
            }
        }

        return default;
    }

    public IEnumerable<object?> GetValues(string key) => _chched.Values.SelectMany(s => s.GetValues(key));

    internal class InternalParameters<T> : IInternalParameters
    {
        private readonly List<KeyValuePair<string, T>> _entries = [];

        public int Count => _entries.Count;

        public IEnumerable<string> Keys => _entries.Select(x => x.Key);

        public void Add(string key, T value) => _entries.Add(new(key, value));

        public bool ContainsKey(string key) =>
            _entries.Any(x => string.Compare(x.Key, key, StringComparison.Ordinal) == 0);

        public T? GetValue(string key)
        {
            foreach (var item in _entries)
            {
                if (string.Compare(item.Key, key, StringComparison.Ordinal) == 0)
                {
                    return item.Value;
                }
            }

            return default;
        }

        public IEnumerable<T?> GetValues(string key) =>
            _entries.Where(w => string.Compare(w.Key, key, StringComparison.Ordinal) == 0).Select(s => s.Value);

        object? IInternalParameters.GetValue(string key) => GetValue(key);

        IEnumerable<object?> IInternalParameters.GetValues(string key) => GetValues(key).Select(s => (object?)s);
    }

    internal interface IInternalParameters
    {
        bool ContainsKey(string key);

        int Count { get; }

        IEnumerable<string> Keys { get; }

        object? GetValue(string key);

        IEnumerable<object?> GetValues(string key);
    }
}
