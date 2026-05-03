using Pointframe.AutomationTests.Fixtures;
using Pointframe.AutomationTests.Support;
using Xunit;

namespace Pointframe.AutomationTests.Smoke;

public sealed class RecordingHudInteractionTests : IClassFixture<DesktopAutomationFixture>
{
    private readonly DesktopAutomationFixture _fixture;

    public RecordingHudInteractionTests(DesktopAutomationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "DesktopAutomation")]
    public void PauseResume_CanPauseAndResumeThenStop()
    {
        _fixture.EnsureRecordingBackendAvailable();
        _fixture.SeedSettings(autoSaveScreenshots: false, recordingFps: 10);

        using var app = AutomationApp.Launch("--automation-open-sample-recording-overlay", _fixture.CreateEnvironmentVariables());
        app.ClickFirstButton(AutomationIds.OverlayWindowRecord, AutomationIds.OverlayWindowCompactRecord);
        app.SwitchToTopLevelWindow(AutomationIds.RecordingOverlayWindowRoot);

        app.ClickButton(AutomationIds.RecordingOverlayWindowPauseResume);
        app.ClickButton(AutomationIds.RecordingOverlayWindowPauseResume);

        Assert.NotNull(app.FindRequiredElement(AutomationIds.RecordingOverlayWindowStop));

        app.ClickButton(AutomationIds.RecordingOverlayWindowStop);
        app.WaitForExit();
    }

    [Fact]
    [Trait("Category", "DesktopAutomation")]
    public void MinimizeHud_ShowsCompactHud()
    {
        _fixture.EnsureRecordingBackendAvailable();
        _fixture.SeedSettings(autoSaveScreenshots: false, recordingFps: 10);

        using var app = AutomationApp.Launch("--automation-open-sample-recording-overlay", _fixture.CreateEnvironmentVariables());
        app.ClickFirstButton(AutomationIds.OverlayWindowRecord, AutomationIds.OverlayWindowCompactRecord);
        app.SwitchToTopLevelWindow(AutomationIds.RecordingOverlayWindowRoot);

        app.ClickButton(AutomationIds.RecordingOverlayWindowMinimizeHud);

        Assert.NotNull(app.FindRequiredElement(AutomationIds.RecordingOverlayWindowHudCompact));

        app.HoverElement(AutomationIds.RecordingOverlayWindowHudCompact);
        app.ClickButton(AutomationIds.RecordingOverlayWindowCompactStop);
        app.WaitForExit();
    }

    [Fact]
    [Trait("Category", "DesktopAutomation")]
    public void ExpandHud_RestoresFullHud()
    {
        _fixture.EnsureRecordingBackendAvailable();
        _fixture.SeedSettings(autoSaveScreenshots: false, recordingFps: 10);

        using var app = AutomationApp.Launch("--automation-open-sample-recording-overlay", _fixture.CreateEnvironmentVariables());
        app.ClickFirstButton(AutomationIds.OverlayWindowRecord, AutomationIds.OverlayWindowCompactRecord);
        app.SwitchToTopLevelWindow(AutomationIds.RecordingOverlayWindowRoot);

        app.ClickButton(AutomationIds.RecordingOverlayWindowMinimizeHud);
        Assert.NotNull(app.FindRequiredElement(AutomationIds.RecordingOverlayWindowHudCompact));

        app.HoverElement(AutomationIds.RecordingOverlayWindowHudCompact);
        app.ClickButton(AutomationIds.RecordingOverlayWindowExpandHud);
        Assert.NotNull(app.FindRequiredElement(AutomationIds.RecordingOverlayWindowStop));

        app.ClickButton(AutomationIds.RecordingOverlayWindowStop);
        app.WaitForExit();
    }

    [Fact]
    [Trait("Category", "DesktopAutomation")]
    public void CompactStop_StopsRecordingFromMinimizedHud()
    {
        _fixture.EnsureRecordingBackendAvailable();
        _fixture.SeedSettings(autoSaveScreenshots: false, recordingFps: 10);

        using var app = AutomationApp.Launch("--automation-open-sample-recording-overlay", _fixture.CreateEnvironmentVariables());
        app.ClickFirstButton(AutomationIds.OverlayWindowRecord, AutomationIds.OverlayWindowCompactRecord);
        app.SwitchToTopLevelWindow(AutomationIds.RecordingOverlayWindowRoot);

        app.ClickButton(AutomationIds.RecordingOverlayWindowMinimizeHud);
        app.HoverElement(AutomationIds.RecordingOverlayWindowHudCompact);
        app.ClickButton(AutomationIds.RecordingOverlayWindowCompactStop);
        app.WaitForExit();
    }
}
