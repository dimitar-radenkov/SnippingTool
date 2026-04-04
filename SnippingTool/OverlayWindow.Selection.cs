using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SnippingTool.ViewModels;

namespace SnippingTool;

public partial class OverlayWindow
{
    private const int LoupeSize = 120;
    private const int LoupeZoom = 4;
    private const int LoupeOffset = 20;

    private void Root_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_vm.CurrentPhase != OverlayViewModel.Phase.Selecting)
        {
            return;
        }

        var start = e.GetPosition(Root);
        Root.Tag = start;
        SelectionBorder.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionBorder, start.X);
        Canvas.SetTop(SelectionBorder, start.Y);
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;
        Root.CaptureMouse();
    }

    private void Root_MouseMove(object sender, MouseEventArgs e)
    {
        if (_vm.CurrentPhase != OverlayViewModel.Phase.Selecting
            || Root.Tag is not Point start)
        {
            return;
        }

        var cur = e.GetPosition(Root);
        var x = Math.Min(cur.X, start.X);
        var y = Math.Min(cur.Y, start.Y);
        var w = Math.Abs(cur.X - start.X);
        var h = Math.Abs(cur.Y - start.Y);

        Canvas.SetLeft(SelectionBorder, x);
        Canvas.SetTop(SelectionBorder, y);
        SelectionBorder.Width = w;
        SelectionBorder.Height = h;

        _vm.UpdateSizeLabel(w, h);
        SizeLabelText.Text = _vm.SizeLabel;
        SizeLabelBorder.Visibility = Visibility.Visible;
        SizeLabelBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var ly = y - SizeLabelBorder.DesiredSize.Height - 4;
        if (ly < 0)
        {
            ly = y + 4;
        }

        Canvas.SetLeft(SizeLabelBorder, x);
        Canvas.SetTop(SizeLabelBorder, ly);

        UpdateLoupe(cur);
    }

    private void Root_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_vm.CurrentPhase != OverlayViewModel.Phase.Selecting
            || Root.Tag is not Point start)
        {
            return;
        }

        LoupeBorder.Visibility = Visibility.Collapsed;
        Root.Tag = null;
        Root.ReleaseMouseCapture();

        var end = e.GetPosition(Root);
        var x = Math.Min(end.X, start.X);
        var y = Math.Min(end.Y, start.Y);
        var w = Math.Abs(end.X - start.X);
        var h = Math.Abs(end.Y - start.Y);

        if (w < 4 || h < 4)
        {
            Close();
            return;
        }

        _vm.CommitSelection(new Rect(x, y, w, h));
        TransitionToAnnotating();
    }

    private void UpdateLoupe(Point cursor)
    {
        if (_screenSnapshot is null)
        {
            return;
        }

        var srcSize = LoupeSize / LoupeZoom;
        var px = (int)(cursor.X * _vm.DpiX) - srcSize / 2;
        var py = (int)(cursor.Y * _vm.DpiY) - srcSize / 2;
        var snapW = _screenSnapshot.PixelWidth;
        var snapH = _screenSnapshot.PixelHeight;
        px = Math.Clamp(px, 0, Math.Max(0, snapW - srcSize));
        py = Math.Clamp(py, 0, Math.Max(0, snapH - srcSize));
        var actualW = Math.Min(srcSize, snapW - px);
        var actualH = Math.Min(srcSize, snapH - py);

        if (actualW <= 0 || actualH <= 0)
        {
            LoupeBorder.Visibility = Visibility.Collapsed;
            return;
        }

        LoupeImage.Source = new CroppedBitmap(_screenSnapshot, new Int32Rect(px, py, actualW, actualH));
        LoupeBorder.Visibility = Visibility.Visible;

        var lx = cursor.X + LoupeOffset;
        var ly = cursor.Y + LoupeOffset;
        if (lx + LoupeSize > Width)
        {
            lx = cursor.X - LoupeSize - LoupeOffset;
        }

        if (ly + LoupeSize > Height)
        {
            ly = cursor.Y - LoupeSize - LoupeOffset;
        }

        Canvas.SetLeft(LoupeBorder, lx);
        Canvas.SetTop(LoupeBorder, ly);
    }

    private void TransitionToAnnotating()
    {
        var sel = _vm.SelectionRect;
        var screenX = (int)((Left + sel.X) * _vm.DpiX);
        var screenY = (int)((Top + sel.Y) * _vm.DpiY);
        var screenW = Math.Max(1, (int)(sel.Width * _vm.DpiX));
        var screenH = Math.Max(1, (int)(sel.Height * _vm.DpiY));
        Visibility = Visibility.Hidden;
        System.Threading.Thread.Sleep(60);
        var backgroundCapture = _screenCapture.Capture(screenX, screenY, screenW, screenH);
        Visibility = Visibility.Visible;
        _screenSnapshot = null;

        EnterAnnotatingSession(sel, backgroundCapture, _vm.DpiX, _vm.DpiY, allowRecording: true);
    }
}
