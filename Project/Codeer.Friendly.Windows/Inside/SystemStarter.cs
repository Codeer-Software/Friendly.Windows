using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Codeer.Friendly;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Codeer.Friendly.DotNetExecutor;
using Codeer.Friendly.Windows.Properties;

namespace Codeer.Friendly.Windows.Inside
{
	/// <summary>
	/// システム起動。
	/// </summary>
	static class SystemStarter
	{
        /// <summary>
        /// エラー通知内容。
        /// </summary>
        const int ERR_UNPREDICATABLE_CLR_VERSION = 1;

        /// <summary>
        /// 開始処理
        /// </summary>
        /// <param name="args">起動引数</param>
        internal delegate void ExecuteStart(string args);

		/// <summary>
		/// 起動。
		/// </summary>
		/// <param name="process">対象プロセス。</param>
        /// <param name="clrVersion">CLRバージョン名称。</param>
        /// <param name="initializeThreadWindowHandle">初期化を実行させるスレッドに属するウィンドウのハンドル。</param>
        /// <returns>システムコントローラー。</returns>
        internal static SystemController Start(Process process, string clrVersion, IntPtr initializeThreadWindowHandle)
		{
            //念のためProcessがメッセージを受け付けるのを確認する
            NativeMethods.SendMessage(initializeThreadWindowHandle, 0, IntPtr.Zero, IntPtr.Zero);

            //必要な権限でプロセスを開く
            int processId = process.Id;
            IntPtr processHandle = NativeMethods.OpenProcess(
                NativeMethods.ProcessAccessFlags.CreateThread |
                NativeMethods.ProcessAccessFlags.VMOperation |
                NativeMethods.ProcessAccessFlags.VMRead |
                NativeMethods.ProcessAccessFlags.VMWrite |
                NativeMethods.ProcessAccessFlags.QueryInformation,
                false, processId);
            if (processHandle == IntPtr.Zero)
            {
                throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorProcessOperation);
            }

            try
            {
                if (!CpuTargetCheckUtility.IsSameCpu(process))
                {
                    throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorTargetCpuDifference);
                }

                //PCにDLLインストール
                var injectionDllPath = DllInstaller.InitializeCodeerFriendlyWindowsNative();

                //対象プロセスにDLLをロードする
                Debug.Trace("Dll Loading.");
                DllInjector.LoadDll(processHandle, injectionDllPath);
                Debug.Trace("Dll Loaded.");

                //.NetCore対応
                if (string.IsNullOrEmpty(clrVersion))
                {
                    clrVersion = TryGetCoreClrDllPath(processHandle, injectionDllPath);
                }
                if (clrVersion.IndexOf("\\") != -1)
                {
                    //.NetCore用の初期化DLLをインジェクションする
                    injectionDllPath = DllInstaller.InitializeCodeerFriendlyWindowsCoreNative();
                    Debug.Trace("Dll Loading.");
                    DllInjector.LoadDll(processHandle, injectionDllPath);
                    Debug.Trace("Dll Loaded.");
                }

                string assemblyStepPath = DllInstaller.InitializeCodeerFriendlyWindowsStep();


