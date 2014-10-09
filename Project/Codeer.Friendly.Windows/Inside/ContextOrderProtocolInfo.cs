using System;
using Codeer.Friendly.Inside.Protocol;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// 実行コンテキスト指定プロトコル情報
    /// </summary>
    [Serializable]
    class ContextOrderProtocolInfo
    {
        ProtocolInfo _protocolInfo;
        IntPtr _executeWindowHandle;

        /// <summary>
        /// プロトコル情報
        /// </summary>
        internal ProtocolInfo ProtocolInfo { get { return _protocolInfo; } }

        /// <summary>
        /// 実行ウィンドウ
        /// </summary>
        internal IntPtr ExecuteWindowHandle { get { return _executeWindowHandle; } }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="protocolInfo">プロトコル情報</param>
        /// <param name="executeWindowHandle">実行ウィンドウハンドル</param>
        internal ContextOrderProtocolInfo(ProtocolInfo protocolInfo, IntPtr executeWindowHandle)
        {
            _protocolInfo = protocolInfo;
            _executeWindowHandle = executeWindowHandle;
        }
    }
}
