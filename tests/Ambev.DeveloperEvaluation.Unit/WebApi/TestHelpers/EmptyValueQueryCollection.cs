using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Ambev.DeveloperEvaluation.Unit.WebApi.TestHelpers;

/// <summary>
/// Coleção de query mínima usada para cobrir o branch defensivo dos controllers em que
/// <c>StringValues.Count == 0</c> (pula a chave). <see cref="IQueryCollection"/> nativo
/// não permite criar pares com valores vazios diretamente.
/// </summary>
internal sealed class EmptyValueQueryCollection : IQueryCollection
{
    private readonly Dictionary<string, StringValues> _data;

    public EmptyValueQueryCollection(IDictionary<string, StringValues> data)
    {
        _data = new Dictionary<string, StringValues>(data, StringComparer.OrdinalIgnoreCase);
    }

    public StringValues this[string key] => _data.TryGetValue(key, out var v) ? v : StringValues.Empty;

    public int Count => _data.Count;

    public ICollection<string> Keys => _data.Keys;

    public bool ContainsKey(string key) => _data.ContainsKey(key);

    public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() => _data.GetEnumerator();

    public bool TryGetValue(string key, out StringValues value) => _data.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
}

