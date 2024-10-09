#define CODE_ANALYSIS
using System;
using Codeer.Friendly.Windows.Inside.CopyDataProtocol;
using System.Diagnostics.CodeAnalysis;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// システム制御。
    /// </summary>
    class SystemController
    {
        internal IntPtr SystemControlWindowInAppHandle { get; }
        IntPtr _friendlyConnectorWindowInAppAsync;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="systemControlWindowInAppHandle">システムコントロールウィンドウハンドル。</param>
        internal SystemController(IntPtr systemControlWindowInAppHandle)
        {
            SystemControlWindowInAppHandle = systemControlWindowInAppHandle;

            //コントロールスレッドに非同期通信用を一つ確保する
            _friendlyConnectorWindowInAppAsync = (IntPtr)CopyDataProtocolTalker.SendAndRecieve(SystemControlWindowInAppHandle,
                new SystemControlInfo(SystemControlType.StartFriendlyConnectorWindowInApp, SystemControlWindowInAppHandle));
            if (_friendlyConnectorWindowInAppAsync == IntPtr.Zero)
            {
                throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorExecuteThreadWindowHandle);
            }
        }

        /// <summary>
        /// FriendlyConnector開始。
        /// </summary>
        /// <param name="executeThreadWindowHandle">実行対象スレッドに属するウィンドウハンドル。</param>
        /// <returns>FriendlyConnector。</returns>
        internal FriendlyConnectorCore StartFriendlyConnector(IntPtr executeThreadWindowHandle)
        {
            //通信用ウィンドウを生成
            IntPtr friendlyConnectorWindowInApp = (IntPtr)CopyDataProtocolTalker.SendAndRecieve(SystemControlWindowInAppHandle,
                    new SystemControlInfo(SystemControlType.StartFriendlyConnectorWindowInApp, executeThreadWindowHandle));
            if (friendlyConnectorWindowInApp == IntPtr.Zero)
            {
                throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorExecuteThreadWindowHandle);
            }
            return new FriendlyConnectorCore(_friendlyConnectorWindowInAppAsync, friendlyConnectorWindowInApp);
        }

        /// <summary>
        /// FriendlyConnector終了。
        /// </summary>
        /// <param name="friendlyConnectorWindowInApp">アプリケーション内部のFriendlyConnectorWindowのハンドル。</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal void EndFriendlyConnector(IntPtr friendlyConnectorWindowInApp)
        {
            //タイミングによっては相手アプリケーションが存在しないことは十分にありうる
            try
            {
                CopyDataProtocolTalker.SendAndRecieve(SystemControlWindowInAppHandle,
                    new SystemControlInfo(SystemControlType.EndFriendlyConnectorWindowInApp, friendlyConnectorWindowInApp));
            }
            catch { }
        }

        /// <summary>
        /// システム終了。
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal void EndSystem()
        {
            //フタイミングによっては相手アプリケーションが存在しないことは十分にありうる
            try
            {
                CopyDataProtocolTalker.SendAndRecieve(SystemControlWindowInAppHandle,
                    new SystemControlInfo(SystemControlType.EndSystem, null));
            }
            catch { }
        }
    }
}
