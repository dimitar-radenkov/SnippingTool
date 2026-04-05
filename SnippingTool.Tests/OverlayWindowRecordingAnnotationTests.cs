using System.Windows;
using SnippingTool.Models;
using Xunit;

namespace SnippingTool.Tests;

public sealed class OverlayWindowRecordingAnnotationTests
{
    [Fact]
    public void CalculateRecordingCaptureBounds_ScalesRecordingBlurRegionByDpi()
    {
        var windowBounds = new Rect(100, 50, 400, 300);
        var blurRegion = new BlurShapeParameters(10, 20, 30, 40);

        var result = OverlayWindow.CalculateRecordingCaptureBounds(windowBounds, blurRegion, 1.5, 2.0);

        Assert.Equal(165, result.X);
        Assert.Equal(140, result.Y);
        Assert.Equal(45, result.Width);
        Assert.Equal(80, result.Height);
    }

    [Fact]
    public void CalculateRecordingCaptureBounds_ClampsToMinimumSize()
    {
        var windowBounds = new Rect(0, 0, 10, 10);
        var blurRegion = new BlurShapeParameters(0.2, 0.2, 0.1, 0.1);

        var result = OverlayWindow.CalculateRecordingCaptureBounds(windowBounds, blurRegion, 1.0, 1.0);

        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
    }
}