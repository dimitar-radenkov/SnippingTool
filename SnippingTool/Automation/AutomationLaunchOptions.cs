namespace SnippingTool.Automation;

internal sealed class AutomationLaunchOptions
{
    private const string OpenSettingsArgument = "--automation-open-settings";
    private const string OpenAboutArgument = "--automation-open-about";
    private const string OpenSampleOverlayArgument = "--automation-open-sample-overlay";
    private const string OpenSampleRecordingOverlayArgument = "--automation-open-sample-recording-overlay";

    private AutomationLaunchOptions(
        bool openSettingsWindow,
        bool openAboutWindow,
        bool openSampleOverlayWindow,
        bool openSampleRecordingOverlayWindow)
    {
        OpenSettingsWindow = openSettingsWindow;
        OpenAboutWindow = openAboutWindow;
        OpenSampleOverlayWindow = openSampleOverlayWindow;
        OpenSampleRecordingOverlayWindow = openSampleRecordingOverlayWindow;
    }

    public bool IsAutomationMode =>
        OpenSettingsWindow
        || OpenAboutWindow
        || OpenSampleOverlayWindow
        || OpenSampleRecordingOverlayWindow;

    public bool OpenSettingsWindow { get; }

    public bool OpenAboutWindow { get; }

    public bool OpenSampleOverlayWindow { get; }

    public bool OpenSampleRecordingOverlayWindow { get; }

    public static AutomationLaunchOptions Parse(IEnumerable<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var parsedArguments = args.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new AutomationLaunchOptions(
            parsedArguments.Contains(OpenSettingsArgument),
            parsedArguments.Contains(OpenAboutArgument),
            parsedArguments.Contains(OpenSampleOverlayArgument),
            parsedArguments.Contains(OpenSampleRecordingOverlayArgument));
    }
}
