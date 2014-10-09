using System;
using System.Collections.Generic;
using System.Reflection;

namespace Codeer.Friendly.Windows.Step
{
    /// <summary>
    /// Windowsアプリケーション拡張。
    /// </summary>
    public static class AssemblyResolver
    {
        static bool connected;
        static Dictionary<string, Assembly> _asmDic = new Dictionary<string, Assembly>();

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
