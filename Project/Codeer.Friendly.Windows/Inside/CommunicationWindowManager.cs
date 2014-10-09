#define CODE_ANALYSIS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics.CodeAnalysis;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// 通信ウィンドウ管理
    /// </summary>
    abstract class CommunicationWindowManager
    {
        const string WindowClassNameBase = "Codeer.Friendly.Windows.Inside.CommunicationWindow_";
        [SuppressMessage("Microsoft.Performance", "CA1802:UseLiteralsWhereAppropriate")]
        internal static readonly string WindowClassName;
        static readonly NativeMethods.WndProc MyWindowProc;
        static Dictionary<IntPtr, CommunicationWindow> _handleAndWindow = new Dictionary<IntPtr, CommunicationWindow>();

        /// <summary>
        /// staticコンストラクタ
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static CommunicationWindowManager()
        {
            string windowClassName = string.Empty;
            for (int i = 0; true; i++)
            {
                windowClassName = WindowClassNameBase + i;

                //ウィンドウプロックをGCの対象から外すためにstaticメンバにする
                MyWindowProc = new NativeMethods.WndProc(WndProc);

                //ウィンドウクラス登録
                NativeMethods.WNDCLASSEX wc = new NativeMethods.WNDCLASSEX();
                wc.cbSize = (uint)Marshal.SizeOf(wc);
                wc.style = 0;
                wc.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(MyWindowProc);
                wc.cbClsExtra = 0;
                wc.cbWndExtra = 0;
                wc.hInstance = NativeMethods.GetModuleHandle(null);
                wc.hIcon = IntPtr.Zero;
                wc.hCursor = IntPtr.Zero;
                wc.hbrBackground = IntPtr.Zero;
                wc.lpszMenuName = string.Empty;
                wc.lpszClassName = windowClassName;
                wc.hIconSm = IntPtr.Zero;
                if (NativeMethods.RegisterClassEx(ref wc) != 0)
                {
                    break;
                }
            }
            WindowClassName = windowClassName;
        }

        /// <summary>
        /// ウィンドウプロック
        /// </summary>
        /// <param name="hwnd">ウィンドウハンドル</param>
        /// <param name="msg">メッセージ</param>
        /// <param name="wParam">WParam</param>
        /// <param name="lParam">LParam</param>
        /// <returns>結果</returns>
        static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            CommunicationWindow w;
            lock (_handleAndWindow)
            {
                if (!_handleAndWindow.TryGetValue(hwnd, out w))
                {
                    return NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
                }
            }
            Message m = new Message();
            m.HWnd = hwnd;
            m.Msg = msg;
            m.WParam = wParam;
            m.LParam = lParam;
            w.CallWndProc(ref m);
            GC.KeepAlive(w);
            return m.Result;
        }

        /// <summary>
        /// 生成
        /// </summary>
        /// <param name="window">ウィンドウ</param>
        /// <returns>生成結果</returns>
        internal static IntPtr Create(CommunicationWindow window)
        {
            //ウィンドウ生成
            IntPtr handle = NativeMethods.CreateWindowEx(0, WindowClassName, string.Empty, 0, 0, 0, 1, 1,
                NativeMethods.HWND_MESSAGE, IntPtr.Zero, NativeMethods.GetModuleHandle(null), IntPtr.Zero);

            //ウィンドウ登録
            lock (_handleAndWindow)
            {
                _handleAndWindow.Add(handle, window);
            }

            //念のため、MyWindowProcが破棄されないように
            GC.KeepAlive(MyWindowProc);
            return handle;
        }

        /// <summary>
        /// ウィンドウの破棄
        /// </summary>
        /// <param name="handle">ハンドル</param>
        internal static void DestroyWindow(IntPtr handle)
        {
            NativeMethods.DestroyWindow(handle);
            lock (_handleAndWindow)
            {
                CommunicationWindow window;
                if (_handleAndWindow.TryGetValue(handle, out window))
                {
                    GC.KeepAlive(_handleAndWindow);
                }
                _handleAndWindow.Remove(handle);
            }

            //念のため、MyWindowProcが破棄されないように
            GC.KeepAlive(MyWindowProc);
        }
    }
}
