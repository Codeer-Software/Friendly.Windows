using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Codeer.Friendly.Dynamic;
using Codeer.Friendly.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class TestNetFramewrok
    {
        //半自動のテスト
        [TestMethod]
        public void TestPermission()
        {
            if (IntPtr.Size != 4) return;

            var processes = Process.GetProcessesByName("TestTargetx86");
            if (processes.Length != 1) return;

            var targetApp = processes[0];
            var app = new WindowsAppFriend(targetApp);

            //操作確認
            app.Type("System.Windows.Application").Current.MainWindow.Title = "xxx";
        }

        [TestMethod]
        public void TestNormal()
        {
            if (IntPtr.Size != 4) return;

            var targetExePath = Path.GetFullPath(GetType().Assembly.Location + @"..\..\..\..\..\TestTargetx86\bin\Debug\TestTargetx86.exe");
            var targetApp = Process.Start(targetExePath);
            var app = new WindowsAppFriend(targetApp);

            //操作確認
            app.Type("System.Windows.Application").Current.MainWindow.Title = "xxx";

            targetApp.Kill();
            Thread.Sleep(1000);
        }
    }
}
