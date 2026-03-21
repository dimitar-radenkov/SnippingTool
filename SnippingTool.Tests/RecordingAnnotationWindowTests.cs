using System.Windows;
using SnippingTool.Models;
using Xunit;

namespace SnippingTool.Tests;

public sealed class RecordingAnnotationWindowTests
{
    [Fact]
    public void CalculateCaptureBounds_ScalesRecordingBlurRegionByDpi()
    {
        var windowBounds = new Rect(100, 50, 400, 300);
        var blurRegion = new BlurShapeParameters(10, 20, 30, 40);

        var result = RecordingAnnotationWindow.CalculateCaptureBounds(windowBounds, blurRegion, 1.5, 2.0);

        Assert.Equal(165, result.X);
        Assert.Equal(140, result.Y);
        Assert.Equal(45, result.Width);
        Assert.Equal(80, result.Height);
    }
}