using Codeer.Friendly.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace LogTest
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        delegate int EnumWindowsDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowEnabled(IntPtr hWnd);
        [DllImport("kernel32.dll")]
        static extern int GetCurrentThreadId();
        [DllImport("user32.dll")]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        [DllImport("user32.dll")]
        static extern int EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public static IntPtr[] GetTopLevelWindows(int processId)
        {
            IntPtr serverWnd = IntPtr.Zero;
            var handles = new List<IntPtr>();
            EnumWindowsDelegate callback = delegate (IntPtr hWnd, IntPtr lParam)
            {
                if (!IsWindow(hWnd))
                {
                    return 1;
                }
                if (!IsWindowVisible(hWnd))
                {
                    return 1;
                }
                if (!IsWindowEnabled(hWnd))
                {
                    return 1;
                }
                int windowProcessId = 0;
                int threadId = GetWindowThreadProcessId(hWnd, out windowProcessId);
                if (processId == windowProcessId)
                {
                    handles.Add(hWnd);
                }
                return 1;
            };
            EnumWindows(callback, IntPtr.Zero);
            GC.KeepAlive(callback);
            return handles.ToArray();
        }


        private void _buttonAttach_Click(object sender, EventArgs e)
        {
            Process process = null;
            try
            {
                process = Process.GetProcessById(int.Parse(_textBoxPID.Text));
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
                return;
            }
            foreach (var x in GetTopLevelWindows(process.Id))
            {
                var b = new StringBuilder(1024);
                GetWindowText(x, b, 1024);
                try
                {
                    using (var app = new WindowsAppFriend(process)) { }
                    MessageBox.Show(b.ToString() + "\r\n成功");
                    break;
                }
                catch (Exception exp)
                {
                    MessageBox.Show(b.ToString() + "\r\n" + exp.Message);
                }
            }

        }
    }
}
