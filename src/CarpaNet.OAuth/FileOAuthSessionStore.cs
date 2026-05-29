using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CarpaNet.OAuth.Storage;

public sealed class FileOAuthSessionStore : IOAuthSessionStore
{
    private readonly string _directory;

    public FileOAuthSessionStore(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
    }

    public async Task StoreAsync(string sub, OAuthSessionData data, CancellationToken cancellationToken = default)
    {
        var path = GetPath(sub);
        var json = JsonSerializer.Serialize(data, OAuthJsonContext.Default.OAuthSessionData);
        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(json).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    public async Task<OAuthSessionData?> GetAsync(string sub, CancellationToken cancellationToken = default)
    {
        var path = GetPath(sub);
        if (!File.Exists(path))
            return null;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize(json, OAuthJsonContext.Default.OAuthSessionData);
    }

    public Task DeleteAsync(string sub, CancellationToken cancellationToken = default)
    {
        var path = GetPath(sub);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string GetPath(string sub)
    {
        var sanitized = sub.Replace(':', '_').Replace('#', '_').Replace('/', '_');
        return Path.Combine(_directory, $"oauth-{sanitized}.json");
    }
}
