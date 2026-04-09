using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnippingTool.Models;
using SnippingTool.Services;
using SnippingTool.Tests.Services.Handlers;
using Xunit;

namespace SnippingTool.Tests.Services;

public sealed class RecordingCursorEffectsServiceTests
{
    private static readonly RecordingSessionGeometry Geometry = new(
        new Int32Rect(1920, 0, 2880, 1620),
        new Int32Rect(2070, 100, 1200, 800),
        new Int32Rect(1920, 0, 2880, 1560),
        new Rect(0, 0, 1920, 810),
        new Rect(0, 0, 1920, 780),
        new Rect(100, 50, 800, 400),
        "DISPLAY2",
        1.5,
        2.0);

    [Fact]
    public void Start_InitializesCanvasAndStartsMouseHook()
    {
        StaTestHelper.Run(() =>
        {
            var canvas = new Canvas();
            var mouseHook = new FakeMouseHookService();
            using var service = CreateService(canvas, mouseHook, () => false);

            service.Start();

            Assert.Equal(Geometry.HostBoundsDips.Width, canvas.Width);
            Assert.Equal(Geometry.HostBoundsDips.Height, canvas.Height);
            Assert.Equal(0d, Canvas.GetLeft(canvas));
            Assert.Equal(0d, Canvas.GetTop(canvas));
            Assert.Equal(1, mouseHook.StartCount);

            var highlightRing = Assert.IsType<Ellipse>(Assert.Single(canvas.Children));
            Assert.Equal(Visibility.Collapsed, highlightRing.Visibility);
        });
    }

    [Fact]
    public void MouseButtonDown_LeftInsideCapture_AddsRippleWhenAnnotationInputIsNotArmed()
    {
        StaTestHelper.Run(() =>
        {
            var canvas = new Canvas();
            var mouseHook = new FakeMouseHookService();
            using var service = CreateService(canvas, mouseHook, () => false);
            service.Start();

            mouseHook.RaiseMouseButtonDown(MouseHookButton.Left, new Point(2085, 140));

            Assert.Equal(2, canvas.Children.Count);
        });
    }

    [Fact]
    public void MouseButtonDown_LeftInsideCapture_DoesNotAddRippleWhenAnnotationInputIsArmed()
    {
        StaTestHelper.Run(() =>
        {
            var canvas = new Canvas();
            var mouseHook = new FakeMouseHookService();
            using var service = CreateService(canvas, mouseHook, () => true);
            service.Start();

            mouseHook.RaiseMouseButtonDown(MouseHookButton.Left, new Point(2085, 140));

            Assert.Single(canvas.Children);
        });
    }

    [Fact]
    public void MouseButtonDown_LeftOutsideCapture_DoesNotAddRipple()
    {
        StaTestHelper.Run(() =>
        {
            var canvas = new Canvas();
            var mouseHook = new FakeMouseHookService();
            using var service = CreateService(canvas, mouseHook, () => false);
            service.Start();

            mouseHook.RaiseMouseButtonDown(MouseHookButton.Left, new Point(2000, 40));

            Assert.Single(canvas.Children);
        });
    }

    [Fact]
    public void Dispose_StopsMouseHookAndClearsCanvas()
    {
        StaTestHelper.Run(() =>
        {
            var canvas = new Canvas();
            var mouseHook = new FakeMouseHookService();
            var service = CreateService(canvas, mouseHook, () => false);
            service.Start();
            mouseHook.RaiseMouseButtonDown(MouseHookButton.Left, new Point(2085, 140));

            service.Dispose();

            Assert.Equal(1, mouseHook.StopCount);
            Assert.Empty(canvas.Children);
        });
    }

    private static RecordingCursorEffectsService CreateService(
        Canvas canvas,
        IMouseHookService mouseHookService,
        Func<bool> isAnnotationInputArmed)
    {
        var settingsService = new Mock<IUserSettingsService>();
        settingsService.SetupGet(service => service.Current).Returns(new UserSettings());

        return new RecordingCursorEffectsService(
            canvas,
            Geometry,
            mouseHookService,
            settingsService.Object,
            isAnnotationInputArmed,
            NullLogger<RecordingCursorEffectsService>.Instance);
    }

    private sealed class FakeMouseHookService : IMouseHookService
    {
        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public event EventHandler<MouseHookEventArgs>? MouseButtonDown;

        public void Start()
        {
            StartCount++;
        }

        public void Stop()
        {
            StopCount++;
        }

        public void RaiseMouseButtonDown(MouseHookButton button, Point screenPoint)
        {
            MouseButtonDown?.Invoke(this, new MouseHookEventArgs(button, screenPoint));
        }
    }
}