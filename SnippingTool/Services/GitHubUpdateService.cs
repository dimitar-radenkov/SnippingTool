using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using SnippingTool.Models;

namespace SnippingTool.Services;

public sealed class GitHubUpdateService : IUpdateService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(15),
        DefaultRequestHeaders = { { "User-Agent", "SnippingTool" } },
    };

    private const string LatestReleaseUrl =
        "https://api.github.com/repos/dimitar-radenkov/SnippingTool/releases/latest";

    private static readonly string[] AllowedDownloadHosts =
    [
        "https://github.com/",
        "https://objects.githubusercontent.com/",
    ];

    private readonly HttpClient _http;

    public GitHubUpdateService() : this(Http) { }

    private GitHubUpdateService(HttpClient http) => _http = http;

    public static GitHubUpdateService CreateForTesting(HttpClient http) => new(http);

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var release = await _http.GetFromJsonAsync<GitHubRelease>(LatestReleaseUrl, cancellationToken)
            ?? throw new InvalidOperationException("Empty response from GitHub API.");

        var latestVersion = ParseVersion(release.TagName);
        var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);

        if (latestVersion <= currentVersion)
        {
            return new UpdateCheckResult(false, latestVersion, string.Empty);
        }

        var downloadUrl = release.Assets
            .FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            ?.BrowserDownloadUrl ?? string.Empty;

        if (!string.IsNullOrEmpty(downloadUrl) && !IsAllowedDownloadUrl(downloadUrl))
        {
            throw new InvalidOperationException($"Unexpected download URL host: {downloadUrl}");
        }

        return new UpdateCheckResult(true, latestVersion, downloadUrl);
    }

    private static Version ParseVersion(string tag)
    {
        var clean = tag.TrimStart('v', 'V');
        return Version.TryParse(clean, out var v) ? v : new Version(0, 0, 0);
    }

    private static bool IsAllowedDownloadUrl(string url) =>
        AllowedDownloadHosts.Any(h => url.StartsWith(h, StringComparison.OrdinalIgnoreCase));

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string TagName { get; init; } = string.Empty;
        [JsonPropertyName("assets")] public List<GitHubAsset> Assets { get; init; } = [];
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
        [JsonPropertyName("browser_download_url")] public string BrowserDownloadUrl { get; init; } = string.Empty;
    }
}
