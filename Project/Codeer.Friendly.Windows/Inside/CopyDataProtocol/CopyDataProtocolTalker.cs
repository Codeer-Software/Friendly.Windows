using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Codeer.Friendly;
using Codeer.Friendly.Inside.Protocol;
using Codeer.Friendly.Inside;

namespace Codeer.Friendly.Windows.Inside.CopyDataProtocol
{
	/// <summary>
	/// Windowメッセージ通信。
	/// </summary>
	static class CopyDataProtocolTalker
	{
		/// <summary>
		/// 送受信。
		/// </summary>
		/// <param name="targetWindowHandle">送信対象ウィンドウハンドル。</param>
		/// <param name="data">送信データ。</param>
		/// <returns>受信データ。</returns>
		internal static object SendAndRecieve(IntPtr targetWindowHandle, object data)
		{
			using(ReceiveAfterSend recieveWindow = new ReceiveAfterSend())
            {
				return SendAndRecieve(targetWindowHandle, data, recieveWindow);
			}
		}

		/// <summary>
		/// 送受信。
		/// </summary>
		/// <param name="targetWindowHandle">送信対象ウィンドウハンドル。</param>
		/// <param name="data">送信データ。</param>
		/// <param name="recieveWindow">受信ウィンドウ。</param>
		/// <returns>受信データ。</returns>
		internal static object SendAndRecieve(IntPtr targetWindowHandle, object data, ReceiveAfterSend recieveWindow)
		{
            //通信番号生成
            int communicationNo = 0;
            if (!recieveWindow.UniqueNoManager.CreateNo(out communicationNo))
            {
                throw new InformationException(ResourcesLocal.Instance.OutOfCommunicationNo);
            }

            //使用可能なスレッドであるかチェック
            if (!recieveWindow.CanUseThread)
            {
                throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorInvalidThreadCall);
            }

            //送受信
            IntPtr globalData = IntPtr.Zero;
            try
			{
				//通信データ作成
				CopyDataProtocolInfo communicationData = new CopyDataProtocolInfo(recieveWindow.Handle, data);
				
				//シリアライズ
				byte[] bin = communicationData.Serialize();

				//WM_COPYDATAでの送信用構造体に移し替え
				NativeMethods.COPYDATASTRUCT copyData = new NativeMethods.COPYDATASTRUCT();
				copyData.dwData = IntPtr.Zero;
				copyData.cbData = (uint)bin.Length;
				copyData.lpData = globalData = Marshal.AllocHGlobal(bin.Length);
				Marshal.Copy(bin, 0, copyData.lpData, bin.Length);

				//送信
				IntPtr sendRet = NativeMethods.SendMessage(targetWindowHandle, NativeMethods.WM_COPYDATA, new IntPtr(communicationNo), ref copyData);
				if (sendRet != ReceiveForm.SendCopyDataSuccess)
				{
                    throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorAppCommunication);
				}

				//受信データ取得
                object receiveData;
                if (!recieveWindow.GetReceiveData(communicationNo, out receiveData))
                {
                    throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorAppCommunication);
                }
                return receiveData;
			}
			finally
			{
				//グローバルメモリ解放
				if (globalData != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(globalData);
				}
                recieveWindow.UniqueNoManager.FreeNo(communicationNo);
			}
		}
	}
}
