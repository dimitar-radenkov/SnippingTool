using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using Application = System.Windows.Application;

namespace SnippingTool;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private HwndSource? _hotkeySource;

    private const int HotkeyId = 9000;
    private const uint VK_PRINTSCREEN = 0x2C;

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        RegisterGlobalHotkey();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_hotkeySource != null)
        {
            UnregisterHotKey(_hotkeySource.Handle, HotkeyId);
            _hotkeySource.Dispose();
        }
        _trayIcon?.Dispose();
        base.OnExit(e);
    }

    private void RegisterGlobalHotkey()
    {
        var p = new HwndSourceParameters("SnippingToolHotkey")
        {
            Width = 0,
            Height = 0,
            WindowStyle = unchecked((int)0x80000000) // WS_POPUP
        };
        _hotkeySource = new HwndSource(p);
        _hotkeySource.AddHook(HotkeyHook);
        RegisterHotKey(_hotkeySource.Handle, HotkeyId, 0, VK_PRINTSCREEN);
    }

    private IntPtr HotkeyHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            StartSnip();
            handled = true;
        }
        return IntPtr.Zero;
    }

    // ─── Tray menu handlers ───────────────────────────────────────────────

    private void TrayIcon_LeftClick(object sender, RoutedEventArgs e) => StartSnip();
    private void NewSnip_Click(object sender, RoutedEventArgs e) => StartSnip();
    private void Exit_Click(object sender, RoutedEventArgs e) => Current.Shutdown();

    private void FullScreen_Click(object sender, RoutedEventArgs e)
    {
        // Brief delay so the context menu closes before we capture
        System.Threading.Tasks.Task.Delay(250).ContinueWith(
            _ => Dispatcher.Invoke(CaptureFullScreen));
    }

    private void ActiveWindow_Click(object sender, RoutedEventArgs e)
    {
        // Capture the foreground window before the menu disappears
        var hwnd = GetForegroundWindow();
        System.Threading.Tasks.Task.Delay(350).ContinueWith(
            _ => Dispatcher.Invoke(() => CaptureWindow(hwnd)));
    }

    // ─── Capture implementations ──────────────────────────────────────────

    private void StartSnip()
    {
        var overlay = new OverlayWindow();
        overlay.Show();
    }

    private void CaptureFullScreen()
    {
        // SystemInformation.VirtualScreen returns bounds in physical (device) pixels
        var b = System.Windows.Forms.SystemInformation.VirtualScreen;
        var bitmap = ScreenCapture.Capture(b.X, b.Y, b.Width, b.Height);
        OnSnipCompleted(bitmap, System.Windows.Rect.Empty);
    }

    private void CaptureWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var r)) return;
        var w = r.Right - r.Left;
        var h = r.Bottom - r.Top;
        if (w <= 0 || h <= 0) return;
        var bitmap = ScreenCapture.Capture(r.Left, r.Top, w, h);
        OnSnipCompleted(bitmap, System.Windows.Rect.Empty);
    }

    private void OnSnipCompleted(BitmapSource bitmap, System.Windows.Rect snipScreenRect)
    {
        var preview = new PreviewWindow(bitmap, snipScreenRect);
        preview.Show();
    }
}