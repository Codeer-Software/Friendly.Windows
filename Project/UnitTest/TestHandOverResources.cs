using System;
using System.Diagnostics;
using System.IO;
using Codeer.Friendly;
using Codeer.Friendly.Dynamic;
using Codeer.Friendly.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class TestHandOverResources
    {
        WindowsAppFriend app2;

        [TestInitialize]
        public void TestInitialize()
        {
            //対象プロセス(x86)
            var targetExePath = Path.GetFullPath(GetType().Assembly.Location + @"..\..\..\..\..\TestTargetx86\bin\Debug\TestTargetx86.exe");
            var targetApp = Process.Start(targetExePath);

            //アタッチしてその通信情報を引き継ぐためのバイナリを生成
            var myProcess = Process.GetCurrentProcess();
            var binPath = Path.GetTempFileName();
            var attachExePath = Path.GetFullPath(GetType().Assembly.Location + @"..\..\..\..\..\Attachx86\bin\Debug\Attachx86.exe");
            Process.Start(attachExePath, $"{targetApp.Id} {myProcess.Id} {binPath}").WaitForExit();

            //バイナリを元にWindowsAppFriend生成
            var bin = File.ReadAllBytes(binPath);
            File.Delete(binPath);
            app2 = new WindowsAppFriend(targetApp.MainWindowHandle, bin);
        }

        [TestCleanup]
        public void TestCleanup() => Process.GetProcessById(app2.ProcessId).Kill();

        [TestMethod]
        public void Execute64()
        {
            //操作確認
            app2.Type("System.Windows.Application").Current.MainWindow.Title = "xxx";
        }

        [TestMethod]
        public void ConvertToWrapper()
        {
            WindowWrapper w = app2.Type("System.Windows.Application").Current.MainWindow;
            w.Title = "yyy";
        }

        [TestMethod]
        public void ConvertNull()
        {
            app2.LoadAssembly(GetType().Assembly);
            WindowWrapper w = app2.Null();
            Assert.IsNull(w);
        }

        [TestMethod]
        public void SerializeTest()
        {
            app2.LoadAssembly(GetType().Assembly);
            WindowWrapperSerializable data = app2.Type(GetType()).SerializableData;
            Assert.AreEqual(100, data.Value);
        }

        public class WindowWrapper
        {
            AppVar _core;
            public WindowWrapper(AppVar src)
            {
                _core = src;
            }

            public string Title
            {
                get => _core.Dynamic().Title;
                set => _core.Dynamic().Title = value;
            }
        }

        public static WindowWrapperSerializable SerializableData = new WindowWrapperSerializable { Value = 100 };

        [Serializable]
        public class WindowWrapperSerializable
        {
            public int Value { get; set; }

            public WindowWrapperSerializable(AppVar src) { }

            public WindowWrapperSerializable() { }
        }

    }
}
