using System;
using System.Collections.Generic;
using Codeer.Friendly;
using Codeer.Friendly.DotNetExecutor;

namespace Codeer.Friendly.Windows.Inside.CopyDataProtocol
{
	/// <summary>
	/// 送信後の返信受信。
	/// </summary>
	class ReceiveAfterSend : ReceiveForm
	{
		Dictionary<int, object> _recieveData = new Dictionary<int,object>();
        UniqueNoManager _uniqueNoManager = new UniqueNoManager();

        /// <summary>
        /// 通信番号管理用。
        /// </summary>
        internal UniqueNoManager UniqueNoManager { get { return _uniqueNoManager; } }

		/// <summary>
		/// データ受信時の処理。
		/// </summary>
        /// <param name="communicationNo">通信番号。</param>
        /// <param name="recieveData">受信データ。</param>
        /// <param name="senderWindow">送信元ウィンドウ。</param>
		protected override void OnRecieveData(int communicationNo, object recieveData, IntPtr senderWindow)
		{
            _recieveData.Remove(communicationNo);
			_recieveData.Add(communicationNo, recieveData);
		}

        /// <summary>
        /// 受信データ取得。
        /// </summary>
        /// <param name="communicationNo">通信番号。</param>
        /// <param name="receieveData">受信データ。</param>
        /// <returns>成否。</returns>
        internal bool GetReceiveData(int communicationNo, out object receieveData)
        {
            if (!_recieveData.TryGetValue(communicationNo, out receieveData))
            {
                return false;
            }
            _recieveData.Remove(communicationNo);
            return true;
        }
	}

}
