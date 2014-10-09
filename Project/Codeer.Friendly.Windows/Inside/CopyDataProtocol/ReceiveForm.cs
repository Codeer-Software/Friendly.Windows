#define CODE_ANALYSIS
using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Codeer.Friendly.Windows.Inside.CopyDataProtocol
{
	/// <summary>
	/// データ受信ウィンドウ共通処理。
	/// </summary>
    internal abstract class ReceiveForm : CommunicationWindow
	{
		/// <summary>
		/// WM_COPYDATA送信成功。
		/// </summary>
		internal static readonly IntPtr SendCopyDataSuccess = new IntPtr(1);

        int _managedThreadId = Thread.CurrentThread.ManagedThreadId;

        /// <summary>
        /// 使用可能なスレッドであるか
        /// </summary>
        internal bool CanUseThread { get { return _managedThreadId == Thread.CurrentThread.ManagedThreadId; } }

		/// <summary>
		/// コンストラクタ。
		/// </summary>
		protected ReceiveForm()
		{
            CreateHandle();
		}

		/// <summary>
		/// メッセージ処理。
		/// </summary>
		/// <param name="message">メッセージ。</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		protected override void WndProc(ref Message message)
		{
			CopyDataProtocolInfo data;
            if (message.HWnd == Handle && ProcessCopyData(ref message, out data))
			{
				if (data != null)
				{
					//対象スレッドで実施されるので、仮に予期せぬ例外が発生しても、無視する。
					try
					{
                        OnRecieveData(message.WParam.ToInt32(), data.Data, data.ReturnWindowHandle);
					}
					catch { }
				}
			}
			else
			{
				base.WndProc(ref message);
			}
		}

		/// <summary>
		/// WM_COPYDATAの処理。
		/// </summary>
		/// <param name="message">メッセージ。</param>
		/// <param name="data">受信データ。</param>
		/// <returns>true→処理した。</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static bool ProcessCopyData(ref Message message, out CopyDataProtocolInfo data)
		{
			data = null;
			if (message.Msg == NativeMethods.WM_COPYDATA)
			{
                //デシリアライズ
				//これが失敗するということは通信の状態が正常ではないので、詳細は返さず、ただ、通信に失敗したことだけ通知する
				try
				{
					NativeMethods.COPYDATASTRUCT globalData = (NativeMethods.COPYDATASTRUCT)message.GetLParam(typeof(NativeMethods.COPYDATASTRUCT));
					byte[] bin = new byte[(int)globalData.cbData];
					Marshal.Copy(globalData.lpData, bin, 0, bin.Length);
					data = CopyDataProtocolInfo.Deserialize(bin);
					message.Result = SendCopyDataSuccess;
				}
				catch
				{
					message.Result = IntPtr.Zero;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// 応答送信。
		/// </summary>
        /// <param name="communicationNo">通信番号。</param>
        /// <param name="receiveWindowHandle">応答受信ウィンドウ。</param>
		/// <param name="data">送信データ。</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		protected static void SendReturnData(int communicationNo, IntPtr receiveWindowHandle, object data)
		{
			IntPtr globalData = IntPtr.Zero;
			try
			{
				CopyDataProtocolInfo communicatonData = new CopyDataProtocolInfo(IntPtr.Zero, data);
				byte[] bin = communicatonData.Serialize();
				NativeMethods.COPYDATASTRUCT copyData = new NativeMethods.COPYDATASTRUCT();
				copyData.dwData = IntPtr.Zero;
				copyData.cbData = (uint)bin.Length;
				copyData.lpData = globalData = Marshal.AllocHGlobal(bin.Length);
				Marshal.Copy(bin, 0, copyData.lpData, bin.Length);
				NativeMethods.SendMessage(receiveWindowHandle, NativeMethods.WM_COPYDATA, new IntPtr(communicationNo), ref copyData);
			}
			catch
			{
				//対象アプリケーションプロセスでの実行なので例外は投げない。
			}
			finally
			{
				if (globalData != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(globalData);
				}
			}
		}

        /// <summary>
        /// データ受信時の処理。
        /// </summary>
        /// <param name="communicationNo">通信番号。</param>
        /// <param name="recieveData">受信データ。</param>
        /// <param name="senderWindow">送信元ウィンドウ。</param>
		protected abstract void OnRecieveData(int communicationNo, object recieveData, IntPtr senderWindow);
	}
}