                //初期化処理
                return StartInApp(processHandle, processId, injectionDllPath, assemblyStepPath, 
                    Debug.ReadDebugMark(clrVersion), initializeThreadWindowHandle);
            }
            finally
            {
                NativeMethods.CloseHandle(processHandle);
                GC.KeepAlive(process);
            }
		}

        /// <summary>
        /// デフォルト以外のAppDomainで起動。
        /// </summary>
        /// <param name="processId">プロセスID。</param>
        /// <param name="initializeThreadWindowHandle">初期化を実行させるスレッドに属するウィンドウのハンドル。</param>
        /// <param name="start">開始処理</param>
        /// <returns>システムコントローラー。</returns>
        internal static SystemController StartInOtherAppDomain(int processId, IntPtr initializeThreadWindowHandle, ExecuteStart start)
        {
            return StartInApp("nouse", "nouse", processId, initializeThreadWindowHandle, start);
        }

        /// <summary>
        /// プロセスでシステムを起動させる。
        /// </summary>
        /// <param name="clrVersion">CLRのバージョン</param>
        /// <param name="assemblyStep">踏み台アセンブリへのパス</param>
        /// <param name="processId">プロセスID。</param>
        /// <param name="initializeThreadWindowHandle">初期化を実行させるスレッドに属するウィンドウのハンドル。</param>
        /// <param name="start">開始処理</param>
        /// <returns>システムコントローラー。</returns>
        static SystemController StartInApp(string clrVersion, string assemblyStep, int processId, IntPtr initializeThreadWindowHandle, ExecuteStart start)
        {
            SystemStartResponseReciever reciever = new SystemStartResponseReciever();
            IntPtr recieveWindowHandle = reciever.Start(processId);

            //開始
            start(CreateStartupInfo(clrVersion, assemblyStep, recieveWindowHandle, initializeThreadWindowHandle));

            //受信スレッドの終了待ち（返信待ち）
            long errNo = 0;
            IntPtr systemControlWindowInAppHandle = reciever.WaitForCompletion(ref errNo);

            //nullなら失敗
            if (systemControlWindowInAppHandle == IntPtr.Zero)
            {
                switch (errNo)
                {
                    case ERR_UNPREDICATABLE_CLR_VERSION:
                        throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorUnpredicatableClrVersion);
                    default:
                        throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorProcessAcess);
                }
            }

            //システムコントロールウィンドウのハンドルを返す
            Debug.Trace("InitializeFriendly Success.");
            return new SystemController(systemControlWindowInAppHandle);
        }

        /// <summary>
        /// プロセスでシステムを起動させる。
        /// </summary>
        /// <param name="processHandle">対象プロセス操作ハンドル。</param>
        /// <param name="processId">プロセスID。</param>
        /// <param name="dllName">サーバー側で動作させるDLL名称。</param>
        /// <param name="assemblyStep">踏み台用アセンブリのパス。</param>
        /// <param name="clrVersion">CLRのバージョン。</param>
        /// <param name="initializeThreadWindowHandle">初期化を実行させるスレッドに属するウィンドウのハンドル。</param>
        /// <returns>システムコントローラー。</returns>
        static SystemController StartInApp(IntPtr processHandle, int processId, string dllName, string assemblyStep, 
                                            string clrVersion, IntPtr initializeThreadWindowHandle)
		{
            return StartInApp(clrVersion, assemblyStep, processId, initializeThreadWindowHandle,
                e => 
                {
                    Debug.Trace("Call InitializeFriendly.");
                    DllInjector.ExecuteRemoteFunction(processHandle, dllName, "InitializeFriendly", e);
                    Debug.Trace("InitializeFriendly Finished.");
                });
		}

        /// <summary>
        /// 起動情報作成
        /// </summary>
        /// <param name="clrVersion">CLRのバージョン</param>
        /// <param name="szAssemblyStep">踏み台アセンブリへのパス</param>
        /// <param name="recieveWindowHandle">通信用ウィンドウハンドル</param>
        /// <param name="initializeThreadWindowHandle">初期化を実行させるスレッドに属するウィンドウのハンドル。</param>
        /// <returns>起動情報</returns>
        private static string CreateStartupInfo(string clrVersion, string szAssemblyStep, IntPtr recieveWindowHandle, IntPtr initializeThreadWindowHandle)
        {
            //通信用ウィンドウハンドルとデバッグマーク
            string startupInfoCore = recieveWindowHandle.ToInt64().ToString(CultureInfo.CurrentCulture) + Debug.DebugMark;

            //実行メソッドの情報
            string szTypeFullName = "Codeer.Friendly.Windows.Step.StartStep";
            string szMethod = "Start";

            //SystemStarterInApp.Startに渡す引数。
            //最初にロードさせるアセンブリとstartupInfoCore
            string szArgs = typeof(AppVar).Assembly.FullName + "|" + typeof(AppVar).Assembly.Location + "||" +
                            typeof(SystemStarter).Assembly.FullName + "|" + typeof(SystemStarter).Assembly.Location + "||" +
                            typeof(SystemStarterInApp).FullName + "||" +
                            "Start" + "||" +
                            startupInfoCore;
            
            //空文字指定の場合。
            //相手のランタイムのバージョンに合わせる。
            //ネイティブの場合は自分のランタイムのバージョンを使う。
            if (string.IsNullOrEmpty(clrVersion))
            {
                clrVersion = "@" + RuntimeEnvironment.GetSystemVersion();
            }
            //互換性を保つため。
            //CLRのバージョンはフルで指定させるようにするが、既存の仕様はそうではなかったので。
            else if (clrVersion == "4.0")
            {
                string srcClr = clrVersion;
                clrVersion = "v4.0.30319";
                Trace.TraceWarning(ResourcesLocal.ObsoleteClrOrder, srcClr, clrVersion);
            }
            else if (clrVersion == "2.0")
            {
                string srcClr = clrVersion;
                clrVersion = "v2.0.50727";
                Trace.TraceWarning(ResourcesLocal.ObsoleteClrOrder, srcClr, clrVersion);
            }
            return initializeThreadWindowHandle + "\t" + recieveWindowHandle + "\t" +
                clrVersion + "\t" + szAssemblyStep + "\t" + szTypeFullName + "\t" + szMethod + "\t" + szArgs;
        }

        static string TryGetCoreClrDllPath(IntPtr processHandle, string injectionForDotNetFramewrokDllPath)
        {
            try
            {
                foreach(var e in DllInjector.GetProcessModules(processHandle))
                {
                    var sb = new StringBuilder((1024 + 1) * 8);
                    NativeMethods.GetModuleFileNameEx(processHandle, e, sb, sb.Capacity);
                    var path = sb.ToString().ToLower();
                    if (path.Contains("coreclr.dll") &&
                        DllInjector.ExecuteRemoteFunction(processHandle, injectionForDotNetFramewrokDllPath, "HasGetCLRRuntimeHost", path) != 0)
                    {
                        return path;
                    }
                }
                return string.Empty;
            }
            catch { }
            return string.Empty;
        }
    }
}
