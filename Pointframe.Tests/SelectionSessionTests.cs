using System.Windows;
using System.Windows.Media.Imaging;
using Xunit;

namespace Pointframe.Tests;

public sealed class SelectionSessionTests
{
    [Fact]
    public void CreateWholeScreenSelectionResult_HasFullScreenMode()
    {
        var bitmap = CreateBitmap(100, 80);
        var hostDips = new Rect(0, 0, 100, 80);
        var hostPixels = new Int32Rect(0, 0, 200, 160);

        var result = SelectionSession.CreateWholeScreenSelectionResult(
            "DISPLAY1",
            bitmap,
            hostDips,
            hostPixels,
            2d,
            2d);

        Assert.Equal(SelectionSessionMode.FullScreen, result.SessionMode);
    }

    [Fact]
    public void CreateWholeScreenSelectionResult_SelectionRectCoversFullHostBounds()
    {
        var bitmap = CreateBitmap(100, 80);
        var hostDips = new Rect(0, 0, 100, 80);
        var hostPixels = new Int32Rect(0, 0, 200, 160);

        var result = SelectionSession.CreateWholeScreenSelectionResult(
            "DISPLAY1",
            bitmap,
            hostDips,
            hostPixels,
            2d,
            2d);

        Assert.Equal(0d, result.SelectionRectDips.X);
        Assert.Equal(0d, result.SelectionRectDips.Y);
        Assert.Equal(hostDips.Width, result.SelectionRectDips.Width);
        Assert.Equal(hostDips.Height, result.SelectionRectDips.Height);
    }

    [Fact]
    public void CreateWholeScreenSelectionResult_SelectionPixelsMatchHostPixels()
    {
        var bitmap = CreateBitmap(100, 80);
        var hostDips = new Rect(0, 0, 100, 80);
        var hostPixels = new Int32Rect(0, 0, 200, 160);

        var result = SelectionSession.CreateWholeScreenSelectionResult(
            "DISPLAY1",
            bitmap,
            hostDips,
            hostPixels,
            2d,
            2d);

        Assert.Equal(hostPixels, result.SelectionBoundsPixels);
    }

    [Fact]
    public void CreateWholeScreenSelectionResult_MonitorNamePreserved()
    {
        var bitmap = CreateBitmap(40, 30);
        var result = SelectionSession.CreateWholeScreenSelectionResult(
            "\\\\.\\DISPLAY2",
            bitmap,
            new Rect(0, 0, 40, 30),
            new Int32Rect(0, 0, 80, 60),
            2d,
            2d);

        Assert.Equal("\\\\.\\DISPLAY2", result.MonitorName);
    }

    private static BitmapSource CreateBitmap(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            System.Windows.Media.PixelFormats.Bgra32,
            null,
            pixels,
            width * 4);
        bitmap.Freeze();
        return bitmap;
    }
}
