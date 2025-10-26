using System.Text.Json;
using StackNetAdvisor.Core.Contracts;

namespace StackNetAdvisor.Infrastructure.Caching;

public class JsonFileCacheProvider : ICacheProvider
{
    private readonly string _cacheDir;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public JsonFileCacheProvider(string? cacheDir = null)
    {
        _cacheDir = cacheDir ?? Path.Combine(AppContext.BaseDirectory, "cache");
        Directory.CreateDirectory(_cacheDir);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var path = Path.Combine(_cacheDir, ToFileName(key));
        if (!File.Exists(path)) return default;
        await using var fs = File.OpenRead(path);
        var wrapper = await JsonSerializer.DeserializeAsync<Wrapper<T>>(fs, cancellationToken: ct);
        if (wrapper is null) return default;
        if (DateTimeOffset.UtcNow > wrapper.ExpiresAt) return default;
        return wrapper.Value;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var path = Path.Combine(_cacheDir, ToFileName(key));
        var wrapper = new Wrapper<T> { Value = value, ExpiresAt = DateTimeOffset.UtcNow.Add(ttl) };
        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, wrapper, _options, ct);
    }

    private static string ToFileName(string key)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            key = key.Replace(c, '_');
        return key + ".json";
    }

    private class Wrapper<T>
    {
        public T? Value { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }
}