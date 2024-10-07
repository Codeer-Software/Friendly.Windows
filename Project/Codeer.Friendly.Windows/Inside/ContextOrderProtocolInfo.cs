using System;
using Codeer.Friendly.Inside.Protocol;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// 実行コンテキスト指定プロトコル情報
    /// </summary>
    [Serializable]
    public class ContextOrderProtocolInfo
    {
        /// <summary>
        /// プロトコル情報
        /// </summary>
        public ProtocolInfo ProtocolInfo { get; set; }

        /// <summary>
        /// 実行ウィンドウ
        /// </summary>
        public IntPtr ExecuteWindowHandle { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ContextOrderProtocolInfo() { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="protocolInfo">プロトコル情報</param>
        /// <param name="executeWindowHandle">実行ウィンドウハンドル</param>
        public ContextOrderProtocolInfo(ProtocolInfo protocolInfo, IntPtr executeWindowHandle)
        {
            ProtocolInfo = protocolInfo;
            ExecuteWindowHandle = executeWindowHandle;
        }
    }
}
