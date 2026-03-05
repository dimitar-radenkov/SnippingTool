using System.Reflection;

namespace SnippingTool.Services;

public sealed class AppVersionService : IAppVersionService
{
    public Version Current { get; } =
        Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
}
