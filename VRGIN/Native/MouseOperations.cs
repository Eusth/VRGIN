using System;
using System.Runtime.InteropServices;
using static VRGIN.Native.WindowsInterop;

namespace VRGIN.Native
{
    public class MouseOperations
    {

        public static void SetCursorPosition(int X, int Y)
        {
            SetCursorPos(X, Y);
        }

        public static void SetClientCursorPosition(int x, int y)
        {
            var clientRect = WindowManager.GetClientRect();
            SetCursorPos(x + clientRect.Left, y + clientRect.Top);
        }

        public static POINT GetClientCursorPosition()
        {
            var pos = GetCursorPosition();
            var clientRect = WindowManager.GetClientRect();

            return new POINT(pos.X - clientRect.Left, pos.Y - clientRect.Top);
        }

        public static void SetCursorPosition(POINT point)
        {
            SetCursorPos(point.X, point.Y);
        }

        public static POINT GetCursorPosition()
        {
            POINT currentMousePoint;
            var gotPoint = GetCursorPos(out currentMousePoint);
            if (!gotPoint) { currentMousePoint = new POINT(0, 0); }
            return currentMousePoint;
        }


        public static void MouseEvent(MouseEventFlags value)
        {
            POINT position = GetCursorPosition();

            mouse_event
                ((int)value,
                 position.X,
                 position.Y,
                 0,
                 0)
                ;
        }

    }
}