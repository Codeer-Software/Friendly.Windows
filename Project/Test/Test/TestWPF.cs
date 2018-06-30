using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Codeer.Friendly;
using Codeer.Friendly.Dynamic;
using Codeer.Friendly.Windows;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Threading;

namespace MultiDomainTest
{
    [TestClass]
    public class TestWPF
    {
        [TestMethod]
        public void TestClose()
        {
            if (IntPtr.Size != 4)
            {
                return;
            }

            var p = Process.Start("WPFApp.exe");
            using (var app = new WindowsAppFriend(p))
            {
                var win = app.Type<Application>().Current.MainWindow;
                win.Close();
               // Thread.Sleep(15000);
                win.Close();

                /*
                while (true)
                {
                    var win = app.Type<Application>().Current.MainWindow;
                }*/
            }
        }
    }
}
