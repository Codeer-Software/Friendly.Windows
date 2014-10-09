using System;
using System.Runtime.InteropServices;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// プロトコルメッセージマネージャ
    /// </summary>
    static class ProtocolMessageManager
    {
        /// <summary>
        /// メッセージフィルタ識別子
        /// </summary>
        internal enum ChangeWindowMessageFilterFlags : uint
        {
            /// <summary>
            /// 追加
            /// </summary>
            Add = 1,

            /// <summary>
            /// 削除
            /// </summary>
            Remove = 2
        };

        /// <summary>
        /// メッセージフィルタ型
        /// </summary>
        /// <param name="msg">メッセージ</param>
        /// <param name="flags">フラグ</param>
        /// <returns>成否</returns>
        internal delegate bool ChangeWindowMessageFilter(uint msg, ChangeWindowMessageFilterFlags flags);

        /// <summary>
        /// 初期化
        /// </summary>
        internal static void Initialize()
        {
            IntPtr ptr = NativeMethods.LoadLibrary("user32.dll");
            IntPtr pFunc = NativeMethods.GetProcAddress(ptr, "ChangeWindowMessageFilter");
            if (pFunc == IntPtr.Zero)
            {
                return;
            }
            //対象アプリケーションから受信するメッセージがUACに邪魔されないようにする
            ChangeWindowMessageFilter filter = (ChangeWindowMessageFilter)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(ChangeWindowMessageFilter));
            filter(SystemStarterInApp.WM_NOTIFY_SYSTEM_CONTROL_WINDOW_HANDLE, ChangeWindowMessageFilterFlags.Add);
            filter(NativeMethods.WM_COPYDATA, ChangeWindowMessageFilterFlags.Add);
        }
    }
}
