using System;
using System.Drawing;
using System.Runtime.InteropServices;
using UnityEngine;
using Graphics = System.Drawing.Graphics;

namespace ScreenCapture
{
    class CaptureScreen
    {
        //This structure shall be used to keep the size of the screen.
        public struct SIZE
        {
            public int cx;
            public int cy;
        }

        public static Bitmap CaptureDesktop(int x = 0, int y = 0, int wid = -1, int hei = -1)
        {
            SIZE size;
            IntPtr hBitmap;
            IntPtr hDC = Win32Stuff.GetDC(Win32Stuff.GetDesktopWindow());
            IntPtr hMemDC = GDIStuff.CreateCompatibleDC(hDC);

            size.cx = wid == -1 ? Win32Stuff.GetSystemMetrics(Win32Stuff.SM_CXSCREEN) : wid;

            size.cy = hei == -1 ? Win32Stuff.GetSystemMetrics(Win32Stuff.SM_CYSCREEN) : hei;

            hBitmap = GDIStuff.CreateCompatibleBitmap(hDC, size.cx, size.cy);

            if (hBitmap != IntPtr.Zero)
            {
                IntPtr hOld = (IntPtr)GDIStuff.SelectObject(hMemDC, hBitmap);

                GDIStuff.BitBlt(hMemDC, 0, 0, size.cx, size.cy, hDC, x, y, GDIStuff.SRCCOPY);

                GDIStuff.SelectObject(hMemDC, hOld);
                GDIStuff.DeleteDC(hMemDC);
                Win32Stuff.ReleaseDC(Win32Stuff.GetDesktopWindow(), hDC);
                Bitmap bmp = System.Drawing.Image.FromHbitmap(hBitmap);
                GDIStuff.DeleteObject(hBitmap);
                GC.Collect();
                return bmp;
            }
            return null;
        }

        private static IntPtr _curWnd;
        private static IntPtr _curhBitmap;
        private static IntPtr _curhDC;
        private static IntPtr _curhMemDC;
        private static int _lastWidth;
        private static int _lastHeight;
        public static Bitmap CaptureWindow(IntPtr wnd, WindowSettings settings, out SIZE size, out Win32Stuff.WINDOWINFO info)
        {
            if (_curWnd != wnd)
            {
                _curWnd = wnd;
                DeleteCopyContexts();
            }
            IntPtr hBitmap;
            IntPtr hDC = Win32Stuff.GetDC(Win32Stuff.GetDesktopWindow());
            if (hDC == IntPtr.Zero)
            {
                DeleteCopyContexts();
                info = new Win32Stuff.WINDOWINFO();
                size.cx = 0;
                size.cy = 0;
                return null;
            }
            _curhDC = hDC;
            IntPtr hMemDC;
            if (_curhMemDC == IntPtr.Zero)
            {
                hMemDC = GDIStuff.CreateCompatibleDC(hDC);
                _curhMemDC = hMemDC;
            }
            else
            {
                hMemDC = _curhMemDC;
            }

            info = new Win32Stuff.WINDOWINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            Win32Stuff.GetWindowInfo(wnd, ref info);
            size.cx = Math.Max(1, info.rcClient.Width + settings.offsetRight - settings.offsetLeft);// Win32Stuff.GetSystemMetrics(Win32Stuff.SM_CXSCREEN);
            size.cy = Math.Max(1, info.rcClient.Height + settings.offsetBottom - settings.offsetTop);// Win32Stuff.GetSystemMetrics(Win32Stuff.SM_CYSCREEN

            if (_curhBitmap == IntPtr.Zero || _lastWidth != size.cx || _lastHeight != size.cy)
            {
                hBitmap = GDIStuff.CreateCompatibleBitmap(hDC, size.cx, size.cy);
                _curhBitmap = hBitmap;
                _lastWidth = size.cx;
                _lastHeight = size.cy;
            }
            else
            {
                hBitmap = _curhBitmap;
            }

            if (hBitmap != IntPtr.Zero)
            {
                IntPtr hOld = (IntPtr)GDIStuff.SelectObject(hMemDC, hBitmap);
                GDIStuff.BitBlt(hMemDC, 0, 0, size.cx, size.cy, hDC, info.rcWindow.X + settings.offsetLeft, info.rcWindow.Y + settings.offsetTop, GDIStuff.SRCCOPY);
                GDIStuff.SelectObject(hMemDC, hOld);
                Bitmap bmp = System.Drawing.Image.FromHbitmap(hBitmap);
                return bmp;
            }
            return null;
        }

