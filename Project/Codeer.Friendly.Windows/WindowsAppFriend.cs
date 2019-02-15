#define CODE_ANALYSIS
using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using Codeer.Friendly.Windows.Inside;
using Codeer.Friendly.Inside.Protocol;
using Codeer.Friendly.Windows.Inside.CopyDataProtocol;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Codeer.Friendly.Windows
{
#if ENG
    /// <summary>
    /// Class that allows manipulating Windows applications.
    /// Inherits from AppFriend.
    /// Can fail to connect depending on the target application's permissions.
    /// </summary>
#else
    /// <summary>
	/// Windowsアプリケーションを操作するためのクラスです。
    /// AppFriendを継承しています。
	/// </summary>
#endif
    public class WindowsAppFriend : AppFriend, IDisposable
	{
        ExecuteContext _context;
        readonly int _processId;
        SystemController _systemController;
        int _appVarCreateCount;
        readonly object _syncAppVarCreateCountUp = new object();
        readonly object _syncCurrentConnector = new object();

#if ENG
        /// <summary>
        /// Connector.
        /// </summary>
#else
        /// <summary>
		/// 接続者。
		/// </summary>
#endif
        protected override IFriendlyConnector FriendlyConnector { get { return new FriendlyConnectorWrap(this); } }

#if ENG
        /// <summary>
        /// Returns the ProcessId of the connected process.
        /// </summary>
#else
        /// <summary>
        /// 操作対象アプリケーションのプロセスIDを取得できます。
		/// </summary>
#endif
        public int ProcessId { get { return _processId; } }

        /// <summary>
        /// システムコントローラー。
        /// </summary>
        internal SystemController SystemController { get { return _systemController; } }

#if ENG
        /// <summary>
        /// Constructor.
        /// Connects to the indicated process.
        /// Operations are carried out in the thread of the window that is the main window at connection time. 
        /// The CLR version used for the target process is determined by examining the version loaded in the target process.
        /// When in multiple CLRs are loaded using process side by side, one of the loaded versions is used. 
        /// Native code does not use the CLR, so the CLR version of the test application is used in this case.
        /// </summary>
        /// <param name="process">Target application process.</param>
#else
        /// <summary>
		/// コンストラクタです。
		/// 指定のプロセスに接続します。
        /// この指定の場合、接続時のメインウィンドウのスレッドで処理が実行されます。
        /// 対象プロセスのCLRのバージョンは対象プロセスにロードされているものを調べて使います。
        /// インプロセスサイドバイサイドで複数CLRが起動している場合はそのどちらかを使います。
        /// ネイティブの場合CLRが動作していないので、操作側の処理が動作するランタイムと同じバージョンを使います。
		/// </summary>
		/// <param name="process">接続対象プロセス。</param>
#endif
        public WindowsAppFriend(Process process) : this(process, string.Empty) { }

#if ENG
        /// <summary>
        /// Constructor.
        /// Connects to the process of the indicated window handle.
        /// Operations are carried out in the thread of the indicated window handle.
        /// The CLR version used for the target process is determined by examining the version loaded in the target process.
        /// When in multiple CLRs are loaded using process side by side, one of the loaded versions is used. 
        /// Native code does not use the CLR, so the CLR version of the test application is used in this case.
        /// </summary>
        /// <param name="executeContextWindowHandle">
        /// Windowshandle that belongs to the target process.
        /// Operations are carried out in the thread of this window. 
        /// </param>
#else
        /// <summary>
        /// コンストラクタです。
        /// 指定のウィンドウハンドルのプロセスに接続します。
        /// また、指定のウィンドウハンドルのスレッドで処理が実行されます。
        /// 対象プロセスのCLRのバージョンは対象プロセスにロードされているものを調べて使います。
        /// インプロセスサイドバイサイドで複数CLRが起動している場合はそのどちらかを使います。
        /// ネイティブの場合CLRが動作していないので、操作側の処理が動作するランタイムと同じバージョンを使います。
        /// </summary>
        /// <param name="executeContextWindowHandle">接続対象プロセスの処理実行スレッドのウィンドウハンドル。</param>
#endif
        public WindowsAppFriend(IntPtr executeContextWindowHandle) : this(executeContextWindowHandle, string.Empty) { }

#if ENG
        /// <summary>
        /// Constructor.
        /// Connects to the indicated process.
        /// Operations are carried out in the thread of the window that is the main window at connection time. 
        /// </summary>
        /// <param name="process">Target application process.</param>
        /// <param name="clrVersion">
        /// CLR version of the target process. "v2.0.50727", "v4.0.30319"
        /// For more information please refer to the Microsoft site.
        /// To ensure backward compatibility, Friendly allows “2.0” for “v2.0.50727” and “4.0” for “v4.0.30319”, but these are now deprecated.
        /// </param>
#else
        /// <summary>
		/// コンストラクタです。
		/// 指定のプロセスに接続します。
        /// この指定の場合、接続時のメインウィンドウのスレッドで処理が実行されます。
		/// </summary>
		/// <param name="process">接続対象プロセス。</param>
        /// <param name="clrVersion">CLRのバージョン "v2.0.50727", "v4.0.30319" のように入力してください。
        /// 詳細はマイクロソフトのサイトを参照お願いします。
        /// Friendlyの過去のバージョンとの互換性のため"v2.0.50727"は"2.0", "v4.0.30319"は"4.0"と入力することも可能ですが、今後これは非推奨となります。
        /// </param>
#endif
        public WindowsAppFriend(Process process, string clrVersion)
		{
            ResourcesLocal.Initialize();
            ProtocolMessageManager.Initialize();

            //アイドル状態になるのを待ちます。
            try
            {
                process.WaitForInputIdle();
            }
            catch
            {
                throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorProcessOperation);
            }

			//メインウィンドウ取得待ち。
			while (process != null && process.MainWindowHandle == IntPtr.Zero)
			{
                try
                {
                    process = Process.GetProcessById(process.Id);
                }
                catch
                {
                    break;
                }
				Thread.Sleep(10);
			}
			if (process == null)
			{
                throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorAppConnection);
			}
			_processId = process.Id;

			//サーバーを開始させる。
            _systemController = SystemStarter.Start(process, clrVersion, process.MainWindowHandle);

            //メインの実行ウィンドウハンドル生成。
            _context = new ExecuteContext(_systemController.StartFriendlyConnector(process.MainWindowHandle));

            //リソース初期化
            ResourcesLocal.Install(this);
		}

