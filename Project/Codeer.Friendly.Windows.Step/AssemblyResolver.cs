using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

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
            File.AppendAllText(@"c:\FriendlyLog\log.txt", "\r\n try load : " + assemblyString + ":" + filePath, Encoding.GetEncoding("shift_jis"));

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

                File.AppendAllText(@"c:\FriendlyLog\log.txt", "\r\n by assemblyString : " + assemblyString, Encoding.GetEncoding("shift_jis"));
            }
            catch { }
            try
            {
                if (asm == null)
                {
                    asm = Assembly.LoadFrom(filePath);

                    File.AppendAllText(@"c:\FriendlyLog\log.txt", "\r\n by assemblyString : " + assemblyString, Encoding.GetEncoding("shift_jis"));
                }
            }
            catch { }

            if (asm == null)
            {
                File.AppendAllText(@"c:\FriendlyLog\log.txt", "\r\n ★★★ Dll Read Error : " + assemblyString, Encoding.GetEncoding("shift_jis"));
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

                File.AppendAllText(@"c:\FriendlyLog\log.txt", "\r\n Entry to Resolver : " + asm.FullName, Encoding.GetEncoding("shift_jis"));

                if (!connected)
                {
                    connected = true;
                    AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args)
                    {
                        Assembly resolve;
                        var ret = _asmDic.TryGetValue(args.Name, out resolve) ? resolve : null;

                        if (ret == null)
                        {
                            File.AppendAllText(@"c:\FriendlyLog\log.txt", "\r\n ★★★ Dll Resolve Error : " + args.Name, Encoding.GetEncoding("shift_jis"));
                        }
                        else
                        {
                            File.AppendAllText(@"c:\FriendlyLog\log.txt", "\r\n Dll Resolve : " + args.Name, Encoding.GetEncoding("shift_jis"));
                        }

                        return ret;
                    };
                }
            }
        }
    }
}
