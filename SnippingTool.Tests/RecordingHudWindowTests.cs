using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnippingTool.Models;
using SnippingTool.Services;
using SnippingTool.Tests.Services.Handlers;
using SnippingTool.ViewModels;
using Xunit;

namespace SnippingTool.Tests;

public sealed class RecordingHudWindowTests
{
    [Fact]
    public void SavedText_Click_InvokesOpenOutputFolderCommand()
    {
        StaTestHelper.Run(() =>
        {
            var window = CreateWindow(out var viewModel, out var processMock, out _);

            InvokePrivateHandler(window, "SavedText_Click", window, null!);

            processMock.Verify(service => service.Start(It.IsAny<System.Diagnostics.ProcessStartInfo>()), Times.Once);
            Assert.False(viewModel.IsStopped);
        });
    }

    [Fact]
    public void ToolButton_Checked_UpdatesSelectedTool()
    {
        StaTestHelper.Run(() =>
        {
            var window = CreateWindow(out _, out _, out var annotationViewModel);
            var button = new RadioButton { Tag = nameof(AnnotationTool.Blur) };

            InvokePrivateHandler(window, "ToolButton_Checked", button, new RoutedEventArgs());

            Assert.Equal(AnnotationTool.Blur, annotationViewModel.SelectedTool);
        });
    }

    [Fact]
    public void OnSourceInitialized_StartsElapsedTimer()
    {
        StaTestHelper.Run(() =>
        {
            var window = CreateWindow(out var viewModel, out _, out _);

            InvokeProtectedHandler(window, "OnSourceInitialized", EventArgs.Empty);

            var elapsedCts = GetPrivateField<CancellationTokenSource?>(viewModel, "_elapsedCts");
            Assert.NotNull(elapsedCts);
            Assert.False(elapsedCts!.IsCancellationRequested);
        });
    }

    [Fact]
    public void OnClosed_CancelsElapsedTimer()
    {
        StaTestHelper.Run(() =>
        {
            var window = CreateWindow(out var viewModel, out _, out _);

            InvokeProtectedHandler(window, "OnSourceInitialized", EventArgs.Empty);
            InvokeProtectedHandler(window, "OnClosed", EventArgs.Empty);

            var elapsedCts = GetPrivateField<CancellationTokenSource?>(viewModel, "_elapsedCts");
            Assert.NotNull(elapsedCts);
            Assert.True(elapsedCts!.IsCancellationRequested);
        });
    }

    private static RecordingHudWindow CreateWindow(
        out RecordingHudViewModel viewModel,
        out Mock<IProcessService> processMock,
        out RecordingAnnotationViewModel annotationViewModel)
    {
        var settings = new UserSettings { HudGapPixels = 8 };
        var settingsMock = new Mock<IUserSettingsService>();
        settingsMock.SetupGet(service => service.Current).Returns(settings);
        processMock = new Mock<IProcessService>();
        viewModel = new RecordingHudViewModel(
            Mock.Of<IScreenRecordingService>(service => service.IsPaused == false),
            @"C:\Videos\rec.mp4",
            settingsMock.Object,
            processMock.Object,
            Mock.Of<IGifExportService>(),
            NullLogger<RecordingHudViewModel>.Instance);

        var annotationSettings = new Mock<IUserSettingsService>();
        annotationSettings.SetupGet(service => service.Current).Returns(new UserSettings());
        annotationViewModel = new RecordingAnnotationViewModel(
            new AnnotationGeometryService(),
            NullLogger<RecordingAnnotationViewModel>.Instance,
            annotationSettings.Object,
            Mock.Of<SnippingTool.Services.Messaging.IEventAggregator>());
        viewModel.AttachAnnotationSession(annotationViewModel, () => false);

        return new RecordingHudWindow(viewModel, new Rect(100, 100, 300, 200), settingsMock.Object);
    }

    private static void InvokePrivateHandler(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(target, args);
    }

    private static void InvokeProtectedHandler(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(target, args);
    }

    private static T? GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T?)field.GetValue(target);
    }
}