using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnippingTool.Models;
using SnippingTool.Services;
using SnippingTool.Services.Messaging;
using SnippingTool.Tests.Services.Handlers;
using SnippingTool.ViewModels;
using Xunit;

namespace SnippingTool.Tests;

public sealed class RecordingAnnotationWindowTests
{
    [Fact]
    public void CalculateCaptureBounds_ScalesRecordingBlurRegionByDpi()
    {
        // Arrange
        var windowBounds = new Rect(100, 50, 400, 300);
        var blurRegion = new BlurShapeParameters(10, 20, 30, 40);

        // Act
        var result = RecordingAnnotationWindow.CalculateCaptureBounds(windowBounds, blurRegion, 1.5, 2.0);

        // Assert
        Assert.Equal(165, result.X);
        Assert.Equal(140, result.Y);
        Assert.Equal(45, result.Width);
        Assert.Equal(80, result.Height);
    }

    [Fact]
    public void CalculateCaptureBounds_ClampsToMinimumSize()
    {
        // Arrange
        var windowBounds = new Rect(0, 0, 10, 10);
        var blurRegion = new BlurShapeParameters(0.2, 0.2, 0.1, 0.1);

        // Act
        var result = RecordingAnnotationWindow.CalculateCaptureBounds(windowBounds, blurRegion, 1.0, 1.0);

        // Assert
        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
    }

    [Fact]
    public void ToggleInputMode_FlipsInputState()
    {
        StaTestHelper.Run(() =>
        {
            var window = CreateWindow();
            try
            {
                Assert.True(window.ToggleInputMode());
                Assert.False(window.ToggleInputMode());
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetInputArmed_WhenRequestedStateMatches_DoesNothing()
    {
        StaTestHelper.Run(() =>
        {
            var window = CreateWindow();
            try
            {
                window.SetInputArmed(false);

                Assert.False(window.IsInputArmed);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetInputArmed_WhenDisablingWithActiveEditor_RemainsArmed()
    {
        StaTestHelper.Run(() =>
        {
            var window = CreateWindow();
            try
            {
                window.SetInputArmed(true);

                var annotationCanvas = GetAnnotationCanvas(window);
                annotationCanvas.Children.Add(new TextBox());

                window.SetInputArmed(false);

                Assert.True(window.IsInputArmed);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void HandleClearRequested_RemovesAllChildrenAndResetsState()
    {
        StaTestHelper.Run(() =>
        {
            var window = CreateWindow(out var vm);
            try
            {
                var annotationCanvas = GetAnnotationCanvas(window);
                annotationCanvas.Children.Add(new Border());
                annotationCanvas.Children.Add(new TextBox());

                InvokePrivate(window, "HandleClearRequested");

                Assert.Empty(annotationCanvas.Children);
                Assert.False(vm.HasActiveAnnotations);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void HandleUndoAndRedo_MoveElementsInAndOutOfCanvas()
    {
        StaTestHelper.Run(() =>
        {
            var window = CreateWindow(out _);
            try
            {
                var annotationCanvas = GetAnnotationCanvas(window);
                var first = new Border();
                var second = new Border();
                annotationCanvas.Children.Add(first);
                annotationCanvas.Children.Add(second);

                InvokePrivate(window, "HandleUndoGroupAsync", new UndoGroupMessage(new object[] { first }));
                Assert.Single(annotationCanvas.Children);

                InvokePrivate(window, "HandleRedoGroupAsync", new RedoGroupMessage(new object[] { first }));
                Assert.Equal(2, annotationCanvas.Children.Count);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void CaptureLiveBlurSource_ReturnsCroppedBitmapAndRestoresVisibility()
    {
        StaTestHelper.Run(() =>
        {
            var window = CreateWindow(out _);
            try
            {
                window.Left = 0;
                window.Top = 0;
                window.Width = 100;
                window.Height = 100;
                window.Visibility = Visibility.Visible;

                SetRendererBackground(window, CreateBitmap(100, 100), 1.0, 1.0);

                var result = InvokeCaptureLiveBlur(window, new BlurShapeParameters(10, 10, 20, 20));

                Assert.NotNull(result);
                Assert.Equal(20, result!.PixelWidth);
                Assert.Equal(20, result.PixelHeight);
                Assert.Equal(Visibility.Visible, window.Visibility);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static RecordingAnnotationWindow CreateWindow(out RecordingAnnotationViewModel vm)
    {
        var eventAggregator = new DefaultEventAggregator(NullLogger<DefaultEventAggregator>.Instance);
        vm = new RecordingAnnotationViewModel(
            new AnnotationGeometryService(),
            NullLogger<RecordingAnnotationViewModel>.Instance,
            Mock.Of<IUserSettingsService>(s => s.Current == new UserSettings()),
            eventAggregator);

        var screenCapture = new Mock<IScreenCaptureService>();
        screenCapture
            .Setup(service => service.Capture(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int _, int _, int width, int height) => CreateBitmap(Math.Max(1, width), Math.Max(1, height)));

        return new RecordingAnnotationWindow(
            vm,
            new Rect(0, 0, 100, 100),
            1.0,
            1.0,
            screenCapture.Object,
            eventAggregator,
            LoggerFactory.Create(builder => { }));
    }

    private static RecordingAnnotationWindow CreateWindow() => CreateWindow(out _);

    private static Canvas GetAnnotationCanvas(RecordingAnnotationWindow window)
    {
        return (Canvas)window.FindName("AnnotationCanvas")!;
    }

    private static object GetRenderer(RecordingAnnotationWindow window)
    {
        var field = typeof(RecordingAnnotationWindow).GetField("_renderer", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(window)!;
    }

    private static void SetRendererBackground(RecordingAnnotationWindow window, BitmapSource bitmap, double dpiX, double dpiY)
    {
        var renderer = GetRenderer(window);
        var method = renderer.GetType().GetMethod("SetBackground", BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        method.Invoke(renderer, [bitmap, dpiX, dpiY]);
    }

    private static BitmapSource CreateBitmap(int width, int height)
    {
        var pixels = new int[width * height];
        var bitmap = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapSource? InvokeCaptureLiveBlur(RecordingAnnotationWindow window, BlurShapeParameters parameters)
    {
        return (BitmapSource?)InvokePrivate(window, "CaptureLiveBlurSource", parameters);
    }

    private static object? InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method.Invoke(target, args);
    }
}