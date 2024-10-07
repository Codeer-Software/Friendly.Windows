using System;

namespace Codeer.Friendly.Windows.Inside
{
	/// <summary>
	/// システムコントロール情報。
	/// </summary>
	[Serializable]
    public class SystemControlInfo
	{
        /// <summary>
        /// コントロールタイプ。
        /// </summary>
        public SystemControlType SystemControlType { get; set; }

        /// <summary>
        /// データ。
        /// コントロールタイプによって異なる。
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SystemControlInfo() { }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="systemControlType">コントロールタイプ。</param>
        /// <param name="data">データ。</param>
        public SystemControlInfo(SystemControlType systemControlType, object data)
		{
			SystemControlType = systemControlType;
			Data = data;
		}
	}
}
