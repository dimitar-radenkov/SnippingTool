using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;

namespace SnippingTool;

public partial class RecordingBorderWindow : Window
{
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int cx, int cy, bool repaint);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    private readonly int _leftPx;
    private readonly int _topPx;
    private readonly int _widthPx;
    private readonly int _heightPx;
    private readonly ILogger<RecordingBorderWindow> _logger;

    public RecordingBorderWindow(
        int leftPx,
        int topPx,
        int widthPx,
        int heightPx,
        ILogger<RecordingBorderWindow> logger)
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.Manual;
        _leftPx = leftPx;
        _topPx = topPx;
        _widthPx = widthPx;
        _heightPx = heightPx;
        _logger = logger;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;

        // Make the window completely click-through so the user can interact
        // with whatever is underneath the recording region.
        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED);

        _logger.LogDebug(
            "RecordingBorderWindow.OnSourceInitialized: requested physical px L={L} T={T} W={W} H={H}",
            _leftPx, _topPx, _widthPx, _heightPx);
        _logger.LogDebug(
            "RecordingBorderWindow.OnSourceInitialized: WPF DIP before MoveWindow Left={Left} Top={Top} Width={Width} Height={Height}",
            Left, Top, Width, Height);

        // First call only: nudge one pixel to trigger WM_DPICHANGED so WPF loads the
        // correct DPI context for the target monitor. The definitive placement is done
        // in Loaded (after WPF's first Measure/Arrange pass which can override positions
        // set here) — see ApplyPhysicalPlacement().
        MoveWindow(hwnd, _leftPx + 1, _topPx, _widthPx - 1, _heightPx, false);

        _logger.LogDebug(
            "RecordingBorderWindow.OnSourceInitialized: WPF DIP after first MoveWindow Left={Left} Top={Top} Width={Width} Height={Height}",
            Left, Top, Width, Height);

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        ApplyPhysicalPlacement();
    }

    private void ApplyPhysicalPlacement()
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        _logger.LogDebug(
            "RecordingBorderWindow.ApplyPhysicalPlacement: WPF DIP before final MoveWindow Left={Left} Top={Top} Width={Width} Height={Height}",
            Left, Top, Width, Height);

        // Definitive placement after WPF's layout pass. Physical pixels bypass the
        // PerMonitorV2 Window.Left/Top DIP re-scaling bug (dotnet/wpf#4127).
        MoveWindow(hwnd, _leftPx, _topPx, _widthPx, _heightPx, true);

        _logger.LogDebug(
            "RecordingBorderWindow.ApplyPhysicalPlacement: WPF DIP after final MoveWindow Left={Left} Top={Top} Width={Width} Height={Height}",
            Left, Top, Width, Height);
    }
}
