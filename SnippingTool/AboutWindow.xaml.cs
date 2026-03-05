using System.Windows;
using SnippingTool.ViewModels;

namespace SnippingTool;

public partial class AboutWindow : Window
{
    public AboutWindow(AboutViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.RequestClose += Close;
    }
}
