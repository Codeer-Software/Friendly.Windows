using System;
using System.Runtime.InteropServices;

namespace Codeer.Friendly.Windows.Step
{
    public class AppDomainControl
    {
        [DllImport("Kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr LoadLibrary(string fileName);

        [DllImport("Kernel32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        static extern IntPtr GetProcAddress(IntPtr hMod, string funcName);

        delegate int EnumDomainsDelegate(
            [MarshalAs(UnmanagedType.LPWStr)]string szVersion,
            [MarshalAs(UnmanagedType.LPWStr)]string szStepAssembly,
            [MarshalAs(UnmanagedType.LPWStr)]string szGetIDClass,
            [MarshalAs(UnmanagedType.LPWStr)]string szGetIDMethod,
            [In, Out]int[] ids,
            int size);

        [return: MarshalAs(UnmanagedType.Bool)]
        delegate bool InitializeAppDomainDelegate(
             [MarshalAs(UnmanagedType.LPWStr)]string szVersion,
             [MarshalAs(UnmanagedType.LPWStr)]string szStepAssembly,
             [MarshalAs(UnmanagedType.LPWStr)]string szGetIDClass,
             [MarshalAs(UnmanagedType.LPWStr)]string szGetIDMethod,
             [MarshalAs(UnmanagedType.LPWStr)]string szStartClass,
             [MarshalAs(UnmanagedType.LPWStr)]string szStartMethod,
             int id,
             [MarshalAs(UnmanagedType.LPWStr)]string pStartInfo);

        EnumDomainsDelegate _enumDomains;
        InitializeAppDomainDelegate _initializeAppDomain;

        public AppDomainControl(string nativeDllPath)
        {
            IntPtr hDll = LoadLibrary(nativeDllPath);

            IntPtr func = GetProcAddress(hDll, "EnumDomains");
            _enumDomains = (EnumDomainsDelegate)Marshal.GetDelegateForFunctionPointer(func, typeof(EnumDomainsDelegate));

            func = GetProcAddress(hDll, "InitializeAppDomain");
            _initializeAppDomain = (InitializeAppDomainDelegate)Marshal.GetDelegateForFunctionPointer(func, typeof(InitializeAppDomainDelegate));
        }

        public int[] EnumDomains()
        {
            int[] getIds = new int[1024];
            int count = 0;

            while (true)
            {
                count = _enumDomains(RuntimeEnvironment.GetSystemVersion(),
                    GetType().Assembly.Location,
                    typeof(AppDomainBridge).FullName,
                    "GetCurrentDomainId",
                    getIds,
                    getIds.Length);
                if (count == -1)
                {
                    return null;
                }
                if (count < getIds.Length)
                {
                    break;
                }
                getIds = new int[count];
            }

            int[] ids = new int[count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = getIds[i];
            }
            return ids;
        }

        public bool InitializeAppDomain(int id, string args)
        {
            return _initializeAppDomain(
                RuntimeEnvironment.GetSystemVersion(),
                    GetType().Assembly.Location,
                    typeof(AppDomainBridge).FullName,
                    "GetCurrentDomainId",
                    typeof(StartStepWrap).FullName,
                    "Start",
                    id,
                    args
                    );
        }
    }
}
