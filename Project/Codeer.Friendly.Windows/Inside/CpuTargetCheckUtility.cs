#define CODE_ANALYSIS
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// CPU対象チェック
    /// </summary>
    public static class CpuTargetCheckUtility
    {
        /// <summary>
        /// 同一のCPUをターゲットとしているか
        /// </summary>
        /// <param name="process">プロセス</param>
        /// <returns>テスト対象となりえるか</returns>
        public static bool IsSameCpu(Process process)
        {
            return IsWow64(process) == IsWow64(Process.GetCurrentProcess());
        }

        /// <summary>
        /// Wow64上で動作しているか
        /// </summary>
        /// <param name="process">プロセス</param>
        /// <returns>Wow64上で動作しているか</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        static bool IsWow64(Process process)
        {
            //IsWow64Processが使えるか調べる
            IntPtr wow64Proc = NativeMethods.GetProcAddress(NativeMethods.GetModuleHandle("Kernel32.dll"), "IsWow64Process");
            if (wow64Proc == IntPtr.Zero)
            {
                return false;
            }
            try
            {
                //IsWow64Processを呼び出す
                bool is32;
                if (NativeMethods.IsWow64Process(process.Handle, out is32))
                {
                    return is32;
                }
            }
            catch { }
            return false;
        }
    }
}
