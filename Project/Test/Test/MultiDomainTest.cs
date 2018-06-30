using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Codeer.Friendly;
using Codeer.Friendly.Dynamic;
using Codeer.Friendly.Windows;
using System.Diagnostics;
using System.Linq;

namespace MultiDomainTest
{
    [TestClass]
    public class MultiDomainTest
    {
        [TestMethod]
        public void Test_MultiDomain_2_32()
        {
            if (IntPtr.Size != 4)
            {
                return;
            }
            TestCore("MultiDomain_2_32.exe");
        }

        [TestMethod]
        public void Test_MultiDomain_4_32()
        {
            if (IntPtr.Size != 4)
            {
                return;
            }
            TestCore("MultiDomain_4_32.exe");
        }

        [TestMethod]
        public void Test_MultiDomain_4_64()
        {
            if (IntPtr.Size != 8)
            {
                return;
            }
            TestCore("MultiDomain_4_64.exe");
        }

        private static void TestCore(string exePath)
        {
            Process process = Process.Start(exePath);
            using (var app = new WindowsAppFriend(process))
            {
                app.Type().System.Windows.Forms.Application.OpenForms[0].StartMultiDomain();
                var apps = app.AttachOtherDomains();
                var names = apps.Select(e => (string)e.Type<AppDomain>().CurrentDomain.FriendlyName);
                Assert.AreEqual(2, names.Count());
                Assert.IsTrue(names.Any(e => e == "XXX"));
                Assert.IsTrue(names.Any(e => e == "YYY"));
            }
            process.Kill();
        }

    }
}
