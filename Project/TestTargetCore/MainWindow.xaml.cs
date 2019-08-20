using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TestTargetCore
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var handle = Kernel32.OpenProcess(Kernel32.ProcessSecurity.ProcessVmRead | Kernel32.ProcessSecurity.ProcessQueryInformation, false, (uint)Process.GetCurrentProcess().Id);

            var w = new Stopwatch();
            w.Start();
            var dlls = string.Join("\r\n", GetModuleFileNames(handle));
            w.Stop();
            var x = w.ElapsedMilliseconds;
            _text.Text = string.Join("\r\n", GetModuleFileNames(handle));
        }

        static string[] GetModuleFileNames(IntPtr hProcess)
        {
            var modules = new IntPtr[2048];
            Psapi.EnumProcessModulesEx(hProcess, modules, IntPtr.Size * 2048, out var cbNeeded, Psapi.ListModules.ListModulesAll);
            var moduleNames = new List<string>();
            for (var i = 0; i < cbNeeded / IntPtr.Size; ++i)
            {
                var sb = new StringBuilder(256);
                Psapi.GetModuleFileNameEx(hProcess, modules[i], sb, 256);
                moduleNames.Add(sb.ToString());
            }

            return moduleNames.ToArray();
        }

        static bool HasCoreClr(string[] modules)
        {
            return modules.Any(x => x.ToLower().Contains("coreclr.dll"));
        }
    }




    static class Kernel32
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess([MarshalAs(UnmanagedType.I4)] ProcessSecurity dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            uint dwProcessId);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        public enum ProcessSecurity : uint
        {
            ProcessVmRead = 0x0010,
            ProcessQueryInformation = 0x0400,
        }
    }

    static class Psapi
    {
        [DllImport("psapi.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumProcesses(uint[] lpidProcesses, uint cb, out uint lpcbNeeded);

        [DllImport("psapi.dll")]
        public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename,
            uint nSize);

        [DllImport("psapi.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumProcessModulesEx(IntPtr hProcess, IntPtr[] lphModule, int cb,
            out uint lpcbNeeded, [MarshalAs(UnmanagedType.I4)] ListModules dwFilterFlag);

        public enum ListModules : int
        {
            ListModules32Bit = 0x01,
            ListModules64Bit = 0x02,
            ListModulesAll = 0x03,
            ListModulesDefault = 0x0,
        }
    }
}
