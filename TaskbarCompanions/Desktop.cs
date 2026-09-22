using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace TaskbarCompanions;

internal static class Desktop
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
    public static void SetApplicationIdentity() => SetCurrentProcessExplicitAppUserModelID("TaskbarCompanions.Desktop");
    [DllImport("user32.dll")] private static extern IntPtr GetThreadDesktop(uint threadId);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetUserObjectInformation(IntPtr handle, int index, StringBuilder value, int length, out int needed);
    public static string InstanceScope()
    {
        var name = new StringBuilder(256);
        return GetUserObjectInformation(GetThreadDesktop(GetCurrentThreadId()), 2, name, name.Capacity * 2, out _) ? name.ToString() : "Default";
    }
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder name, int count);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out Rect rect, int size);

    public static bool ForegroundIsFullscreen()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero || IsIconic(foreground)) return false;
        var name = new StringBuilder(256);
        GetClassName(foreground, name, name.Capacity);
        if (name.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") return false;
        var monitor = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(foreground, 2), ref monitor)) return false;
        if (DwmGetWindowAttribute(foreground, 9, out var bounds, Marshal.SizeOf<Rect>()) != 0 && !GetWindowRect(foreground, out bounds)) return false;
        return Covers(bounds, monitor.Monitor);
    }

    internal static bool Covers(Rect r, Rect m) => r.Left <= m.Left + 1 && r.Top <= m.Top + 1 && r.Right >= m.Right - 1 && r.Bottom >= m.Bottom - 1;

    public static void Dock(Window window, int homeIndex, bool reset)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var screen = reset ? System.Windows.Forms.Screen.PrimaryScreen! : System.Windows.Forms.Screen.FromHandle(handle);
        var area = screen.WorkingArea;
        var source = PresentationSource.FromVisual(window);
        var scale = source?.CompositionTarget?.TransformToDevice.M11 ?? 1;
        var width = (int)(window.Width * scale);
        var height = (int)(window.Height * scale);
        GetWindowRect(handle, out var current);
        var x = reset ? area.Right - (homeIndex + 1) * (width + 16) - 12 : current.Left;
        x = Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - width));
        // Move without touching z-order (SWP_NOZORDER): re-asserting HWND_TOPMOST on every poll
        // lifted the window above its own tooltips and menus. WPF's Topmost keeps it in the topmost band.
        if (current.Left != x || current.Top != area.Bottom - height)
            SetWindowPos(handle, IntPtr.Zero, x, area.Bottom - height, 0, 0, 0x0001 | 0x0004 | 0x0010);
    }
}
