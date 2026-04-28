using System.Windows;
using Xunit;

namespace Pointframe.Tests;

public sealed class OverlayToolbarLayoutTests
{
    [Fact]
    public void Calculate_LargeSelection_KeepsFullActionBarWithoutOverlap()
    {
        var selectionRect = new Rect(400, 220, 700, 320);
        var layout = OverlayToolbarLayoutHelper.Calculate(
            selectionRect,
            new Size(1600, 900),
            new Size(42, 430),
            new Size(360, 50),
            new Size(270, 48));

        Assert.Equal(OverlayActionBarMode.Full, layout.ActionBarMode);
        Assert.False(layout.ToolBounds.IntersectsWith(layout.ActionBounds));
        Assert.False(layout.ToolBounds.IntersectsWith(selectionRect));
        Assert.False(layout.ActionBounds.IntersectsWith(selectionRect));
    }

    [Fact]
    public void Calculate_SmallSelection_UsesCompactActionBar()
    {
        var selectionRect = new Rect(550, 170, 260, 180);
        var layout = OverlayToolbarLayoutHelper.Calculate(
            selectionRect,
            new Size(1365, 768),
            new Size(42, 430),
            new Size(360, 50),
            new Size(270, 48));

        Assert.Equal(OverlayActionBarMode.Compact, layout.ActionBarMode);
        Assert.False(layout.ToolBounds.IntersectsWith(layout.ActionBounds));
        Assert.False(layout.ToolBounds.IntersectsWith(selectionRect));
        Assert.False(layout.ActionBounds.IntersectsWith(selectionRect));
    }

    [Fact]
    public void Calculate_SelectionNearRightEdge_MovesToolbarAwayFromSelection()
    {
        var selectionRect = new Rect(1250, 180, 240, 220);
        var layout = OverlayToolbarLayoutHelper.Calculate(
            selectionRect,
            new Size(1600, 900),
            new Size(42, 430),
            new Size(360, 50),
            new Size(270, 48));

        Assert.True(layout.ToolBounds.Right <= selectionRect.Left || layout.ToolBounds.Left >= selectionRect.Right);
        Assert.False(layout.ToolBounds.IntersectsWith(layout.ActionBounds));
        Assert.False(layout.ActionBounds.IntersectsWith(selectionRect));
    }

    [Fact]
    public void Calculate_WhenNothingFits_UsesFallbackCompactLayout()
    {
        var selectionRect = new Rect(16, 16, 88, 88);
        var layout = OverlayToolbarLayoutHelper.Calculate(
            selectionRect,
            new Size(120, 120),
            new Size(32, 32),
            new Size(32, 32),
            new Size(32, 32));

        Assert.Equal(OverlayActionBarMode.Compact, layout.ActionBarMode);
        Assert.True(layout.ToolBounds.Left >= 0);
        Assert.True(layout.ActionBounds.Bottom <= 120);
    }

    [Fact]
    public void Calculate_FullScreenMode_ToolOnLeftEdge()
    {
        var layout = OverlayToolbarLayoutHelper.Calculate(
            Rect.Empty,
            new Size(1920, 1080),
            new Size(42, 430),
            new Size(360, 50),
            new Size(270, 48),
            OverlayToolbarLayoutMode.FullScreen);

        Assert.True(layout.ToolBounds.Left < 100, "Tool should be near the left edge");
        Assert.True(layout.ToolBounds.Right <= 1920);
        Assert.True(layout.ToolBounds.Top >= 0);
        Assert.True(layout.ToolBounds.Bottom <= 1080);
    }

    [Fact]
    public void Calculate_FullScreenMode_ActionBarWithinOverlay()
    {
        var layout = OverlayToolbarLayoutHelper.Calculate(
            Rect.Empty,
            new Size(1920, 1080),
            new Size(42, 430),
            new Size(360, 50),
            new Size(270, 48),
            OverlayToolbarLayoutMode.FullScreen);

        Assert.True(layout.ActionBounds.Left >= 0);
        Assert.True(layout.ActionBounds.Right <= 1920);
        Assert.True(layout.ActionBounds.Top >= 0);
        Assert.True(layout.ActionBounds.Bottom <= 1080);
    }

    [Fact]
    public void Calculate_FullScreenMode_ToolAndActionBarDoNotOverlap()
    {
        var layout = OverlayToolbarLayoutHelper.Calculate(
            Rect.Empty,
            new Size(1920, 1080),
            new Size(42, 430),
            new Size(360, 50),
            new Size(270, 48),
            OverlayToolbarLayoutMode.FullScreen);

        Assert.False(layout.ToolBounds.IntersectsWith(layout.ActionBounds));
    }

    [Fact]
    public void Calculate_FullScreenMode_CompactScreenStillFitsLayout()
    {
        var layout = OverlayToolbarLayoutHelper.Calculate(
            Rect.Empty,
            new Size(800, 600),
            new Size(42, 300),
            new Size(360, 50),
            new Size(200, 40),
            OverlayToolbarLayoutMode.FullScreen);

        Assert.True(layout.ToolBounds.Right <= 800);
        Assert.True(layout.ActionBounds.Bottom <= 600);
        Assert.False(layout.ToolBounds.IntersectsWith(layout.ActionBounds));
    }
}
