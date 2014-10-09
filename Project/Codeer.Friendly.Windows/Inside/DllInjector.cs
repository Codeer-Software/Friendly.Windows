using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// DLLをインジェクションする処理。
    /// </summary>
    static class DllInjector
    {
        /// <summary>
        /// CodeerFriendlyWindows.dllを対象プロセスに読み込ませる。
        /// </summary>
        /// <param name="processHandle">対象プロセス操作ハンドル。</param>
        /// <param name="dllName">dll名称。</param>
        internal static void LoadDll(IntPtr processHandle, string dllName)
        {
            IntPtr pLibRemote = IntPtr.Zero;
            IntPtr hThreadLoader = IntPtr.Zero;
            try
            {
                //ロードさせるDLL名称を対象プロセス内にメモリを確保して書き込む
                List<byte> szLibPathTmp = new List<byte>(Encoding.Unicode.GetBytes(dllName));
                szLibPathTmp.Add(0);//NULL終端を書き足す。
                byte[] szLibPath = szLibPathTmp.ToArray();
                pLibRemote = NativeMethods.VirtualAllocEx(processHandle, IntPtr.Zero, new IntPtr(szLibPath.Length),
                                              NativeMethods.AllocationType.Commit, NativeMethods.MemoryProtection.ReadWrite);
                if (pLibRemote == IntPtr.Zero ||
                    !NativeMethods.WriteProcessMemory(processHandle, pLibRemote, szLibPath, new IntPtr((int)szLibPath.Length), IntPtr.Zero))
                {
                    throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorProcessOperation);
                }


                //実行関数取得
                IntPtr pFunc = NativeMethods.GetProcAddress(NativeMethods.GetModuleHandle("Kernel32"), "LoadLibraryW");
                if (pFunc == IntPtr.Zero)
                {
                    throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorDllLoad);
                }

                //対象プロセスにDLLをロードさせる
                IntPtr tid;
                hThreadLoader = NativeMethods.CreateRemoteThread(processHandle, IntPtr.Zero, IntPtr.Zero,
                            pFunc, pLibRemote, 0, out tid);
                if (hThreadLoader == IntPtr.Zero)
                {
                    throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorProcessOperation);
                }
                NativeMethods.WaitForSingleObject(hThreadLoader, NativeMethods.INFINITE);


            }
            finally
            {
                if (hThreadLoader != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(hThreadLoader);
                }
                if (pLibRemote != IntPtr.Zero)
                {
                    NativeMethods.VirtualFreeEx(processHandle, pLibRemote,
                             IntPtr.Zero, NativeMethods.FreeType.Release);
                }
            }
        }

        /// <summary>
        /// ネイティブのDLLメソッドを対象プロセスに実行させる。
        /// </summary>
        /// <param name="processHandle">プロセスのハンドル。</param>
        /// <param name="dllPath">DLL。</param>
        /// <param name="function">関数名称。</param>
        /// <param name="args">引数。</param>
        internal static void ExecuteRemoteFunction(IntPtr processHandle, string dllPath, string function, string args)
        {
            IntPtr pArgs = IntPtr.Zero;
            IntPtr hThreadServerOpen = IntPtr.Zero;
            try
            {
                //受信ウィンドウハンドルを対象プロセス内にメモリを確保して書き込む longを文字列化して書き込む
                List<byte> startInfoTmp = new List<byte>(Encoding.Unicode.GetBytes(args));
                startInfoTmp.Add(0);//null終端を足す
                byte[] startInfo = startInfoTmp.ToArray();
                pArgs = NativeMethods.VirtualAllocEx(processHandle, IntPtr.Zero, new IntPtr(startInfo.Length),
                                              NativeMethods.AllocationType.Commit, NativeMethods.MemoryProtection.ReadWrite);
                if (pArgs == IntPtr.Zero ||
                    !NativeMethods.WriteProcessMemory(processHandle, pArgs, startInfo, new IntPtr((int)startInfo.Length), IntPtr.Zero))
                {
                    throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorProcessOperation);
                }

                {
                    //実行関数取得
                    IntPtr pFunc = DllInjector.GetTargetProcAddress(processHandle, dllPath, function);
                    if (pFunc == IntPtr.Zero)
                    {
                        throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorFriendlySystem);
                    }

                    //対象プロセスでサーバー開始メソッドを実行
                    IntPtr tid;
                    hThreadServerOpen = NativeMethods.CreateRemoteThread(processHandle, IntPtr.Zero, IntPtr.Zero,
                                pFunc, pArgs, 0, out tid);
                    if (hThreadServerOpen == IntPtr.Zero)
                    {
                        throw new FriendlyOperationException(ResourcesLocal.Instance.ErrorProcessOperation);
                    }
                    NativeMethods.WaitForSingleObject(hThreadServerOpen, NativeMethods.INFINITE);
                }
            }
            finally
            {
                if (hThreadServerOpen != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(hThreadServerOpen);
                }
                if (pArgs != IntPtr.Zero)
                {
                    NativeMethods.VirtualFreeEx(processHandle, pArgs,
                             IntPtr.Zero, NativeMethods.FreeType.Release);
                }
            }
        }

        /// <summary>
        /// 対象プロセスのDLL関数アドレスの取得。
        /// </summary>
        /// <param name="processHandle">対象プロセス操作ハンドル。</param>
        /// <param name="dllPath">DLL。</param>
        /// <param name="procName">関数名称。</param>
        /// <returns>アドレス。</returns>
        static IntPtr GetTargetProcAddress(IntPtr processHandle, string dllPath, string procName)
        {
            //自分のプロセスにロードして距離を計測する。
            IntPtr mod = NativeMethods.LoadLibrary(dllPath);
            IntPtr proc = NativeMethods.GetProcAddress(mod, procName);
            if (proc == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            //対象プロセス内のベースアドレスを取得。
            IntPtr targetBase = GetModuleBase(processHandle, dllPath);
            if (targetBase == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            //対象プロセスの関数アドレスを計算。
            return (IntPtr.Size == 4) ? CalcProcAddressInt(mod, proc, targetBase) : CalcProcAddressLong(mod, proc, targetBase);
        }

        /// <summary>
        /// 対象プロセスのDLL関数アドレスを計算。
        /// </summary>
        /// <param name="mod">自プロセスのモジュールハンドル。</param>
        /// <param name="proc">自プロセスの関数アドレス。</param>
        /// <param name="targetModBase">対象プロセスのDLLベースアドレス。</param>
        /// <returns>対象プロセスのDLL関数アドレス。</returns>
        static IntPtr CalcProcAddressLong(IntPtr mod, IntPtr proc, IntPtr targetModBase)
        {
            ulong distance = (ulong)proc.ToInt64() - (ulong)mod.ToInt64();
            return new IntPtr((long)((ulong)targetModBase.ToInt64() + distance));
        }

        /// <summary>
        /// 対象プロセスのDLL関数アドレスを計算。
        /// </summary>
        /// <param name="mod">自プロセスのモジュールハンドル。</param>
        /// <param name="proc">自プロセスの関数アドレス。</param>
        /// <param name="targetModBase">対象プロセスのDLLベースアドレス。</param>
        /// <returns>対象プロセスのDLL関数アドレス。</returns>
        static IntPtr CalcProcAddressInt(IntPtr mod, IntPtr proc, IntPtr targetModBase)
        {
            uint distance = (uint)proc.ToInt32() - (uint)mod.ToInt32();
            return new IntPtr((int)((uint)targetModBase.ToInt32() + distance));
        }

        /// <summary>
        /// 指定プロセスのDLLベースアドレスを取得。
        /// </summary>
        /// <param name="processHandle">プロセス操作ハンドル。</param>
        /// <param name="dllPath">DLL。</param>
        /// <returns>指定プロセスのDLLベースアドレス。</returns>
        private static IntPtr GetModuleBase(IntPtr processHandle, string dllPath)
        {
            //指定プロセスのロードしているモジュールを全取得
            IntPtr[] lphModule = new IntPtr[0];
            while (true)
            {
                uint binSize;
                if (!NativeMethods.EnumProcessModules(processHandle, lphModule, (uint)(lphModule.Length * IntPtr.Size), out binSize))
                {
                    return IntPtr.Zero;
                }
                int modCount = (int)(binSize / IntPtr.Size);
                if (modCount == lphModule.Length)
                {
                    break;
                }
                lphModule = new IntPtr[modCount];
            }

            //指定のdllのベースアドレスを取得。
            for (int i = 0; i < lphModule.Length; i++)
            {
                StringBuilder filePath = new StringBuilder((1024 + 1) * 8);
                if (NativeMethods.GetModuleFileNameEx(processHandle, lphModule[i], filePath, 1024 * 8) == 0)
                {
                    continue;
                }
                if (string.Compare(filePath.ToString(), dllPath, true, CultureInfo.CurrentCulture) == 0)
                {
                    return lphModule[i];
                }
            }
            return IntPtr.Zero;
        }
    }
}
