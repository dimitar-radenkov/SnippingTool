using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SnippingTool.ViewModels;
using Forms = System.Windows.Forms;

namespace SnippingTool;

public partial class OverlayWindow
{
    private const int WmNcHitTest = 0x0084;
    private const int HtTransparent = -1;

    private IntPtr OverlayWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmNcHitTest || !_isRecordingOverlayMode)
        {
            return IntPtr.Zero;
        }

        var x = (short)(lParam.ToInt32() & 0xFFFF);
        var y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);
        var screenPoint = new Point(x, y);
        if (IsPointInsideRecordingHud(screenPoint)
            || IsPointInsideRecordingAnnotationCanvas(screenPoint))
        {
            return IntPtr.Zero;
        }

        handled = true;
        return new IntPtr(HtTransparent);
    }

    private bool IsPointInsideRecordingHud(Point screenPoint)
    {
        if (RecordingHudPanel.Visibility != Visibility.Visible
            || RecordingHudPanel.ActualWidth <= 0
            || RecordingHudPanel.ActualHeight <= 0)
        {
            return false;
        }

        var overlayPoint = PointFromScreen(screenPoint);
        var hudBounds = new Rect(
            Canvas.GetLeft(RecordingHudPanel),
            Canvas.GetTop(RecordingHudPanel),
            RecordingHudPanel.ActualWidth,
            RecordingHudPanel.ActualHeight);
        return hudBounds.Contains(overlayPoint);
    }

    private bool IsPointInsideRecordingAnnotationCanvas(Point screenPoint)
    {
        if (RecordingAnnotationCanvas.Visibility != Visibility.Visible
            || !_recordingAnnotationViewModel.IsInputArmed
            || RecordingAnnotationCanvas.ActualWidth <= 0
            || RecordingAnnotationCanvas.ActualHeight <= 0)
        {
            return false;
        }

        var overlayPoint = PointFromScreen(screenPoint);
        var canvasBounds = new Rect(
            Canvas.GetLeft(RecordingAnnotationCanvas),
            Canvas.GetTop(RecordingAnnotationCanvas),
            RecordingAnnotationCanvas.ActualWidth,
            RecordingAnnotationCanvas.ActualHeight);
        return canvasBounds.Contains(overlayPoint);
    }

    private void ShowRecordingHud(Rect regionRect, RecordingHudViewModel hudViewModel)
    {
        HideRecordingHud();
        _recordingHudRegionRect = regionRect;
        _recordingHudViewModel = hudViewModel;
        _recordingHudViewModel.CloseRequested += OnRecordingHudCloseRequested;
        _recordingHudViewModel.StartElapsedTimer();
        RecordingHudPanel.DataContext = hudViewModel;
        RecordingHudPanel.Visibility = Visibility.Visible;
        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(PositionRecordingHud));
    }

    private void HideRecordingHud()
    {
        if (_recordingHudViewModel is not null)
        {
            _recordingHudViewModel.CloseRequested -= OnRecordingHudCloseRequested;
            _recordingHudViewModel.CancelElapsedTimer();
            _recordingHudViewModel = null;
        }

        RecordingHudPanel.DataContext = null;
        RecordingHudPanel.Visibility = Visibility.Collapsed;
    }

    private void OnRecordingHudCloseRequested()
    {
        Dispatcher.Invoke(() =>
        {
            CloseRecordingSessionWindows();
            Close();
        });
    }

    private void RecordingHudPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (RecordingHudPanel.Visibility == Visibility.Visible)
        {
            PositionRecordingHud();
        }
    }

    private void PositionRecordingHud()
    {
        if (RecordingHudPanel.Visibility != Visibility.Visible
            || RecordingHudPanel.ActualWidth <= 0
            || RecordingHudPanel.ActualHeight <= 0)
        {
            return;
        }

        var workArea = GetOverlayWorkAreaForRegion(_recordingHudRegionRect);
        var (left, top) = ComputeRecordingHudPosition(
            _recordingHudRegionRect,
            RecordingHudPanel.ActualWidth,
            RecordingHudPanel.ActualHeight,
            workArea,
            _userSettings.Current.HudGapPixels);
        Canvas.SetLeft(RecordingHudPanel, left);
        Canvas.SetTop(RecordingHudPanel, top);
    }

    internal static (double Left, double Top) ComputeRecordingHudPosition(
        Rect region,
        double hudWidth,
        double hudHeight,
        Rect workArea,
        int gapPixels = 8)
    {
        var left = Math.Max(workArea.Left, Math.Min(region.Left + ((region.Width - hudWidth) / 2d), workArea.Right - hudWidth));
        var top = Math.Min(region.Bottom + gapPixels, workArea.Bottom - hudHeight);
        return (left, top);
    }

    private Rect GetOverlayWorkAreaForRegion(Rect regionRect)
    {
        var topLeftScreen = PointToScreen(new Point(regionRect.Left, regionRect.Top));
        var bottomRightScreen = PointToScreen(new Point(regionRect.Right, regionRect.Bottom));
        var screenBounds = new System.Drawing.Rectangle(
            (int)Math.Floor(topLeftScreen.X),
            (int)Math.Floor(topLeftScreen.Y),
            Math.Max(1, (int)Math.Ceiling(bottomRightScreen.X - topLeftScreen.X)),
            Math.Max(1, (int)Math.Ceiling(bottomRightScreen.Y - topLeftScreen.Y)));
        var workArea = Forms.Screen.FromRectangle(screenBounds).WorkingArea;
        var topLeft = PointFromScreen(new Point(workArea.Left, workArea.Top));
        var bottomRight = PointFromScreen(new Point(workArea.Right, workArea.Bottom));
        return new Rect(topLeft, bottomRight);
    }

    private void RecordingSavedText_Click(object sender, MouseButtonEventArgs e)
    {
        _recordingHudViewModel?.OpenOutputFolderCommand.Execute(null);
    }

    private void RecordingToolButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.RadioButton { Tag: string tag }
            && _recordingHudViewModel?.SelectToolCommand.CanExecute(tag) == true)
        {
            _recordingHudViewModel.SelectToolCommand.Execute(tag);
            RecordingAnnotationCanvas.Cursor = GetRecordingAnnotationCursor();
        }
    }
}
