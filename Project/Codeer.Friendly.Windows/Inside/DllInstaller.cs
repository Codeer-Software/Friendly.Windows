using System;
using System.IO;
using System.Threading;
using Codeer.Friendly.Windows.Properties;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// DLLインストール。
    /// </summary>
    static class DllInstaller
    {
        /// <summary>
        /// CodeerFriendlyWindows_cpu.dllのフルパス。
        /// </summary>
        internal static string CodeerFriendlyWindowsNativeDllPath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                            "Codeer.Friendly");
                string cpu = (IntPtr.Size == 4) ? "x86" : "x64";
                string dllName = "CodeerFriendlyWindows_" + cpu + "_1035.dll";
                string dllPath = Path.Combine(dir, dllName);
                return dllPath;
            }
        }

        /// <summary>
        /// CodeerFriendlyWindows_cpu.dllのフルパス。
        /// </summary>
        internal static string CodeerFriendlyWindowsCoreNativeDllPath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                            "Codeer.Friendly");
                string cpu = (IntPtr.Size == 4) ? "x86" : "x64";
                string dllName = "CodeerFriendlyWindowsCore_" + cpu + "_0000.dll";
                string dllPath = Path.Combine(dir, dllName);
                return dllPath;
            }
        }

        /// <summary>
        /// 踏み台用DLLのフルパス。
        /// </summary>
        static string CodeerFriendlyWindowsStepDllPth
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                            "Codeer.Friendly");
                string dllName = "Codeer.Friendly.Windows.Step" + "_1021" + ".dll";
                string dllPath = Path.Combine(dir, dllName);
                return dllPath;
            }
        }

        /// <summary>
        /// ネイティブのCodeerFriendlyWindows_cpu.dllの初期化。
        /// </summary>
        /// <returns>CodeerFriendlyWindows_cpu.dllのフルパス。</returns>
        internal static string InitializeCodeerFriendlyWindowsNative()
        {
            string dllPath = CodeerFriendlyWindowsNativeDllPath;
            byte[] dllData = (IntPtr.Size == 4) ? Resources.CodeerFriendlyWindows_x86 : Resources.CodeerFriendlyWindows_x64;
            InstallDll(dllPath, dllData);
            return dllPath;
        }

        /// <summary>
        /// ネイティブのCodeerFriendlyWindows_cpu.dllの初期化。
        /// </summary>
        /// <returns>CodeerFriendlyWindows_cpu.dllのフルパス。</returns>
        internal static string InitializeCodeerFriendlyWindowsCoreNative()
        {
            string dllPath = CodeerFriendlyWindowsCoreNativeDllPath;
            byte[] dllData = (IntPtr.Size == 4) ? Resources.CodeerFriendlyWindowsCore_x86 : Resources.CodeerFriendlyWindowsCore_x64;
            InstallDll(dllPath, dllData);
            return dllPath;
        }

        /// <summary>
        /// 踏み台用DLL初期化。
        /// </summary>
        /// <returns>踏み台用DLLのフルパス。</returns>
        internal static string InitializeCodeerFriendlyWindowsStep()
        {
            string dllPath = CodeerFriendlyWindowsStepDllPth;
            byte[] dllData = Resources.Codeer_Friendly_Windows_Step;
            InstallDll(dllPath, dllData);
            return dllPath;
        }

        /// <summary>
        /// DLLのインストール。
        /// </summary>
        /// <param name="dllPath">DLLのパス。</param>
        /// <param name="dllData">DLLのバイナリデータ。</param>
        private static void InstallDll(string dllPath, byte[] dllData)
        {
            string dir = Path.GetDirectoryName(dllPath);
            string name = Path.GetFileNameWithoutExtension(dllPath);
            Mutex mutex = new Mutex(false, name);
            try
            {
                try
                {
                    mutex.WaitOne();
                }
                catch { }
                try
                {
                    byte[] buf = File.ReadAllBytes(dllPath);
                    if (IsMatchBinary(buf, dllData))
                    {
                        return;//インストール済み
                    }
                }
                catch { }

                //ディレクトリ作成               
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch
                {
                    throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorFriendlySystem);
                }
                //古いファイルを削除
                try
                {
                    if (File.Exists(dllPath))
                    {
                        File.Delete(dllPath);
                    }
                }
                catch
                {
                    throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorBinaryInstall
                        + Environment.NewLine + dllPath);
                }
                //書き込み
                try
                {
                    File.WriteAllBytes(dllPath, dllData);
                }
                catch
                {
                    throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorFriendlySystem);
                }
            }
            finally
            {
                //ミューテックスを解放する
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// バイナリの一致チェック。
        /// </summary>
        /// <param name="buf1">バイナリ1。</param>
        /// <param name="buf2">バイナリ2。</param>
        /// <returns>一致するか。</returns>
        private static bool IsMatchBinary(byte[] buf1, byte[] buf2)
        {
            if (buf1.Length != buf2.Length)
            {
                return false;
            }
            for (int i = 0; i < buf1.Length; i++)
            {
                if (buf1[i] != buf2[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
