#define CODE_ANALYSIS
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// Windowsアプリケーション拡張。
    /// </summary>
    public static class WindowsAppExpanderInApp
    {
        static bool connected;
        static Dictionary<string, Assembly> _asmDic = new Dictionary<string, Assembly>();

        /// <summary>
        /// DLLのロード。
        /// </summary>
        /// <param name="fileName">ファイル名称。</param>
        /// <returns>モジュールハンドル。</returns>
        static IntPtr LoadLibrary(string fileName)
        {
            return NativeMethods.LoadLibrary(fileName);
        }

        /// <summary>
        /// ファイルからアセンブリ読み込み。
        /// </summary>
        /// <param name="filePath">ファイルパス。</param>
        static void LoadFile(string filePath)
        {
            EntryAssembly(Assembly.LoadFile(filePath));
        }

        /// <summary>
        /// アセンブリのフルネームで読み込めたら、それを採用。
        /// 読み込めなければ、指定のファイルパスを使う。
        /// </summary>
        /// <param name="assemblyString">長い形式のアセンブリ名。</param>
        /// <param name="filePath">ファイルパス。</param>
        internal static void LoadAssembly(string assemblyString, string filePath)
        {
            lock (_asmDic)
            {
                if (_asmDic.ContainsKey(assemblyString))
                {
                    return;
                }
            }
            Assembly asm = null;
            try
            {
                asm = Assembly.Load(assemblyString);
            }
            catch { }
            if (asm == null)
            {
                asm = Assembly.LoadFile(filePath);
            }
            EntryAssembly(asm);
        }

        /// <summary>
        /// アセンブリ名称からアセンブリ読み込み。
        /// </summary>
        /// <param name="assemblyString">長い形式のアセンブリ名。</param>
        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "0#")]
        static void Load(string assemblyString)
        {
            EntryAssembly(Assembly.Load(assemblyString));
        }

        /// <summary>
        /// アセンブリ登録。
        /// </summary>
        /// <param name="asm">アセンブリ。</param>
        internal static void EntryAssembly(Assembly asm)
        {
            lock (_asmDic)
            {
                if (_asmDic.ContainsKey(asm.FullName))
                {
                    return;
                }
                _asmDic.Add(asm.FullName, asm);
                if (!connected)
                {
                    connected = true;
                    AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args)
                    {
                        Assembly resolve;
                        return _asmDic.TryGetValue(args.Name, out resolve) ? resolve : null;
                    };
                }
            }
        }
    }
}
