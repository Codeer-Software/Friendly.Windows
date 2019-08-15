using System;
using System.Diagnostics;
using System.IO;
using Codeer.Friendly.Dynamic;
using Codeer.Friendly.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class TestPermission
    {
     //   [TestMethod]
        public void Execute()
        {
            var targetApp = Process.GetProcessesByName("TestTargetx86")[0];
            var app = new WindowsAppFriend(targetApp);

            //操作確認
            app.Type("System.Windows.Application").Current.MainWindow.Title = "xxx";
        }
    }
}
