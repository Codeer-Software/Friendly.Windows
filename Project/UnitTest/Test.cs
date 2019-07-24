using System.Diagnostics;
using System.IO;
using Codeer.Friendly.Dynamic;
using Codeer.Friendly.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class Test
    {
        [TestMethod]
        public void Execute64()
        {
            //対象プロセス(x64)
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
            var app2 = new WindowsAppFriend(targetApp.MainWindowHandle, bin);

            //操作確認
            app2.Type("System.Windows.Application").Current.MainWindow.Title = "xxx";

            targetApp.Kill();
        }
    }
}
