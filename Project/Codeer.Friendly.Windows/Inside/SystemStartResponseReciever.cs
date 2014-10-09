using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// システム開始レスポンス受信。
    /// </summary>
    class SystemStartResponseReciever
    {
        Thread _recieveThread;
        IntPtr _systemControlWindowInAppHandle;
        long _errNo;

        /// <summary>
        /// 開始。
        /// </summary>
        /// <param name="targetProcessId">対象プロセスID。</param>
        /// <returns>レスポンス受信用ウィンドウハンドル。</returns>
        internal IntPtr Start(int targetProcessId)
        {
            IntPtr recieveWindowHandle = IntPtr.Zero;
            object sync = new object();
            _recieveThread = new Thread((ThreadStart)delegate
            {
                using (RecieveWindow window = new RecieveWindow())
                {
                    lock (sync)
                    {
                        recieveWindowHandle = window.Handle;
                    }

                    //受信待ち
                    while (!window.IsError)
                    {
                        NativeMethods.MSG msg = new NativeMethods.MSG();
                        if (NativeMethods.PeekMessage(ref msg, window.Handle, 0, 0, NativeMethods.PeekMsgOption.PM_REMOVE))
                        {
                            NativeMethods.TranslateMessage(ref msg);
                            NativeMethods.DispatchMessage(ref msg);
                        }
                        if (window.SystemControlWindowHandle != IntPtr.Zero)
                        {
                            lock (sync)
                            {
                                _systemControlWindowInAppHandle = window.SystemControlWindowHandle;
                            }
                            break;
                        }
                        Thread.Sleep(10);

                        //通信プロセスが消えたら終わり
                        try
                        {
                            if (Process.GetProcessById(targetProcessId) == null)
                            {
                                break;
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                    lock (sync)
                    {
                        _errNo = window.ErrNo;
                    }
                }
            });
            _recieveThread.Start();

            //受信ウィンドウ生成待ち。
            while (true)
            {
                lock (sync)
                {
                    if (recieveWindowHandle != IntPtr.Zero)
                    {
                        break;
                    }
                }
            }
            return recieveWindowHandle;
        }

        /// <summary>
        /// 受信スレッドの終了待ち（返信待ち）。
        /// </summary>
        /// <param name="errNo">エラー番号。</param>
        /// <returns>対象プロセス内のコントロール用ウィンドウのハンドル。</returns>
        internal IntPtr WaitForCompletion(ref long errNo)
        {
            while (_recieveThread.IsAlive)
            {
                Thread.Sleep(10);
            }
            errNo = _errNo;
            return _systemControlWindowInAppHandle;
        }

        /// <summary>
        /// システムコントロールウィンドウハンドル受信。
        /// </summary>
        class RecieveWindow : CommunicationWindow
        {
            IntPtr _systemControlWindowHandle;
            bool _isError;
            long _errNo;

            /// <summary>
            /// システムコントロールウィンドウハンドル。
            /// </summary>
            internal IntPtr SystemControlWindowHandle { get { return _systemControlWindowHandle; } }

            /// <summary>
            /// エラー終了したか。
            /// </summary>
            internal bool IsError { get { return _isError; } }

            /// <summary>
            /// エラー番号。
            /// </summary>
            internal long ErrNo { get { return _errNo; } }

            /// <summary>
            /// コンストラクタ。
            /// </summary>
            internal RecieveWindow()
            {
                CreateHandle();
            }

            /// <summary>
            /// ウィンドウプロック。
            /// </summary>
            /// <param name="m">メッセージ。</param>
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == SystemStarterInApp.WM_NOTIFY_SYSTEM_CONTROL_WINDOW_HANDLE)
                {
                    _systemControlWindowHandle = m.WParam;
                    if (_systemControlWindowHandle == IntPtr.Zero)
                    {
                        _isError = true;
                        _errNo = m.LParam.ToInt64();
                    }
                    return;
                }
                base.WndProc(ref m);
            }
        }
    }
}
