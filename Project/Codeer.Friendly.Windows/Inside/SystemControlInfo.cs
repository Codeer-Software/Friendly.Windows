using System;

namespace Codeer.Friendly.Windows.Inside
{
	/// <summary>
	/// システムコントロール情報。
	/// </summary>
	[Serializable]
	class SystemControlInfo
	{
		SystemControlType _systemControlType;
		object _data;

		/// <summary>
		/// コントロールタイプ。
		/// </summary>
		internal SystemControlType SystemControlType { get { return _systemControlType; } }
		
		/// <summary>
		/// データ。
		/// コントロールタイプによって異なる。
		/// </summary>
		internal object Data { get { return _data; } }

		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="systemControlType">コントロールタイプ。</param>
		/// <param name="data">データ。</param>
		internal SystemControlInfo(SystemControlType systemControlType, object data)
		{
			_systemControlType = systemControlType;
			_data = data;
		}
	}
}
