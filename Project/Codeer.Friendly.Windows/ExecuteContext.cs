using System;
using Codeer.Friendly.Windows.Inside;
using Codeer.Friendly.Windows.Inside.CopyDataProtocol;
using Codeer.Friendly.Inside.Protocol;
using System.Windows.Forms;

namespace Codeer.Friendly.Windows
{
#if ENG
    /// <summary>
    /// Used for changing the executing process thread in the target application.
    /// </summary>
#else
    /// <summary>
    /// 実行コンテキスト。
    /// テスト対象アプリケーションでの処理実行スレッドを変更するのに使用します。
    /// </summary>
#endif
    public class ExecuteContext : IDisposable
    {
        readonly FriendlyConnectorCore _friendlyConnector;
        SystemController _systemController;

        /// <summary>
        /// 接続者。
        /// </summary>
        internal FriendlyConnectorCore FriendlyConnector { get { return _friendlyConnector; } }

#if ENG

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="app">Application manipulation object.</param>
        /// <param name="executeThreadWindowHandle">Window handle in the thread where test operations will be carried out.</param>
#else

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="app">アプリケーション操作クラス。</param>
        /// <param name="executeThreadWindowHandle">処理を実行させるスレッドで動作するウィンドウのハンドルです。</param>
#endif
        public ExecuteContext(WindowsAppFriend app, IntPtr executeThreadWindowHandle)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            _systemController = app.SystemController;
            _friendlyConnector = _systemController.StartFriendlyConnector(executeThreadWindowHandle);
        }

#if ENG
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="app">Application manipulation object.</param>
        /// <param name="executeThreadWindowControl">AppVar for a .Net window object in the thread where test operations will be carried out.</param>
#else
        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="app">アプリケーション操作クラス。</param>
        /// <param name="executeThreadWindowControl">処理を実行させるスレッドで動作するウィンドウの.Netオブジェクトの入ったアプリケーション内変数です。</param>
#endif
        public ExecuteContext(WindowsAppFriend app, AppVar executeThreadWindowControl)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            _systemController = app.SystemController;
            IntPtr handle = (IntPtr)app[typeof(ExecuteContext), "GetHandleThreadSafe"](executeThreadWindowControl).Core;
            _friendlyConnector = _systemController.StartFriendlyConnector(handle);
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="friendlyConnector">接続者。</param>
        internal ExecuteContext(FriendlyConnectorCore friendlyConnector)
        {
            _friendlyConnector = friendlyConnector;
        }

		/// <summary>
        /// ファイナライザ。
		/// </summary>
        ~ExecuteContext()
		{
			Dispose(false);
		}

#if ENG
        /// <summary>
        /// Disposes this object.
        /// This context cannot be used after this method is called.
        /// </summary>
#else
        /// <summary>
        /// 破棄します。
        /// このメソッドが呼び出されると、このコンテキストを使用して処理を実行させることができなくなります。
		/// </summary>
#endif
        public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

#if ENG
        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">flag.</param>
#else
        /// <summary>
        /// 破棄。
		/// </summary>
        /// <param name="disposing">破棄フラグ。</param>
#endif
        protected virtual void Dispose(bool disposing)
        {
            if (_systemController == null)
            {
                return;
            }
            _systemController.EndFriendlyConnector(_friendlyConnector.FriendlyConnectorWindowInAppHandle);
            _systemController = null;
            GC.Collect();
        }

        /// <summary>
        /// ハンドルの取得
        /// App内部から使用される
        /// </summary>
        /// <param name="control">取得対象コントロール</param>
        /// <returns>ハンドル</returns>
        static IntPtr GetHandleThreadSafe(Control control)
        {
            if (control == null)
            {
                return IntPtr.Zero;
            }
            IntPtr ptr = IntPtr.Zero;
            control.Invoke((MethodInvoker)delegate
            {
                ptr = control.Handle;
            });
            return ptr;
        }
    }
}