        public static Bitmap CaptureWindowDirect(IntPtr wnd, WindowSettings settings, out SIZE size, out Win32Stuff.WINDOWINFO info)
        {
            if (_curWnd != wnd)
            {
                _curWnd = wnd;
                DeleteCopyContexts();
            }
            IntPtr hBitmap;
            IntPtr hDC = Win32Stuff.GetDC(wnd);
            if (hDC == IntPtr.Zero)
            {
                DeleteCopyContexts();
                info = new Win32Stuff.WINDOWINFO();
                size.cx = 0;
                size.cy = 0;
                return null;
            }
            _curhDC = hDC;
            IntPtr hMemDC;
            if (_curhMemDC == IntPtr.Zero)
            {
                hMemDC = GDIStuff.CreateCompatibleDC(hDC);
                _curhMemDC = hMemDC;
            }
            else
            {
                hMemDC = _curhMemDC;
            }

            info = new Win32Stuff.WINDOWINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            Win32Stuff.GetWindowInfo(wnd, ref info);

            size.cx = Math.Max(1, info.rcClient.Width + settings.offsetRight - settings.offsetLeft);// Win32Stuff.GetSystemMetrics(Win32Stuff.SM_CXSCREEN);
            size.cy = Math.Max(1, info.rcClient.Height + settings.offsetBottom - settings.offsetTop);// Win32Stuff.GetSystemMetrics(Win32Stuff.SM_CYSCREEN

            if (_curhBitmap == IntPtr.Zero || _lastWidth != size.cx || _lastHeight != size.cy)
            {
                hBitmap = GDIStuff.CreateCompatibleBitmap(hDC, size.cx, size.cy);
                _curhBitmap = hBitmap;
                _lastWidth = size.cx;
                _lastHeight = size.cy;
            }
            else
            {
                hBitmap = _curhBitmap;
            }

            if (hBitmap != IntPtr.Zero)
            {
                IntPtr hOld = (IntPtr)GDIStuff.SelectObject(_curhMemDC, _curhBitmap);
                GDIStuff.BitBlt(hMemDC, 0, 0, size.cx, size.cy, hDC, settings.offsetLeft, settings.offsetTop, GDIStuff.SRCCOPY);
                GDIStuff.SelectObject(_curhMemDC, hOld);
                Bitmap bmp = System.Drawing.Image.FromHbitmap(hBitmap);
                return bmp;
            }
            return null;
        }

        public static void DeleteCopyContexts()
        {
            if (_curhDC == IntPtr.Zero || _curhBitmap == IntPtr.Zero || _curhMemDC == IntPtr.Zero) return;
            GDIStuff.DeleteDC(_curhMemDC);
            Win32Stuff.ReleaseDC(Win32Stuff.GetDesktopWindow(), _curhDC);
            GDIStuff.DeleteObject(_curhBitmap);
            GC.Collect();
            _curhBitmap = IntPtr.Zero;
            _curhDC = IntPtr.Zero;
            _curhMemDC = IntPtr.Zero;
        }

        public static RECT GetWindowRect(IntPtr wnd)
        {
            RECT r = new RECT();
            Win32Stuff.GetWindowRect(wnd, ref r);
            return r;
        }

        public static bool SetWindowRect(IntPtr wnd, RECT r, bool Top)
        {
            return Win32Stuff.SetWindowPos(wnd,
                (IntPtr)(Top ? SpecialWindowHandles.HWND_TOP : SpecialWindowHandles.HWND_BOTTOM),
                r.Left,
                r.Top,
                r.Width,
                r.Height,
                Top ? (SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOOWNERZORDER)
                    : (SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOOWNERZORDER | SetWindowPosFlags.SWP_NOACTIVATE));
        }


        static Bitmap CaptureCursor(ref int x, ref int y)
        {
            Bitmap bmp;
            IntPtr hicon;
            Win32Stuff.CURSORINFO ci = new Win32Stuff.CURSORINFO();
            Win32Stuff.ICONINFO icInfo;
            ci.cbSize = Marshal.SizeOf(ci);
            if (Win32Stuff.GetCursorInfo(out ci))
            {
                if (ci.flags == Win32Stuff.CURSOR_SHOWING)
                {
                    UnityEngine.Debug.Log(ci.hCursor);
                    hicon = Win32Stuff.CopyIcon(ci.hCursor);
                    if (Win32Stuff.GetIconInfo(hicon, out icInfo))
                    {
                        x = ci.ptScreenPos.x - ((int)icInfo.xHotspot);
                        y = ci.ptScreenPos.y - ((int)icInfo.yHotspot);
                        UnityEngine.Debug.Log(hicon);
                        Icon ic = Icon.FromHandle(hicon);
                        bmp = ic.ToBitmap(); 
                        return bmp;
                    }
                }
            }

            return null;
        }

        public static Bitmap CaptureDesktopWithCursor()
        {
            int cursorX = 0;
            int cursorY = 0;
            Bitmap desktopBMP;
            Bitmap cursorBMP;

            desktopBMP = CaptureDesktop();
            cursorBMP = CaptureCursor(ref cursorX, ref cursorY);
            if (desktopBMP != null)
            {
                if (cursorBMP != null)
                {
                    Rectangle r = new Rectangle(cursorX, cursorY, cursorBMP.Width, cursorBMP.Height);
                    Graphics g = Graphics.FromImage(desktopBMP);
                    g.DrawImage(cursorBMP, r);
                    g.Flush();
                }
                return desktopBMP;
            }
            return null;
        }
    }
}
