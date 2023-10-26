using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScreenshotOcr.Helpers;

static class DesktopCaptureHelper
{

    [DllImport("gdi32.dll")]
    static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

    [DllImport("user32.dll")]
    static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern bool BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern bool DeleteDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetWindowDC(IntPtr hWnd);


    public static Bitmap CaptureScreen()
    {
        IntPtr hDesktop = GetDesktopWindow();
        IntPtr hDC = GetWindowDC(hDesktop);

        var monitorInfo = MonitorEnumerationHelper.GetMonitors().First();

        int width = (int)monitorInfo.ScreenSize.X;
        int height = (int)monitorInfo.ScreenSize.Y;
#if (DEBUG)
    Debug.WriteLine($"width x height: {width} x {height}");
#endif

        IntPtr hCaptureDC = CreateCompatibleDC(hDC);
        IntPtr hCaptureBitmap = CreateCompatibleBitmap(hDC, width, height);

        IntPtr hOldBitmap = SelectObject(hCaptureDC, hCaptureBitmap);

        BitBlt(hCaptureDC, 0, 0, width, height, hDC, 0, 0, (int)CopyPixelOperation.SourceCopy);

        Bitmap bmp = System.Drawing.Image.FromHbitmap(hCaptureBitmap);

        SelectObject(hCaptureDC, hOldBitmap);
        DeleteObject(hCaptureBitmap);
        DeleteDC(hCaptureDC);

        ReleaseDC(hDesktop, hDC);

        return bmp;
    }
}