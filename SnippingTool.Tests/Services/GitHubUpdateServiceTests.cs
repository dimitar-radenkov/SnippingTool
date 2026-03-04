using System.Net;
using System.Net.Http;
using System.Reflection;
using SnippingTool.Services;
using Xunit;

namespace SnippingTool.Tests.Services;

public sealed class GitHubUpdateServiceTests
{
    private static GitHubUpdateService CreateService(string responseJson)
    {
        var handler = new FakeHttpMessageHandler(responseJson);
        var http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5),
            DefaultRequestHeaders = { { "User-Agent", "SnippingTool-Test" } },
        };
        return GitHubUpdateService.CreateForTesting(http);
    }

    [Fact]
    public async Task CurrentIsLatest_ReturnsNoUpdate()
    {
        var current = Assembly.GetAssembly(typeof(GitHubUpdateService))?.GetName().Version ?? new Version(1, 0, 0);
        var json = $$"""
            {
              "tag_name": "v{{current.Major}}.{{current.Minor}}.{{current.Build}}",
              "assets": []
            }
            """;

        var result = await CreateService(json).CheckForUpdatesAsync();

        Assert.False(result.IsUpdateAvailable);
    }

    [Fact]
    public async Task NewerVersionAvailable_ReturnsUpdateWithDownloadUrl()
    {
        const string json = """
            {
              "tag_name": "v999.0.0",
              "assets": [
                {
                  "name": "SnippingTool-Setup-999.0.0.exe",
                  "browser_download_url": "https://github.com/dimitar-radenkov/SnippingTool/releases/download/v999.0.0/SnippingTool-Setup-999.0.0.exe"
                }
              ]
            }
            """;

        var result = await CreateService(json).CheckForUpdatesAsync();

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal(new Version(999, 0, 0), result.LatestVersion);
        Assert.Contains("SnippingTool-Setup-999.0.0.exe", result.DownloadUrl);
    }

    [Fact]
    public async Task InvalidTagFormat_ReturnsNoUpdate()
    {
        const string json = """
            {
              "tag_name": "not-a-version",
              "assets": []
            }
            """;

        var result = await CreateService(json).CheckForUpdatesAsync();

        Assert.False(result.IsUpdateAvailable);
    }

    private sealed class FakeHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json"),
            });
    }
}
