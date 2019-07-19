using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using Codeer.Friendly.Windows.Inside;
using Codeer.Friendly;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Codeer.Friendly.Windows.Inside
{
	/// <summary>
	/// Windowsアプリケーション操作開始クラス。
	/// </summary>
	public static class SystemStarterInApp
	{
        /// <summary>
        /// ウィンドウハンドル通知メッセージ。
        /// </summary>
        internal const int WM_NOTIFY_SYSTEM_CONTROL_WINDOW_HANDLE = 0x8100;

        /// <summary>
        /// 開始。
        /// </summary>
        /// <param name="startInfo">開始情報。</param>
        public static int Start(string startInfo)
        {
            startInfo = Debug.ReadDebugMark(startInfo);
            Debug.Trace("Start in App.");

            //端末でアプリケーションコントロールウィンドウのハンドルを待っているウィンドウのハンドル
            IntPtr terminalWindow = new IntPtr(long.Parse(startInfo, CultureInfo.CurrentCulture));
            Thread t = new Thread((ThreadStart)delegate
            {
                StartCore(terminalWindow);
            });
            t.Start();
            return 0;
        }

        /// <summary>
        /// 開始。
        /// </summary>
        /// <param name="terminalWindow">端末ウィンドウ。</param>
        public static void StartCore(IntPtr terminalWindow)
        {
			//コントロールスレッド終了管理スレッド
            //システムに負荷をかけない程度で端末プロセス、コントローススレッド、メインスレッドを監視する
			new Thread((ThreadStart)delegate
			{
                Debug.Trace("Start waiting thread.");

                //コントロールウィンドウのハンドル格納用
                object sync = new object();
                IntPtr controlWindowHandle = IntPtr.Zero;

				//コントロールスレッド起動
                //処理に対して素早く対応するためGetMessageを使用する
				Thread controlThread = new Thread((ThreadStart)delegate
				{
                    using (SystemControlWindowInApp window = new SystemControlWindowInApp())
                    {
                        NativeMethods.MSG msg = new NativeMethods.MSG();
                        lock (sync)
                        {
                            controlWindowHandle = window.Handle;
                        }
                        while (true)
                        {
                            if (NativeMethods.GetMessage(ref msg, window.Handle, 0, 0) == 0)
                            {
                                break;
                            }
                            NativeMethods.TranslateMessage(ref msg);
                            NativeMethods.DispatchMessage(ref msg);
                        }
                    }
				});
				controlThread.Start();

				//ウィンドウハンドル生成待ち
				while (true)
				{
					lock (sync)
					{
						if (controlWindowHandle != IntPtr.Zero)
						{
							break;
						}
					}
                    Thread.Sleep(10);
				}

                Debug.Trace("Control Window Created.");

                //対象プロセスのIDを取得
                int windowProcessId = 0;
                NativeMethods.GetWindowThreadProcessId(terminalWindow, out windowProcessId);

                //端末側にコントロールウィンドウのハンドルを送信する
                NativeMethods.SendMessage(terminalWindow, WM_NOTIFY_SYSTEM_CONTROL_WINDOW_HANDLE, controlWindowHandle, IntPtr.Zero);

                //終了待ち
                EventHandler appExit = new EventHandler(delegate { NativeMethods.PostMessage(controlWindowHandle, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero); });
                Application.ApplicationExit += appExit;
                Debug.Trace("Success in App.");
                Process windowProcess = null;
                try
                {
                    windowProcess = Process.GetProcessById(windowProcessId);
                }
                catch {}

                while (windowProcess != null)
                {
                    //通信プロセスが消えたら終わり
                    windowProcess.Refresh();
                    if (windowProcess.HasExited)
                    {
                        break;
                    }

                    //コントロールスレッドが終了したら終わり
                    if (!controlThread.IsAlive)
                    {
                        break;
                    }

					Thread.Sleep(200);
				}

				//コントロールスレッド終了
				while (controlThread.IsAlive)
				{
					NativeMethods.PostMessage(controlWindowHandle, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
					Thread.Sleep(10);
				}

                Application.ApplicationExit -= appExit;

                //メモリ解放
                GC.Collect();
            }).Start();
		}
	}
}
