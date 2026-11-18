using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using PitWallConnector.Models;

namespace PitWallConnector.Services;

public sealed class SnapshotPublisher : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly Channel<ConnectorSnapshot> _channel = Channel.CreateBounded<ConnectorSnapshot>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true
        });

    private readonly HttpClient _httpClient = new();
    private readonly string? _apiUrl;
    private readonly string? _apiToken;
    private readonly string _outputPath;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;

    public SnapshotPublisher()
    {
        _apiUrl = Environment.GetEnvironmentVariable("PITWALL_API_URL");
        _apiToken = Environment.GetEnvironmentVariable("PITWALL_API_TOKEN");
        _outputPath = Environment.GetEnvironmentVariable("PITWALL_SNAPSHOT_PATH")
            ?? Path.Combine(AppContext.BaseDirectory, "Data", "latest-snapshot.json");
        _worker = Task.Run(ProcessQueueAsync);
    }

    public void Publish(ConnectorSnapshot snapshot) => _channel.Writer.TryWrite(snapshot);

    private async Task ProcessQueueAsync()
    {
        long lastFileWrite = 0;
        await foreach (var snapshot in _channel.Reader.ReadAllAsync(_cts.Token))
        {
            try
            {
                long now = System.Diagnostics.Stopwatch.GetTimestamp();
                if (System.Diagnostics.Stopwatch.GetElapsedTime(lastFileWrite).TotalSeconds >= 1)
                {
                    lastFileWrite = now;
                    string? directory = Path.GetDirectoryName(_outputPath);
                    if (!string.IsNullOrWhiteSpace(directory))
                        Directory.CreateDirectory(directory);
                    await File.WriteAllTextAsync(_outputPath, JsonSerializer.Serialize(snapshot, JsonOptions), _cts.Token);
                }

                if (!string.IsNullOrWhiteSpace(_apiUrl))
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
                    {
                        Content = JsonContent.Create(snapshot, options: JsonOptions)
                    };

                    if (!string.IsNullOrWhiteSpace(_apiToken))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

                    using var response = await _httpClient.SendAsync(request, _cts.Token);
                    if (!response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync(_cts.Token);
                        Console.Error.WriteLine($"Laravel API returned {(int)response.StatusCode}: {body}");
                    }
                }
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Snapshot publish error: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        try { await _worker; } catch (OperationCanceledException) { }
        _cts.Cancel();
        _cts.Dispose();
        _httpClient.Dispose();
    }
}
