using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SnippingTool.Services;

namespace SnippingTool.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    public string Version { get; }

    public event Action? RequestClose;

    public AboutViewModel(IAppVersionService appVersion)
    {
        var v = appVersion.Current;
        Version = $"Version {v.Major}.{v.Minor}.{v.Build}";
    }

    [RelayCommand]
    private void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    [RelayCommand]
    private void Close() => RequestClose?.Invoke();
}
