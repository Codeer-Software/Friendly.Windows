using System;
using System.Diagnostics;
using System.IO;
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
            var targetExePath = Path.GetFullPath(GetType().Assembly.Location + @"..\..\..\..\..\TestTargetCore\bin\DebugAnyCpu\netcoreapp3.0\TestTargetCore.exe");
            if (IntPtr.Size == 4)
            {
                //not tested.
                targetExePath = Path.GetFullPath(GetType().Assembly.Location + @"..\..\..\..\..\TestTargetCore\bin\Debugx86\netcoreapp3.0\TestTargetCore.exe");
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

        static Data GetIntValue(Data src) => new Data { Value = src.Value + 1 };
    }

    [Serializable]
    public class Data
    {
        public int Value { get; set; }
    }
}
