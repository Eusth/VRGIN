using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using VRGIN.Core;
using static VRGIN.Native.WindowsInterop;

namespace VRGIN.Native
{
    public class WindowManager
    {
        private static List<IntPtr> GetRootWindowsOfProcess(int pid)
        {
            List<IntPtr> rootWindows = GetChildWindows(IntPtr.Zero);
            List<IntPtr> dsProcRootWindows = new List<IntPtr>();
            foreach (IntPtr hWnd in rootWindows)
            {
                uint lpdwProcessId;
                WindowsInterop.GetWindowThreadProcessId(hWnd, out lpdwProcessId);
                if (lpdwProcessId == pid)
                    dsProcRootWindows.Add(hWnd);
            }
            return dsProcRootWindows;
        }

        private static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                WindowsInterop.Win32Callback childProc = new WindowsInterop.Win32Callback(EnumWindow);
                WindowsInterop.EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            //  You can modify this to check to see if you want to cancel the operation, then return a null here
            return true;
        }

        private static IntPtr? _Handle;
        public static IntPtr Handle
        {
            get
            {
                if(_Handle == null)
                {
                    int currentWidth = 0;
                    RECT rect = new RECT();
                    var name = Process.GetCurrentProcess().ProcessName;
                    var handles = GetRootWindowsOfProcess(Process.GetCurrentProcess().Id);
                    foreach (var handle in handles)
                    {
                        if(GetWindowRect(handle, ref rect) && (rect.Right - rect.Left) > currentWidth)
                        {
                            currentWidth = rect.Right - rect.Left;
                            _Handle = handle;
                        }
                    }

                    if(!_Handle.HasValue)
                    {
                        VRLog.Warn("Fall back to first handle!");
                        _Handle = handles.First();
                    }

                }
                return _Handle.Value;
            }
        }
        
        public static string GetWindowText(IntPtr hWnd)
        {
            // Allocate correct string length first
            int length = WindowsInterop.GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            WindowsInterop.GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
        public static void ConfineCursor()
        {
            var clientRect = GetClientRect();
            ClipCursor(ref clientRect);
        }

        public static RECT GetClientRect()
        {
            RECT clientRect = new RECT();
            WindowsInterop.GetClientRect(Handle, ref clientRect);

            POINT topLeft = new POINT();
            ClientToScreen(Handle, ref topLeft);

            clientRect.Left = topLeft.X;
            clientRect.Top = topLeft.Y;
            clientRect.Right += topLeft.X;
            clientRect.Bottom += topLeft.Y;

            return clientRect;
        }

        public static void Activate()
        {
            SetForegroundWindow(Handle);
        }
    }
}
