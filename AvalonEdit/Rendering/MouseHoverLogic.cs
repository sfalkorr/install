using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace AvalonEdit.Rendering;

public class MouseHoverLogic : IDisposable
{
    private UIElement target;

    private DispatcherTimer mouseHoverTimer;
    private Point           mouseHoverStartPoint;
    private MouseEventArgs  mouseHoverLastEventArgs;
    private bool            mouseHovering;

    public MouseHoverLogic(UIElement target)
    {
        this.target            =  target ?? throw new ArgumentNullException(nameof(target));
        this.target.MouseLeave += MouseHoverLogicMouseLeave;
        this.target.MouseMove  += MouseHoverLogicMouseMove;
        this.target.MouseEnter += MouseHoverLogicMouseEnter;
    }

    private void MouseHoverLogicMouseMove(object sender, MouseEventArgs e)
    {
        var mouseMovement = mouseHoverStartPoint - e.GetPosition(target);
        if (Math.Abs(mouseMovement.X) > SystemParameters.MouseHoverWidth || Math.Abs(mouseMovement.Y) > SystemParameters.MouseHoverHeight) StartHovering(e);
    }

    private void MouseHoverLogicMouseEnter(object sender, MouseEventArgs e)
    {
        StartHovering(e);
    }

    private void StartHovering(MouseEventArgs e)
    {
        StopHovering();
        mouseHoverStartPoint    = e.GetPosition(target);
        mouseHoverLastEventArgs = e;
        mouseHoverTimer         = new DispatcherTimer(SystemParameters.MouseHoverTime, DispatcherPriority.Background, OnMouseHoverTimerElapsed, target.Dispatcher);
        mouseHoverTimer.Start();
    }

    private void MouseHoverLogicMouseLeave(object sender, MouseEventArgs e)
    {
        StopHovering();
    }

    private void StopHovering()
    {
        if (mouseHoverTimer != null)
        {
            mouseHoverTimer.Stop();
            mouseHoverTimer = null;
        }

        if (mouseHovering)
        {
            mouseHovering = false;
            OnMouseHoverStopped(mouseHoverLastEventArgs);
        }
    }

    private void OnMouseHoverTimerElapsed(object sender, EventArgs e)
    {
        mouseHoverTimer.Stop();
        mouseHoverTimer = null;

        mouseHovering = true;
        OnMouseHover(mouseHoverLastEventArgs);
    }

    public event EventHandler<MouseEventArgs> MouseHover;

    protected virtual void OnMouseHover(MouseEventArgs e)
    {
        if (MouseHover != null) MouseHover(this, e);
    }

    public event EventHandler<MouseEventArgs> MouseHoverStopped;

    protected virtual void OnMouseHoverStopped(MouseEventArgs e)
    {
        if (MouseHoverStopped != null) MouseHoverStopped(this, e);
    }

    private bool disposed;

    public void Dispose()
    {
        if (!disposed)
        {
            target.MouseLeave -= MouseHoverLogicMouseLeave;
            target.MouseMove  -= MouseHoverLogicMouseMove;
            target.MouseEnter -= MouseHoverLogicMouseEnter;
        }

        disposed = true;
    }
}