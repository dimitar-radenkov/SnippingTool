using System.Windows;
using Xunit;

namespace SnippingTool.Tests;

public class RecordingHudPositionTests
{
    // Work area used by all tests: 0,0 → 1920×1040
    private static readonly Rect WorkArea = new(0, 0, 1920, 1040);
    // HUD dimensions
    private const double HudW = 140;
    private const double HudH = 44;

    [Fact]
    public void ComputePosition_NormalRegion_CenteredBelowWithGap()
    {
        // Region at centre of screen, plenty of space all around
        var region = new Rect(600, 200, 400, 300);

        var (left, top) = RecordingHudWindow.ComputePosition(region, HudW, HudH, WorkArea);

        // Expected horizontal centre: 600 + (400 - 140) / 2 = 730
        Assert.Equal(730, left);
        // Expected vertical: 200 + 300 + 8 = 508
        Assert.Equal(508, top);
    }

    [Fact]
    public void ComputePosition_RegionNearRightEdge_ClampsLeft()
    {
        // Region so far right that centred HUD would exceed work area right
        var region = new Rect(1850, 200, 400, 300);

        var (left, _) = RecordingHudWindow.ComputePosition(region, HudW, HudH, WorkArea);

        // Must not exceed workArea.Right - hudWidth = 1920 - 140 = 1780
        Assert.Equal(1780, left);
    }

    [Fact]
    public void ComputePosition_RegionNearLeftEdge_ClampsLeft()
    {
        // Region so far left that centred HUD would go negative
        var region = new Rect(-300, 200, 200, 300);

        var (left, _) = RecordingHudWindow.ComputePosition(region, HudW, HudH, WorkArea);

        // Must not go below workArea.Left = 0
        Assert.Equal(0, left);
    }

    [Fact]
    public void ComputePosition_RegionNearBottom_ClampsTop()
    {
        // Region whose bottom + 8 + hudHeight would exceed work area bottom
        var region = new Rect(600, 1020, 400, 60);

        var (_, top) = RecordingHudWindow.ComputePosition(region, HudW, HudH, WorkArea);

        // region.Bottom = 1080, 1080 + 8 = 1088 > 1040 − 44 = 996 → clamped to 996
        Assert.Equal(WorkArea.Bottom - HudH, top);
    }
}
