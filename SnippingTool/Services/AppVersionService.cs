using System.Diagnostics;
using System.Reflection;

namespace SnippingTool.Services;

public sealed class AppVersionService : IAppVersionService
{
    public Version Current { get; } = GetCurrentVersion();

    private static Version GetCurrentVersion()
    {
        var location = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrEmpty(location))
        {
            var fvi = FileVersionInfo.GetVersionInfo(location);
            if (Version.TryParse(fvi.FileVersion, out var fileVersion))
            {
                return fileVersion;
            }
        }

        return Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
    }
}
