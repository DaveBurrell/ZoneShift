using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

[DllImport("user32.dll")]
static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

[DllImport("user32.dll")]
static extern bool SetForegroundWindow(IntPtr hWnd);

[DllImport("user32.dll")]
static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

var path = args.Length > 0 ? args[0] : "shot.png";
var name = args.Length > 1 ? args[1] : "ZoneShift";

var proc = Process.GetProcessesByName(name)
    .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);

if (proc is null)
{
    Console.WriteLine("NO_WINDOW");
    return 1;
}

ShowWindow(proc.MainWindowHandle, 9); // SW_RESTORE
SetForegroundWindow(proc.MainWindowHandle);
Thread.Sleep(500);

if (!GetWindowRect(proc.MainWindowHandle, out var r))
{
    Console.WriteLine("NO_RECT");
    return 2;
}

var w = r.Right - r.Left;
var h = r.Bottom - r.Top;
if (w < 20 || h < 20)
{
    Console.WriteLine($"BAD_SIZE {w}x{h}");
    return 3;
}

using var bmp = new Bitmap(w, h);
using (var g = Graphics.FromImage(bmp))
{
    g.CopyFromScreen(r.Left, r.Top, 0, 0, new Size(w, h));
}

bmp.Save(path, ImageFormat.Png);
Console.WriteLine($"SAVED {path} {w}x{h}");
return 0;

struct RECT
{
    public int Left, Top, Right, Bottom;
}
