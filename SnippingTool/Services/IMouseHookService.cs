namespace SnippingTool.Services;

public enum MouseHookButton
{
    Left,
    Right,
    Middle,
    X1,
    X2,
}

public sealed class MouseHookEventArgs : EventArgs
{
    public MouseHookEventArgs(MouseHookButton button, Point screenPoint)
    {
        Button = button;
        ScreenPoint = screenPoint;
    }

    public MouseHookButton Button { get; }

    public Point ScreenPoint { get; }
}

public interface IMouseHookService
{
    event EventHandler<MouseHookEventArgs>? MouseButtonDown;

    void Start();

    void Stop();
}
