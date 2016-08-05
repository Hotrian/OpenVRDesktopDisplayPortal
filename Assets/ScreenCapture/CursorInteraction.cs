using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class CursorInteraction
{

    [DllImport("user32.dll")]
    static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

    [DllImport("user32.dll")]
    static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

    [DllImport("user32.dll")]
    internal static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern bool SendNotifyMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, IntPtr lParam);

#pragma warning disable 649
    internal struct INPUT
    {
        public UInt32 Type;
        public MOUSEKEYBDHARDWAREINPUT Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct MOUSEKEYBDHARDWAREINPUT
    {
        [FieldOffset(0)]
        public MOUSEINPUT Mouse;
    }

    internal struct MOUSEINPUT
    {
        public Int32 X;
        public Int32 Y;
        public UInt32 MouseData;
        public UInt32 Flags;
        public UInt32 Time;
        public IntPtr ExtraInfo;
    }

#pragma warning restore 649

    // Click a foreground window
    /*public static void ClickOnPointAtCursor(IntPtr wndHandle, Point clientPoint)
    {
        var oldPos = Cursor.Position;

        /// get screen coordinates
        ClientToScreen(wndHandle, ref clientPoint);

        /// set cursor on coords, and press mouse
        Cursor.Position = new Point(clientPoint.X, clientPoint.Y);

        var inputMouseDown = new INPUT();
        inputMouseDown.Type = 0; /// input type mouse
        inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

        var inputMouseUp = new INPUT();
        inputMouseUp.Type = 0; /// input type mouse
        inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up

        var inputs = new INPUT[] { inputMouseDown, inputMouseUp };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

        /// return mouse 
        //Cursor.Position = oldPos;
        var oldPos = Cursor.Position;
        IntPtr lParam = (IntPtr)((clientPoint.Y << 16) | clientPoint.X);
        IntPtr lParam2 = (IntPtr)((clientPoint.Y << 16) | clientPoint.X - 1);
        IntPtr wParam = IntPtr.Zero;
        ClientToScreen(wndHandle, ref clientPoint);
        Cursor.Position = new Point(clientPoint.X, clientPoint.Y);
        SendMessage(wndHandle, (uint)MouseEvents.WM_MOUSEMOVE, wParam, lParam2);
        SendMessage(wndHandle, (uint)MouseEvents.WM_MOUSEMOVE, wParam, lParam);
        //SendMessage(wndHandle, (uint) MouseEvents.WM_LBUTTONDOWN, wParam, lParam);
        //SendMessage(wndHandle, (uint) MouseEvents.WM_LBUTTONUP, wParam, lParam);
        Cursor.Position = oldPos;
    }*/

    private static bool _clicking;

    // Click the cursor where it is
    public static void ClickOnPointAtCursor(IntPtr wndHandle, bool doubleClick = false)
    {
        // Calculate current cursor pos
        var clientPoint = new Point(Cursor.Position.X, Cursor.Position.Y);
        ScreenToClient(wndHandle, ref clientPoint);
        IntPtr lParam = (IntPtr)((clientPoint.Y << 16) | clientPoint.X);
        // Click Mouse
        if (doubleClick)
        {
            SendNotifyMessage(wndHandle, (uint)MouseEvents.WM_LBUTTONDBLCLK, (UIntPtr)1, lParam);
            SendNotifyMessage(wndHandle, (uint)MouseEvents.WM_LBUTTONUP, UIntPtr.Zero, lParam);
        }
        else
        {
            SendNotifyMessage(wndHandle, (uint)MouseEvents.WM_LBUTTONDOWN, (UIntPtr)1, lParam);
            SendNotifyMessage(wndHandle, (uint)MouseEvents.WM_LBUTTONUP, UIntPtr.Zero, lParam);
        }
        _clicking = true;
    }

    // Click on a window even if it's in the background
    public static void ClickOnPoint(IntPtr wndHandle, Point clientPoint, bool doubleClick = false)
    {
        UnityEngine.Debug.Log(clientPoint.X + " / " + clientPoint.Y);
        var oldPos = Cursor.Position;
        IntPtr lParam = (IntPtr)((clientPoint.Y << 16) | clientPoint.X);
        ClientToScreen(wndHandle, ref clientPoint);
        Cursor.Position = new Point(clientPoint.X, clientPoint.Y);
        if (doubleClick)
        {
            SendMessage(wndHandle, (uint)MouseEvents.WM_LBUTTONDBLCLK, (IntPtr)1, lParam);
            SendMessage(wndHandle, (uint)MouseEvents.WM_LBUTTONUP, IntPtr.Zero, lParam);
        }
        else
        {
            SendMessage(wndHandle, (uint)MouseEvents.WM_LBUTTONDOWN, (IntPtr)1, lParam);
            SendMessage(wndHandle, (uint)MouseEvents.WM_LBUTTONUP, IntPtr.Zero, lParam);
        }
        Cursor.Position = oldPos;
    }
    // Release click - not currently used
    public static void ReleaseClick(IntPtr wndHandle)
    {
        var clientPoint = new Point(Cursor.Position.X, Cursor.Position.Y);
        ScreenToClient(wndHandle, ref clientPoint);
        SendNotifyMessage(wndHandle, (uint)MouseEvents.WM_LBUTTONUP, UIntPtr.Zero, (IntPtr)((clientPoint.Y << 16) | clientPoint.X));
        _clicking = false;
    }

    private static IntPtr lastHandle;
    private static Point lastPoint;
    // Move the cursor to a given point over a window
    public static void MoveOverWindow(IntPtr wndHandle, Point clientPoint)
    {
        if (lastPoint.X == clientPoint.X && lastPoint.Y == clientPoint.Y &&
            lastHandle != IntPtr.Zero && lastHandle == wndHandle) return;
        lastHandle = wndHandle;
        lastPoint = clientPoint;
        IntPtr lParam = (IntPtr)((clientPoint.Y << 16) | clientPoint.X);
        ClientToScreen(wndHandle, ref clientPoint);
        Cursor.Position = new Point(clientPoint.X, clientPoint.Y);
        SendNotifyMessage(wndHandle, (uint)MouseEvents.WM_MOUSEMOVE, _clicking ? (UIntPtr)1 : UIntPtr.Zero, lParam);
    }

    private enum MouseEvents
    {
        WM_MOUSEMOVE = 0x200,
        WM_LBUTTONDOWN = 0x201,
        WM_LBUTTONUP = 0x202,
        WM_LBUTTONDBLCLK = 0x203,
        WM_RBUTTONDOWN = 0x204,
        WM_RBUTTONUP = 0x205,
        WM_RBUTTONDBLCLK = 0x206,
        WM_MBUTTONDOWN = 0x207,
        WM_MBUTTONUP = 0x208,
        WM_MBUTTONDBLCLK = 0x209,
        WM_MOUSEWHEEL = 0x20A,
        WM_XBUTTONDOWN = 0x20B,
        WM_XBUTTONUP = 0x20C,
        WM_XBUTTONDBLCLK = 0x20D,
        WM_MOUSEHWHEEL = 0x20E,
    }
}