#if ENG
        /// <summary>
        /// Constructor.
        /// Connects to the process of the indicated window handle.
        /// Operations are carried out in the thread of the indicated window handle.
        /// </summary>
        /// <param name="executeContextWindowHandle">
        /// Windowshandle that belongs to the target process.
        /// Operations are carried out in the thread of this window. 
        /// </param>
        /// <param name="clrVersion">
        /// CLR version of the target process. "v2.0.50727", "v4.0.30319"
        /// For more information please refer to the Microsoft site.
        /// To ensure backward compatibility, Friendly allows “2.0” for “v2.0.50727” and “4.0” for “v4.0.30319”, but these are now deprecated.
        /// </param>
#else
        /// <summary>
        /// コンストラクタです。
        /// 指定のウィンドウハンドルのプロセスに接続します。
        /// また、指定のウィンドウハンドルのスレッドで処理が実行されます。
        /// </summary>
        /// <param name="executeContextWindowHandle">接続対象プロセスの処理実行スレッドのウィンドウハンドル。</param>
        /// <param name="clrVersion">CLRのバージョン "v2.0.50727", "v4.0.30319" のように入力してください。
        /// 詳細はマイクロソフトのサイトを参照お願いします。
        /// Friendlyの過去のバージョンとの互換性のため"v2.0.50727"は"2.0", "v4.0.30319"は"4.0"と入力することも可能ですが、今後これは非推奨となります。
        /// </param>
#endif
        public WindowsAppFriend(IntPtr executeContextWindowHandle, string clrVersion)
        {
            ResourcesLocal.Initialize();
            ProtocolMessageManager.Initialize();

            //プロセスの取得
            NativeMethods.GetWindowThreadProcessId(executeContextWindowHandle, out _processId);

            //サーバーを開始させる。
            _systemController = SystemStarter.Start(Process.GetProcessById(_processId), clrVersion, executeContextWindowHandle);

            //メインの実行ウィンドウハンドル生成。
            _context = new ExecuteContext(_systemController.StartFriendlyConnector(executeContextWindowHandle));

            //リソース初期化
            ResourcesLocal.Install(this);
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="controller">システムコントローラ</param>
        /// <param name="executeContextWindowHandle">処理実行するスレッドに属するウィンドウハンドル</param>
        /// <param name="processId">プロセスId</param>
        WindowsAppFriend(SystemController controller, IntPtr executeContextWindowHandle, int processId)
        {
            _systemController = controller;
            _processId = processId;
            _context = new ExecuteContext(_systemController.StartFriendlyConnector(executeContextWindowHandle));
            ResourcesLocal.Install(this);
        }

		/// <summary>
		/// ファイナライザ。
		/// </summary>
		~WindowsAppFriend()
		{
			Dispose(false);
		}
#if ENG      
        /// <summary>
        /// Attach to other AppDomain.
        /// </summary>
        /// <returns>WindowsAppFriend for manipulation.</returns>
#else
        /// <summary>
        /// 現在操作しているAppDomain以外にアタッチ
        /// </summary>
        /// <returns>操作するためのWindowsAppFriend</returns>
#endif
        public WindowsAppFriend[] AttachOtherDomains()
        {
            var ctrl = Dim(new NewInfo("Codeer.Friendly.Windows.Step.AppDomainControl", DllInstaller.CodeerFriendlyWindowsNativeDllPath));
            var ids = (int[])ctrl["EnumDomains"]().Core;
            if (ids == null) {
                throw new NotSupportedException(ResourcesLocal.Instance.ErrorAttachOtherDomainsNeedNet4);
            }
            var ws = new List<WindowsAppFriend>();
            for (int i = 0; i < ids.Length; i++)
            {
                int id = ids[i];
                if (id == (int)this[typeof(AppDomain), "CurrentDomain"]()["Id"]().Core)
                {
                    continue;
                }
                var executeContextWindowHandle = _context.FriendlyConnector.FriendlyConnectorWindowInAppHandle;
                SystemController system = SystemStarter.StartInOtherAppDomain(ProcessId, executeContextWindowHandle,
                    e => ctrl["InitializeAppDomain"](id, e));
                ws.Add(new WindowsAppFriend(system, executeContextWindowHandle, _processId));
            }
            return ws.ToArray();
        }

#if ENG
        /// <summary>
        /// Disposes this object.
        /// When this method is called, communication with the target application
        /// is terminated and managed variables are be released.
        /// However, variables are only released from the managed domain and memory
        /// release is left to garbage collection.
        /// </summary>
#else
        /// <summary>
		/// 破棄します。
		/// このメソッドが呼び出されるとアプリケーションとの通信が切断され、管理していた変数が解放されます。
        /// ただし、管理領域から解放されるだけで、メモリの解放はガベージコレクションに委ねられます。
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
            if (_systemController != null)
			{
                _systemController.EndSystem();
                _systemController = null;
			}
            if (_syncCurrentConnector != null)
            {
                lock (_syncCurrentConnector)
                {
                    if (_context != null)
                    {
                        _context.Dispose();
                        _context = null;
                    }
                }

            }
            GC.Collect();
        }

