using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Codeer.Friendly.Windows.Inside.CopyDataProtocol
{
	/// <summary>
	/// 通信データ。
	/// </summary>
	[Serializable]
	public class CopyDataProtocolInfo
	{
        /// <summary>
        /// 返信ウィンドウハンドル。
        /// </summary>
        public IntPtr ReturnWindowHandle { get; set; }

        /// <summary>
        /// データ。
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CopyDataProtocolInfo() { }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="returnWindowHandle">返信ウィンドウ。</param>
        /// <param name="data">データ。</param>
        internal CopyDataProtocolInfo(IntPtr returnWindowHandle, object data)
		{
			ReturnWindowHandle = returnWindowHandle;
			Data = data;
		}

        /// <summary>
        /// シリアライズ。
        /// </summary>
        /// <returns>バイナリ。</returns>
        internal byte[] Serialize() => SerializeUtility.Serialize(this);

        /// <summary>
        /// デシリアライズ。
        /// </summary>
        /// <param name="bin">バイナリ。</param>
        /// <returns>データ。</returns>
        internal static CopyDataProtocolInfo Deserialize(byte[] bin) => (CopyDataProtocolInfo)SerializeUtility.Deserialize(bin);
    }
}
