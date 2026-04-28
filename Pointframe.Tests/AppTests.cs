using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Pointframe.Services;
using Pointframe.Tests.Services.Handlers;
using Pointframe.ViewModels;
using Xunit;

namespace Pointframe.Tests;

public sealed class AppTests
{
    [Fact]
    public void ConfigureServices_RegistersCoreServicesAndFactories()
    {
        var services = new ServiceCollection();

        typeof(App)
            .GetMethod("ConfigureServices", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [services]);

        using var provider = services.BuildServiceProvider();

        Assert.IsType<DialogService>(provider.GetRequiredService<IDialogService>());
        Assert.IsType<MessageBoxService>(provider.GetRequiredService<IMessageBoxService>());
        Assert.NotNull(provider.GetRequiredService<Func<IScreenRecordingService, string, RecordingHudViewModel>>());
    }

    [Fact]
    public void RegisterAutomationWindow_WhenAutomationDisabled_DoesNotAttachHandler()
    {
        StaTestHelper.Run(() =>
        {
            var app = CreateAppWithoutRunning();
            SetField(app, "_isAutomationMode", false);
            var window = new Window();

            var closedHandlers = 0;
            window.Closed += (_, _) => closedHandlers++;

            app.RegisterAutomationWindow(window);
            window.Close();

            Assert.Equal(1, closedHandlers);
        });
    }

    private static App CreateAppWithoutRunning()
    {
        return (App)RuntimeHelpers.GetUninitializedObject(typeof(App));
    }

    private static void SetField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(target, value);
    }
}
