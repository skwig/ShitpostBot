using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ShitpostBot.Backprocessor;

public class JsonBackprocessorStateStore(IOptions<BackprocessorOptions> options)
    : IBackprocessorStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public async Task<BackprocessorState> LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = options.Value.StateFilePath;
        if (!File.Exists(path))
        {
            return new BackprocessorState();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<BackprocessorState>(
                stream,
                JsonOptions,
                cancellationToken
            ) ?? new BackprocessorState();
    }

    public async Task SaveAsync(
        BackprocessorState state,
        CancellationToken cancellationToken = default
    )
    {
        var path = options.Value.StateFilePath;
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
        }

        File.Move(tempPath, path, overwrite: true);
    }
}
