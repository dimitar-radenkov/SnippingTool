using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace SnippingTool;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var v = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0, 0);
        VersionText.Text = $"Version {v.Major}.{v.Minor}.{v.Build}";
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