#if ENG
        /// <summary>
        /// It changes executing thread context within the target application.
        /// </summary>
        /// <param name="context">ExecuteContext object indicating the executing thread in the target application.</param>
        /// <returns>The executing context before the change.</returns>
#else
        /// <summary>
        /// 対象アプリケーションでの実行スレッドを変更します。
        /// </summary>
        /// <param name="context">実行コンテキスト。</param>
        /// <returns>変更前の実行コンテキスト。</returns>
#endif
        public ExecuteContext ChangeContext(ExecuteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            lock (_syncCurrentConnector)
            {
                var old = _context;
                _context = context;
                return old;
            }
        }

#if ENG
        /// <summary>
        /// Causes the target application to load an assembly from an indicated path.
        /// If can't load assembly by Assembly.Load, load by Assembly.LoadFile.
        /// </summary>
        /// <param name="assembly">Assembly.</param>
#else
        /// <summary>
        /// テスト対象アプリケーションにアセンブリをロードさせます。
        /// Assembly.Loadで読み込める場合は、それを優先します。
        /// 読み込めなければAssembly.LoadFileを実行します。
        /// </summary>
        /// <param name="assembly">アセンブリ。</param>
#endif
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void LoadAssembly(Assembly assembly)
            => WindowsAppExpander.LoadAssembly(this, assembly);

        /// <summary>
        /// 送受信
        /// </summary>
        /// <param name="info">通信情報</param>
        /// <returns>戻り値</returns>
        ReturnInfo SendAndReceive(ProtocolInfo info)
        {
            FriendlyConnectorCore connector = null;
            lock (_syncCurrentConnector)
            {
                if (_context == null)
                {
                    return new ReturnInfo();
                }
                connector = _context.FriendlyConnector;
            }
            return connector.SendAndReceive(info, null);
        }
        /// <summary>
        /// アプリケーション内変数作成通知
        /// </summary>
        void AppVarCreateCountUp()
        {
            bool cleanUp = false;
            lock (_syncAppVarCreateCountUp)
            {
                _appVarCreateCount++;

                //テスト中はGCの回収率が悪い時がある。
                //それに備えて、一定数AppVarを生成するごとにGCを実施するようにする。
                if (100 < _appVarCreateCount)
                {
                    cleanUp = true;
                    _appVarCreateCount = 0;
                }
            }

            if (cleanUp)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// 接続者。
        /// </summary>
        class FriendlyConnectorWrap : IFriendlyConnector
        {
            WindowsAppFriend _app;

            /// <summary>
            /// コンストラクタ。
            /// </summary>
            /// <param name="app">アプリケーション操作クラス。</param>
            public FriendlyConnectorWrap(WindowsAppFriend app)
            {
                _app = app;
            }

            /// <summary>
            /// アプリケーション操作クラスを取得します。
            /// </summary>
            public AppFriend App { get { return _app; } }

            /// <summary>
            /// 接続者を区別するためのユニークなオブジェクト。
            /// </summary>
            public object Identity { get { return _app; } }

            /// <summary>
            /// 送受信。
            /// </summary>
            /// <param name="info">通信情報。</param>
            /// <returns>戻り値。</returns>
            public ReturnInfo SendAndReceive(ProtocolInfo info)
            {
                ReturnInfo ret = _app.SendAndReceive(info);
                if (ret != null && ((ret.ReturnValue as VarAddress) != null))
                {
                    _app.AppVarCreateCountUp();
                }
                return ret;
            }
        }
	}
}
