using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Globalization;
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

        static Dictionary<IntPtr, int> _manipulatorPocessChangeDictionary = new Dictionary<IntPtr, int>();

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
                var windowProcess = GetProcessById(windowProcessId);
                bool isDifferentPermissions = false;
                while (windowProcess != null)
                {
                    //通信プロセスが消えたら終わり
                    if (!IsExistProcess(windowProcess, ref isDifferentPermissions))
                    {
                        break;
                    }

                    //コントロールスレッドが終了したら終わり
                    if (!controlThread.IsAlive)
                    {
                        break;
                    }

                    //操作元プロセス変更要求確認
                    windowProcess = CheckChangeManipulatorProcess(controlWindowHandle, windowProcess, ref isDifferentPermissions);

                    Thread.Sleep(200);
                }
                if (windowProcess != null) windowProcess.Dispose();

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

        static bool IsExistProcess(Process windowProcess, ref bool isDifferentPermissions)
        {
            if (!isDifferentPermissions)
            {
                try
                {
                    windowProcess.Refresh();
                    //操作側が管理者権限、対象アプリが通常権限の場合にHasExitedをコールすると例外が発生する
                    //その場合はGetProcessByIdで判断するようにする
                    if (windowProcess.HasExited)
                    {
                        return false;
                    }
                }
                catch { }
            }
            isDifferentPermissions = true;
            var process = GetProcessById(windowProcess.Id);
            if (process == null) return false;
            process.Dispose();
            return true;
        }

        static Process CheckChangeManipulatorProcess(IntPtr controlWindowHandle, Process windowProcess, ref bool isDifferentPermissions)
        {
            lock (_manipulatorPocessChangeDictionary)
            {
                if (_manipulatorPocessChangeDictionary.TryGetValue(controlWindowHandle, out var value))
                {
                    windowProcess.Dispose();
                    windowProcess = GetProcessById(value);
                    _manipulatorPocessChangeDictionary.Remove(controlWindowHandle);
                    isDifferentPermissions = false;
                }
            }

            return windowProcess;
        }

        static void ChangeManipulatorProcess(IntPtr controlWindowHandle, int newProcess)
        {
            lock (_manipulatorPocessChangeDictionary)
            {
                _manipulatorPocessChangeDictionary[controlWindowHandle] = newProcess;
            }
            while (true)
            {
                lock (_manipulatorPocessChangeDictionary)
                {
                    if (!_manipulatorPocessChangeDictionary.ContainsKey(controlWindowHandle)) break;
                }
                Thread.Sleep(1);
            }
        }

        static Process GetProcessById(int id)
        {
            Process process = null;

            try
            {
                process = Process.GetProcessById(id);
            }
            catch { }

            return process;
        }
	}
}
