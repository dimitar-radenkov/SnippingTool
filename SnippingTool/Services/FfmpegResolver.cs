namespace SnippingTool.Services;

internal static class FfmpegResolver
{
    private const string FfmpegPathOverrideKey = "SnippingTool.FfmpegPath";

    internal static string Resolve()
    {
        if (AppContext.GetData(FfmpegPathOverrideKey) is string overridePath && !string.IsNullOrWhiteSpace(overridePath))
        {
            return overridePath;
        }

        // 1. Look next to the application binary
        var appDir = AppContext.BaseDirectory;
        var candidate = System.IO.Path.Combine(appDir, "ffmpeg.exe");
        if (System.IO.File.Exists(candidate))
        {
            return candidate;
        }

        // 2. Look in Assets/ffmpeg subfolder
        candidate = System.IO.Path.Combine(appDir, "Assets", "ffmpeg", "ffmpeg.exe");
        if (System.IO.File.Exists(candidate))
        {
            return candidate;
        }

        // 3. Fall back to PATH-resolved ffmpeg
        return "ffmpeg.exe";
    }
}
