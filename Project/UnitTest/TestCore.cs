using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Codeer.Friendly.Dynamic;
using Codeer.Friendly.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class TestCore
    {
        WindowsAppFriend _app;

        [TestInitialize]
        public void TestInitialize()
        {
            var targetExePath = Path.GetFullPath(GetType().Assembly.Location + @"..\..\..\..\..\TestTargetCore\bin\Debug\netcoreapp3.0\TestTargetCore.exe");
            if (IntPtr.Size == 4)
            {
                //not tested.
                targetExePath = Path.GetFullPath(GetType().Assembly.Location + @"..\..\..\..\..\TestTargetCore\bin\Debug\netcoreapp3.0\TestTargetCore.exe");
            }

            var targetApp = Process.Start(targetExePath);
            _app = new WindowsAppFriend(targetApp);
        }

        [TestCleanup]
        public void TestCleanup() => Process.GetProcessById(_app.ProcessId).Kill();

        [TestMethod]
        public void Execute()
        {
            var w = _app.Type("System.Windows.Application").Current.MainWindow;
            w.Title = "yyy";
        }

        [TestMethod]
        public void LoadAssembly()
        {
            _app.LoadAssembly(GetType().Assembly);
            Data value = _app.Type(GetType()).GetIntValue(new Data { Value = 200 });
            Assert.AreEqual(201, value.Value);
        }

        public class X<T1, T2, T3, T4, T5> { }
        public class Y { }

        [TestMethod]
        public void TestFinder()
        {
            var typeFinder = new Codeer.Friendly.DotNetExecutor.TypeFinder();
            var typeFinderCoreType = typeFinder.GetType("Codeer.Friendly.DotNetExecutor.TypeFinder+TypeFinderCore");
            var typeFinderCore = Activator.CreateInstance(typeFinderCoreType);

            Type stringType = typeFinder.GetType("Codeer.Friendly.DotNetExecutor.TypeFinder+StringType");
            MethodInfo methodInfoMakeType = stringType.GetMethod("MakeType", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo methodInfoParse = stringType.GetMethod("Parse", BindingFlags.Static | BindingFlags.NonPublic);

            var testTargetType = new[]
            {
                  typeof(int)
                , typeof(List<int>)
                , typeof(Dictionary<int, string>)
                , typeof(Dictionary<Dictionary<string, List<Codeer.Friendly.DotNetExecutor.TypeFinder>>, Dictionary<bool, X<Y, Y, int, string, bool>>>)
                // 最後の1つは全てキャッシュから変換されるか確認用
                , typeof(Dictionary<Dictionary<string, List<Codeer.Friendly.DotNetExecutor.TypeFinder>>, Dictionary<bool, X<Y, Y, int, string, bool>>>)
            };

            foreach(var type in testTargetType)
            {
                var typeTmp = methodInfoParse.Invoke(null, new[] { type.FullName });
                var result = methodInfoMakeType.Invoke(typeTmp, new[] { typeFinderCore });
                var fullName = result.GetType().GetProperty("FullName").GetValue(result, null) as string;
                Assert.IsTrue(type.FullName == fullName);
            }
        }

        static Data GetIntValue(Data src) => new Data { Value = src.Value + 1 };
    }

    [Serializable]
    public class Data
    {
        public int Value { get; set; }
    }
}
