#define CODE_ANALYSIS
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using Codeer.Friendly.Inside.Protocol;
using Codeer.Friendly.DotNetExecutor;
using Codeer.Friendly.Windows.Inside.CopyDataProtocol;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace Codeer.Friendly.Windows.Inside
{
	/// <summary>
	/// アプリケーション側システムコントロールウィンドウ。
	/// </summary>
	class SystemControlWindowInApp : ReceiveForm
	{
        FriendlyConnectorWindowInAppManager _handleAndWindow = new FriendlyConnectorWindowInAppManager();
        DotNetFriendlyControl _dotNetFriendlyControl = new DotNetFriendlyControl();

        /// <summary>
        /// データ受信時の処理。
        /// </summary>
        /// <param name="communicationNo">通信番号。</param>
        /// <param name="recieveData">受信データ。</param>
        /// <param name="senderWindow">送信元ウィンドウ。</param>
        protected override void OnRecieveData(int communicationNo, object recieveData, IntPtr senderWindow)
        {
            SystemControlInfo controlInfo = (SystemControlInfo)recieveData;
			object ret = null;
			switch (controlInfo.SystemControlType)
			{
                case SystemControlType.StartFriendlyConnectorWindowInApp:
                    ret = StartFriendlyConnectorWindowInApp(controlInfo);
                    break;
				case SystemControlType.EndFriendlyConnectorWindowInApp:
                    EndFriendlyConnectorWindowInApp(controlInfo);
					break;
				case SystemControlType.EndSystem:
                    EndSystem();
					break;
			}
            SendReturnData(communicationNo, senderWindow, ret);
		}

        /// <summary>
        /// フレンドリー操作ウィンドウ開始。
        /// </summary>
        /// <param name="controlInfo">コントロール情報。</param>
        /// <returns>FriendlyConnectorWindowInAppのハンドル。</returns>
        IntPtr StartFriendlyConnectorWindowInApp(SystemControlInfo controlInfo)
        {
            IntPtr targetThreadWindowHandle = (IntPtr)controlInfo.Data;
            FriendlyConnectorWindowInApp window = null;
            IntPtr executeWindowHandle = IntPtr.Zero;
            if (!TargetWindowExecutor.Execute(targetThreadWindowHandle, delegate
            {
                window = new FriendlyConnectorWindowInApp(_handleAndWindow, _dotNetFriendlyControl);
                executeWindowHandle = window.Handle;
            }))
            {
                return IntPtr.Zero;
            }

            //登録
            _handleAndWindow.Add(executeWindowHandle, window);
            return executeWindowHandle;
        }

        /// <summary>
        /// フレンドリー操作ウィンドウ終了。
        /// </summary>
        /// <param name="controlInfo">コントロール情報。</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        void EndFriendlyConnectorWindowInApp(SystemControlInfo controlInfo)
        {
            IntPtr handle = (IntPtr)controlInfo.Data;
            FriendlyConnectorWindowInApp invokeWindow = _handleAndWindow.FromHandle(handle);
            _handleAndWindow.Remove(handle);

            //スレッドが異なるので、Invokeによって終了処理を実施する。
            if (invokeWindow != null)
            {
                try
                {
                    invokeWindow.Invoke((MethodInvoker)delegate
                    {
                        try
                        {
                            invokeWindow.RequestDispose();
                        }
                        catch { }
                    });
                }
                catch { }
            }
        }

        /// <summary>
        /// システム終了
        /// </summary>
        void EndSystem()
        {
            //自身のスレッドを終了させる
            NativeMethods.PostMessage(Handle, NativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// 破棄
        /// </summary>
        /// <param name="disposing">Disposeメソッドから呼び出されたか</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (_handleAndWindow != null)
                {
                    //ウィンドウ全終了
                    foreach (KeyValuePair<IntPtr, FriendlyConnectorWindowInApp> element in _handleAndWindow.Clone())
                    {
                        //何らかの都合で終了に失敗するものが出ても消せるだけ消しておく
                        //スレッドが異なるので、Invokeによって終了処理を実施する
                        try
                        {
                            element.Value.Invoke((MethodInvoker)delegate
                            {
                                try
                                {
                                    element.Value.RequestDispose();
                                }
                                catch { }
                            });
                        }
                        catch { }
                    }
                    _handleAndWindow = null;
                }
                _dotNetFriendlyControl = null;
            }
            catch { }
            base.Dispose(disposing);
        }
    }
}
