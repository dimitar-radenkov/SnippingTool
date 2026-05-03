using Pointframe.AutomationTests.Fixtures;
using Pointframe.AutomationTests.Support;
using Xunit;

namespace Pointframe.AutomationTests.Smoke;

public sealed class RecordingAnnotationToolSmokeTests : IClassFixture<DesktopAutomationFixture>
{
    private readonly DesktopAutomationFixture _fixture;

    public RecordingAnnotationToolSmokeTests(DesktopAutomationFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<string> AllRecordingToolIds => new()
    {
        AutomationIds.RecordingOverlayWindowToolPen,
        AutomationIds.RecordingOverlayWindowToolArrow,
        AutomationIds.RecordingOverlayWindowToolRectangle,
        AutomationIds.RecordingOverlayWindowToolText,
        AutomationIds.RecordingOverlayWindowToolBlur,
    };

    [Theory]
    [MemberData(nameof(AllRecordingToolIds))]
    [Trait("Category", "DesktopAutomation")]
    public void SelectRecordingTool_RecordingOverlayRemainsOperational(string toolAutomationId)
    {
        _fixture.EnsureRecordingBackendAvailable();
        _fixture.SeedSettings(autoSaveScreenshots: false, recordingFps: 10);

        using var app = AutomationApp.Launch("--automation-open-sample-recording-overlay", _fixture.CreateEnvironmentVariables());
        Assert.Equal(AutomationIds.OverlayWindowRoot, app.MainWindowAutomationId);

        app.ClickFirstButton(
            AutomationIds.OverlayWindowRecord,
            AutomationIds.OverlayWindowCompactRecord);

        app.SwitchToTopLevelWindow(AutomationIds.RecordingOverlayWindowRoot);
        Assert.Equal(AutomationIds.RecordingOverlayWindowRoot, app.MainWindowAutomationId);

        app.ClickButton(AutomationIds.RecordingOverlayWindowToggleAnnotation);

        app.SelectRadioButton(toolAutomationId);

        Assert.NotNull(app.FindRequiredElement(AutomationIds.RecordingOverlayWindowStop));

        app.ClickButton(AutomationIds.RecordingOverlayWindowStop);
        app.WaitForExit();
    }
}
