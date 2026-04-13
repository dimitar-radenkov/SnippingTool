using System.Diagnostics;
using System.IO;
using SnippingTool.Services;
using Xunit;

namespace SnippingTool.Tests.Services;

public sealed class ProcessServiceTests : IDisposable
{
    private readonly string _outputPath = Path.Combine(Path.GetTempPath(), $"snip-process-{Guid.NewGuid():N}.txt");

    [Fact]
    public void Start_LaunchesConfiguredProcess()
    {
        var sut = new ProcessService();
        var startInfo = new ProcessStartInfo("cmd.exe", $"/c echo coverage> \"{_outputPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        sut.Start(startInfo);

        Assert.True(SpinWait.SpinUntil(() => this.TryReadOutput() is "coverage", millisecondsTimeout: 3000));
    }

    public void Dispose()
    {
        if (File.Exists(_outputPath))
        {
            SpinWait.SpinUntil(this.TryDeleteOutput, millisecondsTimeout: 3000);
        }
    }

    private string? TryReadOutput()
    {
        if (!File.Exists(_outputPath))
        {
            return null;
        }

        try
        {
            return File.ReadAllText(_outputPath).Trim();
        }
        catch (IOException)
        {
            return null;
        }
    }

    private bool TryDeleteOutput()
    {
        try
        {
            File.Delete(_outputPath);
            return !File.Exists(_outputPath);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}