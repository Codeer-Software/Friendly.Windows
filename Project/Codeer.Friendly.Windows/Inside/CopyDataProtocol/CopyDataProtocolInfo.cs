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
	class CopyDataProtocolInfo
	{
		IntPtr _returnWindowHandle;
		object _data;

		/// <summary>
		/// 返信ウィンドウハンドル。
		/// </summary>
		internal IntPtr ReturnWindowHandle { get { return _returnWindowHandle; } }

		/// <summary>
		/// データ。
		/// </summary>
		internal object Data { get { return _data; } }

		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="returnWindowHandle">返信ウィンドウ。</param>
		/// <param name="data">データ。</param>
		internal CopyDataProtocolInfo(IntPtr returnWindowHandle, object data)
		{
			_returnWindowHandle = returnWindowHandle;
			_data = data;
		}

		/// <summary>
		/// シリアライズ。
		/// </summary>
		/// <returns>バイナリ。</returns>
		internal byte[] Serialize()
		{
			IFormatter formatter = new BinaryFormatter();
			using (MemoryStream stream = new MemoryStream())
			{
				formatter.Serialize(stream, this);
				return stream.ToArray();
			}
		}

		/// <summary>
		/// デシリアライズ。
		/// </summary>
		/// <param name="bin">バイナリ。</param>
		/// <returns>データ。</returns>
		internal static CopyDataProtocolInfo Deserialize(byte[] bin)
		{
			IFormatter formatter = new BinaryFormatter();
			using (MemoryStream stream = new MemoryStream(bin))
			{
				return (CopyDataProtocolInfo)formatter.Deserialize(stream);
			}
		}
	}
}
