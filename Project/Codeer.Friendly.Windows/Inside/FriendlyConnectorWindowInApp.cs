#define CODE_ANALYSIS
using System;
using Codeer.Friendly;
using Codeer.Friendly.Inside.Protocol;
using Codeer.Friendly.DotNetExecutor;
using Codeer.Friendly.Windows.Inside.CopyDataProtocol;
using System.Windows.Forms;
using System.Diagnostics.CodeAnalysis;

namespace Codeer.Friendly.Windows.Inside
{
	/// <summary>
	/// アプリケーション側フレンドリー操作ウィンドウ。
	/// </summary>
    class FriendlyConnectorWindowInApp : ReceiveForm, IAsyncInvoke
	{
        FriendlyConnectorWindowInAppManager _manager;
        DotNetFriendlyControl _dotNetFriendlyControl;
        int _invokeCount;
        bool _isCloseMode;

        /// <summary>
        /// BinOff高速通信用。
        /// </summary>
        internal const int WM_BINOFF = 0x8000;

        /// <summary>
        /// BinOff成功戻り値。
        /// </summary>
        static readonly IntPtr SuccessBinOff = new IntPtr(1);

		/// <summary>
        /// コンストラクタ。
		/// </summary>
        /// <param name="manager">ウィンドウ管理</param>
        /// <param name="dotNetFriendlyControl">.Net側処理呼び出しクラス。</param>
        internal FriendlyConnectorWindowInApp(FriendlyConnectorWindowInAppManager manager, DotNetFriendlyControl dotNetFriendlyControl)
		{
            _manager = manager;
            _dotNetFriendlyControl = dotNetFriendlyControl;
		}

        /// <summary>
        /// 終了要求。
        /// </summary>
        internal void RequestDispose()
        {
            _isCloseMode = true;
            if (_invokeCount == 0)
            {
                DisposeCore();
            }
        }

        /// <summary>
        /// 非同期実行。
        /// </summary>
        /// <param name="method">非同期メソッド。</param>
        public void Execute(AsyncMethod method)
        {
            _invokeCount++;
            this.BeginInvoke((MethodInvoker)delegate
            {
                method();
                _invokeCount--;
                if (_isCloseMode && _invokeCount == 0)
                {
                    DisposeCore();
                }
            });
        }

        /// <summary>
        /// データ受信時の処理。
        /// </summary>
        /// <param name="communicationNo">通信番号。</param>
        /// <param name="recieveData">受信データ。</param>
        /// <param name="senderWindow">送信元ウィンドウ。</param>
        protected override void OnRecieveData(int communicationNo, object recieveData, IntPtr senderWindow)
        {
            ContextOrderProtocolInfo contextOrder = recieveData as ContextOrderProtocolInfo;
            if (contextOrder != null)
            {
                FriendlyConnectorWindowInApp async = _manager.FromHandle(contextOrder.ExecuteWindowHandle);
                SendReturnData(communicationNo, senderWindow, _dotNetFriendlyControl.Execute(async, contextOrder.ProtocolInfo));
            }
            else
            {
                ProtocolInfo protocolInfo = recieveData as ProtocolInfo;
                SendReturnData(communicationNo, senderWindow, _dotNetFriendlyControl.Execute(this, protocolInfo));
            }
        }

        /// <summary>
        /// メッセージ処理。
        /// </summary>
        /// <param name="message">メッセージ。</param>
        protected override void WndProc(ref Message message)
        {
            //BinOff高速処理用
            if (message.Msg == WM_BINOFF)
            {
                _dotNetFriendlyControl.Execute(this, new ProtocolInfo(ProtocolType.BinOff, null, new VarAddress(message.WParam.ToInt32()), string.Empty, string.Empty, new string[0]));
                message.Result = SuccessBinOff;
                return;
            }
            base.WndProc(ref message);
        }

        /// <summary>
        /// 破棄
        /// </summary>
        private void DisposeCore()
        {
            Dispose();
            _manager = null;
            _dotNetFriendlyControl = null;
            GC.Collect();
        }
    }
}
