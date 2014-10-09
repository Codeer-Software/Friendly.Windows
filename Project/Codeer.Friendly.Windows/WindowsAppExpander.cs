#define CODE_ANALYSIS
using System;
using System.Diagnostics.CodeAnalysis;
using Codeer.Friendly.Windows.Inside;
using System.Reflection;

namespace Codeer.Friendly.Windows
{
#if ENG
    /// <summary>
    /// Extends test target applications to make them test-ready.
    /// Can load native DLLs as well as assemblies.
    /// When this class is used to load an assembly, references are resolved without placing the assembly in the search path of the target application.
    /// </summary>
#else
    /// <summary>
    /// テスト対象アプリケーションをテスト用に拡張します。
    /// 具体的には、ネイティブDllのロードとアセンブリのロードができます。
    /// アセンブリはこのクラスでロードした場合、対象アプリケーションの検索パスに置かなくとも参照解決されます。
    /// </summary>
#endif
    public static class WindowsAppExpander
    {
#if ENG
        /// <summary>
        /// Causes the target application to load the indicated native DLL.
        /// </summary>
        /// <param name="app">Application manipulation object.</param>
        /// <param name="fileName">Full path of DLL file.</param>
        /// <returns>Success / Failure.</returns>
#else
        /// <summary>
        /// テスト対象アプリケーションにネイティブdllをロードさせます。
        /// </summary>
        /// <param name="app">アプリケーション操作クラス。</param>
        /// <param name="fileName">ファイル名称。WindowsApiのLoadLibraryと同様のルールで使用できます。</param>
        /// <returns>成否。</returns>
#endif
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static bool LoadLibrary(WindowsAppFriend app, string fileName)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            AppVar ptr = app[typeof(WindowsAppExpanderInApp), "LoadLibrary"](fileName);
            return ((IntPtr)ptr.Core != IntPtr.Zero);
        }

#if ENG
        /// <summary>
        /// Causes the target application to load an assembly from an indicated path.
        /// </summary>
        /// <param name="app">Application manipulation object.</param>
        /// <param name="filePath">Full path of assembly.</param>
#else
        /// <summary>
        /// テスト対象アプリケーションにアセンブリをロードさせます。
        /// </summary>
        /// <param name="app">アプリケーション操作クラス。</param>
        /// <param name="filePath">ファイルパス。</param>
#endif
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void LoadAssemblyFromFile(WindowsAppFriend app, string filePath)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            app[typeof(WindowsAppExpanderInApp), "LoadFile"](filePath);
        }

#if ENG
        /// <summary>
        /// Causes the target application to load an assembly from an indicated path.
        /// If can't load assembly by Assembly.Load, load by Assembly.LoadFile.
        /// </summary>
        /// <param name="app">Application manipulation object.</param>
        /// <param name="assemblyString">Full name of assembly.</param>
        /// <param name="filePath">Full path of assembly.</param>
#else
        /// <summary>
        /// テスト対象アプリケーションにアセンブリをロードさせます。
        /// Assembly.Loadで読み込める場合は、それを優先します。
        /// 読み込めなければAssembly.LoadFileを実行します。
        /// </summary>
        /// <param name="app">アプリケーション操作クラス。</param>
        /// <param name="assemblyString">長い形式のアセンブリ名。</param>
        /// <param name="filePath">ファイルパス。</param>
#endif
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void LoadAssembly(WindowsAppFriend app, string assemblyString, string filePath)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            app[typeof(WindowsAppExpanderInApp), "LoadAssembly"](assemblyString, filePath);
        }

#if ENG
        /// <summary>
        /// Causes the target application to load an assembly from an indicated path.
        /// If can't load assembly by Assembly.Load, load by Assembly.LoadFile.
        /// </summary>
        /// <param name="app">Application manipulation object.</param>
        /// <param name="assembly">Assembly.</param>
#else
        /// <summary>
        /// テスト対象アプリケーションにアセンブリをロードさせます。
        /// Assembly.Loadで読み込める場合は、それを優先します。
        /// 読み込めなければAssembly.LoadFileを実行します。
        /// </summary>
        /// <param name="app">アプリケーション操作クラス。</param>
        /// <param name="assembly">アセンブリ。</param>
#endif
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void LoadAssembly(WindowsAppFriend app, Assembly assembly)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            } 
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            app[typeof(WindowsAppExpanderInApp), "LoadAssembly"](assembly.FullName, assembly.Location);
        }

#if ENG
        /// <summary>
        /// Causes the target application to load an assembly using an indicated full name.
        /// </summary>
        /// <param name="app">Application manipulation object.</param>
        /// <param name="assemblyString">Full name of assembly.</param>
#else
        /// <summary>
        /// テスト対象アプリケーションにアセンブリをロードさせます。
        /// </summary>
        /// <param name="app">アプリケーション操作クラス。</param>
        /// <param name="assemblyString">長い形式のアセンブリ名。</param>
#endif
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        [SuppressMessage("Microsoft.Naming", "CA1720:AvoidTypeNamesInParameters", MessageId = "1#")]
        public static void LoadAssemblyFromFullName(WindowsAppFriend app, string assemblyString)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            app[typeof(WindowsAppExpanderInApp), "Load"](assemblyString);
        }
    }
}
