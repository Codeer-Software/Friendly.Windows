using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// 指定のウィンドウで処理を実行させる。
    /// </summary>
    class TargetWindowExecutor
    {
        /// <summary>
        /// 実行。
        /// </summary>
        /// <param name="targetThreadWindowHandle">処理を実行させるウィンドウ。</param>
        /// <param name="action">処理。</param>
        /// <returns>成否。</returns>
        internal static bool Execute(IntPtr targetThreadWindowHandle, MethodInvoker action)
        {
            //現在のウィンドウプロックを取得
            IntPtr currentProc = NativeMethods.GetWindowLongPtr(targetThreadWindowHandle, NativeMethods.GWL_WNDPROC);
            //InvokeWindowを起動するためのプロックを設定
            bool executed = false;
            NativeMethods.WndProc proc = delegate(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
            {
                switch (msg)
                {
                    case 0:
                        if (!executed)
                        {
                            action();
                            executed = true;
                        }
                        break;
                    default:
                        break;
                }
                return NativeMethods.CallWindowProc(currentProc, hwnd, msg, wParam, lParam);
            };
            NativeMethods.SetWindowLongPtr(targetThreadWindowHandle, NativeMethods.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(proc));

            //実行完了待ち
            while (!executed)
            {
                //指定のウィンドウが消えていたら終了
                if (!NativeMethods.IsWindow(targetThreadWindowHandle))
                {
                    return false;
                }
                NativeMethods.SendMessage(targetThreadWindowHandle, 0, IntPtr.Zero, IntPtr.Zero);
                Thread.Sleep(10);
            }
            GC.KeepAlive(proc);
            GC.KeepAlive(action);

            //元に戻す
            NativeMethods.SetWindowLongPtr(targetThreadWindowHandle, NativeMethods.GWL_WNDPROC, currentProc);
            return true;
        }
    }
}
